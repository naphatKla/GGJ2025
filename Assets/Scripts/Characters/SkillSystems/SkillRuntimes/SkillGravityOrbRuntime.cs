using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime for the Gravity Orb auto skill.
    /// On cast: picks up to <c>OrbCount</c> distinct enemies (farthest-first, within
    /// <c>TargetSearchRadius</c>) and fires one orb at each. Each orb flies to its target's
    /// position and stops there (it does not keep homing), then pulls any enemy that wanders
    /// into <c>OrbEffectRadius</c> toward its center for <c>OrbLifeTime</c> seconds while those
    /// enemies are locked out of their Primary/Secondary skills, and finally explodes for AOE
    /// damage in that same radius.
    /// </summary>
    public class SkillGravityOrbRuntime : BaseSkillRuntime<SkillGravityOrbDataSo>
    {
        private readonly Collider2D[] _searchBuffer = new Collider2D[64];
        private readonly Collider2D[] _pullBuffer = new Collider2D[64];

        private readonly List<UniTask> _activeOrbTasks = new();
        private readonly List<GravityOrbSkillObject> _activeOrbs = new();

        // Reused per-frame scratch buffers for the pull phase (safe: never re-entered mid-call).
        private readonly HashSet<BaseController> _frameSeen = new();
        private readonly List<BaseController> _leftFieldBuffer = new();

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<GravityOrbSkillObject>(
                this.skillData.OrbPrefab.name,
                PoolingGroupName.SkillObject,
                CreatePoolInstance,
                prewarmCount: this.skillData.OrbCount);
        }

        // ======================== LIFECYCLE ========================

        protected override void OnSkillStart()
        {
            _activeOrbTasks.Clear();

            var targets = FindFarthestUniqueEnemies(owner.transform.position, skillData.TargetSearchRadius, skillData.OrbCount);
            for (int i = 0; i < targets.Count; i++)
                _activeOrbTasks.Add(RunOrb(targets[i], cts.Token));
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            if (_activeOrbTasks.Count == 0) return;

            try
            {
                await UniTask.WhenAll(_activeOrbTasks);
            }
            catch (OperationCanceledException)
            {
            }
        }

        protected override void OnSkillExit()
        {
            _activeOrbTasks.Clear();

            // Safety cleanup for any orb that was still active (e.g. skill got cancelled mid-flight).
            for (int i = _activeOrbs.Count - 1; i >= 0; i--)
                ReleaseOrb(_activeOrbs[i]);
        }

        // ======================== TARGETING ========================

        /// <summary>
        /// Finds up to <paramref name="count"/> distinct enemies within <paramref name="radius"/>,
        /// preferring the farthest ones first, so each orb gets a unique target.
        /// </summary>
        private List<Transform> FindFarthestUniqueEnemies(Vector2 origin, float radius, int count)
        {
            var result = new List<Transform>(Mathf.Max(0, count));
            if (count <= 0) return result;

            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int found = Physics2D.OverlapCircleNonAlloc(origin, radius, _searchBuffer, damageLayer);
            if (found <= 0) return result;

            var candidates = new List<(Transform target, float sqrDist)>(found);
            var seen = new HashSet<Transform>();

            for (int i = 0; i < found; i++)
            {
                var col = _searchBuffer[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (target.transform == owner.transform) continue;
                if (target.HealthSystem == null || target.HealthSystem.IsDead || !target.HealthSystem.CanAim) continue;
                if (!seen.Add(target.transform)) continue; // a character can have multiple colliders

                float sqrDist = ((Vector2)target.transform.position - origin).sqrMagnitude;
                candidates.Add((target.transform, sqrDist));
            }

            candidates.Sort((a, b) => b.sqrDist.CompareTo(a.sqrDist)); // farthest first

            for (int i = 0; i < candidates.Count && result.Count < count; i++)
                result.Add(candidates[i].target);

            return result;
        }

        // ======================== ORB LIFECYCLE ========================

        private async UniTask RunOrb(Transform target, CancellationToken ct)
        {
            if (!target) return;

            var orb = PoolingManager.Instance.Get<GravityOrbSkillObject>(skillData.OrbPrefab.name);
            orb.transform.position = owner.transform.position;
            orb.gameObject.SetActive(true);
            _activeOrbs.Add(orb);

            Vector2 destination = target.position; // Snapshot - the orb stops here, it doesn't keep homing.

            try
            {
                // Phase 1: travel to the target's position and stop.
                var travelTween = orb.MovementSystem.TryMoveToPositionBySpeed(
                    destination, skillData.TravelSpeed, skillData.TravelEaseCurve, skillData.TravelMoveCurve);

                if (travelTween != null)
                    await travelTween.ToUniTask(cancellationToken: ct);
                else
                    orb.transform.position = destination;

                if (ct.IsCancellationRequested || !orb) return;

                // Phase 2: sit still, pulling nearby enemies for the orb's lifetime.
                orb.SetPullFieldVisible(true);
                await PullPhase(orb, skillData.OrbLifeTime, ct);
                orb.SetPullFieldVisible(false);

                if (ct.IsCancellationRequested || !orb) return;

                // Phase 3: explode.
                Explode(orb.transform.position);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                ReleaseOrb(orb);
            }
        }

        /// <summary>
        /// Runs the continuous gravity-pull field for <paramref name="duration"/> seconds,
        /// applying/removing the CC status on enemies as they enter/leave the field, and
        /// guaranteeing everyone still held gets released when the phase ends for any reason.
        /// </summary>
        private async UniTask PullPhase(GravityOrbSkillObject orb, float duration, CancellationToken ct)
        {
            var pulledTargets = new HashSet<BaseController>();
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];

            try
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (ct.IsCancellationRequested || !orb || !orb.gameObject.activeSelf) return;

                    float dt = Time.fixedDeltaTime;
                    elapsed += dt;

                    UpdatePull(orb.transform.position, damageLayer, pulledTargets, dt);

                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                }
            }
            finally
            {
                foreach (var pulled in pulledTargets)
                    RemovePulledEffect(pulled);
            }
        }

        private void UpdatePull(Vector2 center, LayerMask damageLayer, HashSet<BaseController> pulledTargets, float dt)
        {
            int found = Physics2D.OverlapCircleNonAlloc(center, skillData.OrbEffectRadius, _pullBuffer, damageLayer);

            _frameSeen.Clear();

            for (int i = 0; i < found; i++)
            {
                var col = _pullBuffer[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (target.HealthSystem == null || target.HealthSystem.IsDead) continue;

                _frameSeen.Add(target);

                if (pulledTargets.Add(target))
                    ApplyPulledEffect(target);

                var mover = target.MovementSystem;
                if (!mover) continue;

                Vector2 pos = mover.transform.position;
                Vector2 toCenter = center - pos;
                float dist = toCenter.magnitude;
                if (dist <= skillData.PullStopDistance) continue;

                float proximity = Mathf.InverseLerp(skillData.OrbEffectRadius, 0f, dist);
                float curveMul = Mathf.Max(0f, skillData.PullCurve.Evaluate(proximity));
                float strength = skillData.PullStrength * curveMul;

                if (!skillData.UseExponentialPull)
                {
                    Vector2 dir = toCenter / dist;
                    float step = strength * dt;
                    Vector2 nextPos = pos + dir * Mathf.Min(step, dist - skillData.PullStopDistance);
                    mover.TryMoveRawPosition(nextPos);
                }
                else
                {
                    float t = 1f - Mathf.Exp(-strength * dt);
                    Vector2 lerped = Vector2.Lerp(pos, center, t);
                    if ((lerped - center).sqrMagnitude < skillData.PullStopDistance * skillData.PullStopDistance)
                        lerped = center + (pos - center).normalized * skillData.PullStopDistance;
                    mover.TryMoveRawPosition(lerped);
                }
            }

            // Release anyone that fell outside the field this frame.
            _leftFieldBuffer.Clear();
            foreach (var pulled in pulledTargets)
                if (!_frameSeen.Contains(pulled))
                    _leftFieldBuffer.Add(pulled);

            for (int i = 0; i < _leftFieldBuffer.Count; i++)
            {
                var left = _leftFieldBuffer[i];
                pulledTargets.Remove(left);
                RemovePulledEffect(left);
            }
        }

        private void ApplyPulledEffect(BaseController target)
        {
            if (!target || skillData.PulledStatusEffects == null || skillData.PulledStatusEffects.Count == 0) return;
            StatusEffectManager.ApplyEffectTo(target.gameObject, skillData.PulledStatusEffects);
        }

        private void RemovePulledEffect(BaseController target)
        {
            if (!target || skillData.PulledStatusEffects == null || skillData.PulledStatusEffects.Count == 0) return;
            StatusEffectManager.RemoveEffectAt(target.gameObject, skillData.PulledStatusEffects);
        }

        // ======================== EXPLOSION ========================

        private void Explode(Vector2 center)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            var targets = Physics2D.OverlapCircleAll(center, skillData.OrbEffectRadius, damageLayer);
            if (targets.Length == 0) return;

            owner.TryPlayFeedback(skillData.ExplodeFeedback);

            foreach (var target in targets)
            {
                if (!CombatManager.TryGetCharacterFromCache(target.gameObject, out var targetController)) continue;
                if (targetController.HealthSystem == null || targetController.HealthSystem.IsDead) continue;

                CombatManager.ApplyCalculatedDamageTo(
                    target.gameObject,
                    owner.gameObject,
                    owner.CharacterData.CharacterId,
                    owner.gameObject,
                    target.ClosestPoint(center),
                    skillData.ExplosionBaseDamage,
                    skillData.ExplosionDamageMultiplier,
                    0, 0, 0, 0);
            }
        }

        // ======================== POOL ========================

        private void ReleaseOrb(GravityOrbSkillObject orb)
        {
            // Guards against double-release: RunOrb's finally and OnSkillExit's safety
            // sweep can both target the same orb when the skill is cancelled mid-flight.
            if (!orb || !orb.gameObject.activeSelf) return;

            _activeOrbs.Remove(orb);
            orb.MovementSystem.StopTween();
            orb.ResetForPool();
            orb.gameObject.SetActive(false);
            orb.transform.position = owner.transform.position;
            PoolingManager.Current?.Release(skillData.OrbPrefab.name, orb);
        }

        private GravityOrbSkillObject CreatePoolInstance()
        {
            var obj = Instantiate(skillData.OrbPrefab);
            obj.gameObject.SetActive(false);
            obj.transform.position = owner.transform.position;
            return obj;
        }
    }
}
