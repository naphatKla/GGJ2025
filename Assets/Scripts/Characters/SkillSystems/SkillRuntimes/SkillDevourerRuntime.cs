using System.Collections.Generic;
using System.Threading;
using Cameras;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GameControl.EventMap;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime for Bright2's primary skill "Devourer" (Design Ver 0.0.20).
    /// <para/>
    /// The boss itself is the black hole - everything is pulled toward the owner, not toward a spawned
    /// object. Runs as: <b>Pull</b> (drag everything within Pull Radius inward, damaging whatever is
    /// inside the smaller Damage Radius at Hit/s) and, for the phase-3 variant only, <b>Flee</b> (stop
    /// pulling and back away from the player) then <b>Explode</b>.
    /// <para/>
    /// Devourer is the only skill that triggers <see cref="ISpecialInteractionTarget"/>s: anything on the
    /// interaction layer that reaches the damage radius is consumed and runs its own reaction (a Black
    /// Hole explodes, a Border piece just disappears). This skill deliberately knows nothing about what
    /// those reactions do.
    /// </summary>
    public class SkillDevourerRuntime : BaseSkillRuntime<SkillDevourerDataSo>
    {
        private readonly Collider2D[] _pullBuffer = new Collider2D[64];
        private readonly Collider2D[] _damageBuffer = new Collider2D[64];
        private readonly List<ISpecialInteractionTarget> _interactionBuffer = new();

        private readonly HashSet<BaseController> _pulledTargets = new();
        private readonly HashSet<BaseController> _frameSeen = new();
        private readonly List<BaseController> _leftFieldBuffer = new();

        /// <summary>Next time each target may be hit again, so Hit/s is honoured per target.</summary>
        private readonly Dictionary<BaseController, float> _nextHitTime = new();

        /// <summary>Whatever the owner's pull immunity was before this channel, so exiting can restore it.</summary>
        private bool _previousIgnoreExternalPull;

        private LayerMask DamageLayer => CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];

        // ======================== LIFECYCLE ========================

        protected override void OnSkillStart()
        {
            _pulledTargets.Clear();
            _nextHitTime.Clear();

            if (skillData.ImmuneToExternalPull && owner && owner.MovementSystem)
            {
                _previousIgnoreExternalPull = owner.MovementSystem.IgnoreExternalPull;
                owner.MovementSystem.IgnoreExternalPull = true;
            }
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await PullPhase(cancelToken);

            // Phase 1 & 2 Devourer simply ends here.
            if (!skillData.EnableFleeAndExplode) return;

            await FleePhase(cancelToken);
            Explode();
        }

        protected override void OnSkillExit()
        {
            ReleaseAllPulledTargets();
            _nextHitTime.Clear();

            // Restored (not cleared) on every exit path: a caster that is ALWAYS immune - Bright2 is - must
            // keep its immunity after the channel, while a cancelled channel must not leave one immune forever.
            if (skillData.ImmuneToExternalPull && owner && owner.MovementSystem)
                owner.MovementSystem.IgnoreExternalPull = _previousIgnoreExternalPull;
        }

        // ======================== STATE: PULL ========================

        private async UniTask PullPhase(CancellationToken cancelToken)
        {
            try
            {
                float elapsed = 0f;
                while (elapsed < skillData.PullDuration)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    if (!owner) return;

                    float dt = Time.fixedDeltaTime;
                    elapsed += dt;

                    Vector2 center = owner.transform.position;
                    UpdatePull(center, dt);
                    UpdateDamage(center);
                    UpdateSpecialInteractions(center, dt);

                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancelToken);
                }
            }
            finally
            {
                // Whatever ends this phase - timeout, cancel, Break Point breaking the boss mid-cast -
                // nobody stays stuck with the pulled status effect.
                ReleaseAllPulledTargets();
            }
        }

        private void UpdatePull(Vector2 center, float dt)
        {
            int found = Physics2D.OverlapCircleNonAlloc(center, skillData.PullRadius, _pullBuffer, DamageLayer);

            _frameSeen.Clear();

            for (int i = 0; i < found; i++)
            {
                var col = _pullBuffer[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (target == owner) continue;
                if (target.HealthSystem == null || target.HealthSystem.IsDead) continue;

                _frameSeen.Add(target);

                if (_pulledTargets.Add(target))
                    ApplyPulledEffect(target);

                var mover = target.MovementSystem;
                if (!mover) continue;

                // A dash is a movement tween; pulling writes raw positions every FixedUpdate and would
                // simply erase it. Let the dash finish - that is the counterplay to being pulled in.
                if (skillData.IgnoreDashingTargets && mover.IsMoveTweenActive) continue;

                Vector2 position = mover.transform.position;
                Vector2 toCenter = center - position;
                float distance = toCenter.magnitude;
                if (distance <= skillData.PullStopDistance) continue;

                mover.TryMoveRawPosition(ComputePullStep(position, center, distance, dt));
            }

            // Release anyone that slipped out of the field this frame.
            _leftFieldBuffer.Clear();
            foreach (var pulled in _pulledTargets)
                if (!_frameSeen.Contains(pulled))
                    _leftFieldBuffer.Add(pulled);

            for (int i = 0; i < _leftFieldBuffer.Count; i++)
            {
                var left = _leftFieldBuffer[i];
                _pulledTargets.Remove(left);
                RemovePulledEffect(left);
            }
        }

        /// <summary>
        /// One frame of pull for anything at <paramref name="position"/>: applies the falloff curve, then
        /// caps the step to Max Pull Speed.
        /// <para/>
        /// The exponential pull is a rate, not a speed - at the spec's Pull Force it closes a huge fraction
        /// of the remaining distance every FixedUpdate, so without the cap escaping is impossible.
        /// </summary>
        private Vector2 ComputePullStep(Vector2 position, Vector2 center, float distance, float dt)
        {
            Vector2 toCenter = center - position;

            float proximity = Mathf.InverseLerp(skillData.PullRadius, 0f, distance);
            float curveMultiplier = Mathf.Max(0f, skillData.PullCurve.Evaluate(proximity));
            float strength = skillData.PullStrength * curveMultiplier;

            Vector2 next;
            if (!skillData.UseExponentialPull)
            {
                Vector2 direction = toCenter / distance;
                float step = strength * dt;
                next = position + direction * Mathf.Min(step, distance - skillData.PullStopDistance);
            }
            else
            {
                float t = 1f - Mathf.Exp(-strength * dt);
                next = Vector2.Lerp(position, center, t);
                if ((next - center).sqrMagnitude < skillData.PullStopDistance * skillData.PullStopDistance)
                    next = center + (position - center).normalized * skillData.PullStopDistance;
            }

            if (skillData.MaxPullSpeed <= 0f) return next;

            float maxStep = skillData.MaxPullSpeed * dt;
            Vector2 delta = next - position;
            if (delta.sqrMagnitude <= maxStep * maxStep) return next;

            return position + delta.normalized * maxStep;
        }

        /// <summary>
        /// Damages everything inside the (smaller) damage radius, at most once per 1/HitPerSecond
        /// seconds per target.
        /// </summary>
        private void UpdateDamage(Vector2 center)
        {
            int found = Physics2D.OverlapCircleNonAlloc(center, skillData.DamageRadius, _damageBuffer, DamageLayer);
            if (found <= 0) return;

            float now = Time.time;
            float interval = 1f / Mathf.Max(0.01f, skillData.HitPerSecond);

            for (int i = 0; i < found; i++)
            {
                var col = _damageBuffer[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (target == owner) continue;
                if (target.HealthSystem == null || target.HealthSystem.IsDead) continue;

                if (_nextHitTime.TryGetValue(target, out float nextHit) && now < nextHit) continue;
                _nextHitTime[target] = now + interval;

                CombatManager.ApplyCalculatedDamageTo(
                    target.gameObject,
                    owner.gameObject,
                    owner.CharacterData.CharacterId,
                    owner.gameObject,
                    col.ClosestPoint(center),
                    skillData.BaseDamagePerHit,
                    skillData.DamageMultiplier,
                    0, 0, 0, 0,
                    skillData.BaseHeavyDamage);
            }
        }

        // ======================== SPECIAL INTERACTION ========================

        /// <summary>
        /// Drags Special Interaction targets inward exactly like everything else, and consumes any that
        /// reach the damage radius. Per the doc a Black Hole has to be pulled INTO Devourer before it
        /// reacts - it never wanders in by itself, so without this the interaction could never fire.
        /// <para/>
        /// These are moved by transform: map events have no MovementSystem (and no Collider2D either,
        /// which is why they come from <see cref="SpecialInteractionRegistry"/> instead of a physics sweep).
        /// </summary>
        private void UpdateSpecialInteractions(Vector2 center, float dt)
        {
            SpecialInteractionRegistry.GetTargetsInRadius(center, skillData.PullRadius, _interactionBuffer);
            if (_interactionBuffer.Count == 0) return;

            var context = new SpecialInteractionContext(
                owner, owner.gameObject, owner.CharacterData.CharacterId, center);

            // _interactionBuffer is our own copy, so targets unregistering themselves as they fire is fine.
            for (int i = 0; i < _interactionBuffer.Count; i++)
            {
                var interaction = _interactionBuffer[i];
                if (interaction == null || !interaction.CanTriggerSpecialInteraction) continue;

                var targetTransform = interaction.Transform;
                if (!targetTransform) continue;

                Vector2 position = targetTransform.position;
                float distance = Vector2.Distance(position, center);

                // Reached the grinder - consume it and let it run its own reaction.
                if (distance <= skillData.SpecialInteractionRadius)
                {
                    interaction.TriggerSpecialInteraction(context);
                    continue;
                }

                Vector2 next = ComputePullStep(position, center, distance, dt);
                targetTransform.position = new Vector3(next.x, next.y, targetTransform.position.z);
            }
        }

        // ======================== STATE: FLEE ========================

        /// <summary>
        /// Phase-3 only: the boss stops pulling and backs away from the player for a moment before it
        /// blows up, giving the player a window to read the explosion coming.
        /// </summary>
        private async UniTask FleePhase(CancellationToken cancelToken)
        {
            if (!owner) return;

            Vector2 ownerPosition = owner.transform.position;
            Vector2 awayDirection = PlayerController.Instance
                ? ownerPosition - (Vector2)PlayerController.Instance.transform.position
                : Vector2.zero;

            awayDirection = awayDirection.sqrMagnitude > 0.0001f
                ? awayDirection.normalized
                : (Vector2)owner.transform.up;

            Vector2 destination = ownerPosition + awayDirection * skillData.FleeDistance;

            // Never flee outside the playable area.
            var confiner = Cinemachine2DCameraController.Instance
                ? Cinemachine2DCameraController.Instance.ConfinerBounds
                : null;
            if (confiner)
                destination = confiner.ClosestPoint(destination);

            owner.MovementSystem.TryMoveToPositionOverTime(destination, skillData.FleeDuration);

            await UniTask.WaitForSeconds(skillData.FleeDuration, cancellationToken: cancelToken);
        }

        // ======================== STATE: EXPLODE ========================

        private void Explode()
        {
            if (!owner) return;

            Vector2 center = owner.transform.position;
            var targets = Physics2D.OverlapCircleAll(center, skillData.ExplodeRadius, DamageLayer);
            if (targets.Length == 0) return;

            owner.TryPlayFeedback(skillData.ExplodeFeedback);

            var hitTargets = new HashSet<BaseController>();

            foreach (var col in targets)
            {
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;
                if (target == owner) continue;
                if (!hitTargets.Add(target)) continue; // a character can own several colliders
                if (target.HealthSystem == null || target.HealthSystem.IsDead) continue;

                CombatManager.ApplyCalculatedDamageTo(
                    target.gameObject,
                    owner.gameObject,
                    owner.CharacterData.CharacterId,
                    owner.gameObject,
                    col.ClosestPoint(center),
                    skillData.ExplodeBaseDamage,
                    skillData.ExplodeDamageMultiplier,
                    0, 0, 0, 0,
                    skillData.BaseHeavyDamage);
            }
        }

        // ======================== STATUS EFFECTS ========================

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

        private void ReleaseAllPulledTargets()
        {
            foreach (var pulled in _pulledTargets)
                RemovePulledEffect(pulled);

            _pulledTargets.Clear();
        }

        // ======================== GIZMOS ========================

        /// <summary>Drawn without selecting anything while the skill is live, so the field is visible in play mode.</summary>
        private void OnDrawGizmos()
        {
            if (!IsPerforming) return;
            DrawRangeGizmos();
        }

        /// <summary>Same rings while idle, for setting the numbers up in the Inspector.</summary>
        private void OnDrawGizmosSelected()
        {
            if (IsPerforming) return; // already drawn by OnDrawGizmos
            DrawRangeGizmos();
        }

        private void DrawRangeGizmos()
        {
            if (skillData == null || !owner) return;

            Vector3 center = owner.transform.position;

            // Pull field - outermost.
            Gizmos.color = new Color(0.35f, 0.5f, 1f, 0.9f);
            Gizmos.DrawWireSphere(center, skillData.PullRadius);

            // Damage radius.
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
            Gizmos.DrawWireSphere(center, skillData.DamageRadius);

            // Where a dragged Black Hole / Border piece is finally consumed.
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(center, skillData.SpecialInteractionRadius);

            if (!skillData.EnableFleeAndExplode) return;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
            Gizmos.DrawWireSphere(center, skillData.ExplodeRadius);
        }
    }
}
