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
    public class SkillPhotonBarrageRuntime : BaseSkillRuntime<SkillPhotonBarrageDataSo>
    {
        private readonly List<PhotonBarrageSkillObject> _missiles = new();
        private readonly Collider2D[] _candidates = new Collider2D[64];

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<PhotonBarrageSkillObject>(
                this.skillData.MissilePrefab.name,
                PoolingGroupName.SkillObject,
                CreatePoolInstance,
                prewarmCount: this.skillData.MissileCount);
        }

        // ======================== LIFECYCLE ========================

        protected override void OnSkillStart()
        {
            _missiles.Clear();

            for (int i = 0; i < skillData.MissileCount; i++)
            {
                var missile = PoolingManager.Instance.Get<PhotonBarrageSkillObject>(skillData.MissilePrefab.name);
                missile.transform.position = owner.transform.position;
                missile.DamageOnTouch.EnableDamage(
                    owner.gameObject,
                    owner.CharacterData.CharacterId,
                    this,
                    skillData.DamageHitPerSec,
                    skillData.BaseDamagePerHit,
                    skillData.DamageMultiplier);

                missile.OnHitEnemy += HandleMissileHit;
                missile.ResetTrail();
                missile.gameObject.SetActive(true);
                _missiles.Add(missile);
            }
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            int count = skillData.MissileCount;

            // ──────── Phase 1 : Spread outward ────────
            await RunStaggeredSpread(count, cancelToken);

            // ──────── Phase 2 : Homing by speed ────────
            await LaunchHomingAll(count, cancelToken);
        }

        protected override void OnSkillExit()
        {
            foreach (var missile in _missiles)
            {
                if (!missile || !missile.gameObject) continue;
                missile.OnHitEnemy -= HandleMissileHit;
                missile.ClearCallbacks();
                missile.DamageOnTouch.DisableDamage(this);
                missile.gameObject.SetActive(false);
                missile.transform.position = owner.transform.position;
                PoolingManager.Current?.Release(skillData.MissilePrefab.name, missile);
            }

            _missiles.Clear();
        }

        // ======================== PHASE 1 : SPREAD ========================

        private async UniTask RunStaggeredSpread(int count, CancellationToken ct)
        {
            float entireDur = skillData.SpreadEntireDuration;
            float staggerDur = skillData.SpreadStaggerDuration;

            float[] weights = BuildWeights(count, skillData.SpreadStaggerCurve);
            float weightSum = Sum(weights);
            float remaining = entireDur;

            for (int i = 0; i < count; i++)
            {
                float delay = weightSum > 0f ? (weights[i] / weightSum) * staggerDur : 0f;
                float moveDur = Mathf.Clamp(remaining - delay, 0.05f, remaining);
                remaining -= delay;

                float angle = i * 2f * Mathf.PI / count;
                Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 target = (Vector2)_missiles[i].transform.position + dir * skillData.SpreadDistance;

                _missiles[i].MovementSystem.TryMoveToPositionOverTime(
                    target, moveDur,
                    skillData.SpreadEaseCurve,
                    skillData.SpreadMoveCurve);

                if (delay > 0f)
                    await UniTask.Delay((int)(delay * 1000), cancellationToken: ct);
            }

            await UniTask.Delay((int)(remaining * 1000), cancellationToken: ct);
        }

        // ======================== PHASE 2 : HOMING ========================

        private async UniTask LaunchHomingAll(int count, CancellationToken ct)
        {
            // Stagger the launch of each missile's homing loop
            float[] weights = BuildWeights(count, skillData.HomingStaggerCurve);
            float weightSum = Sum(weights);

            List<UniTask> homingTasks = new(count);

            for (int i = 0; i < count; i++)
            {
                float delay = weightSum > 0f
                    ? (weights[i] / weightSum) * skillData.HomingStaggerDuration
                    : 0f;

                if (delay > 0f)
                    await UniTask.Delay((int)(delay * 1000), cancellationToken: ct);

                var missile = _missiles[i];
                if (!missile.gameObject.activeSelf) continue;

                // Stop any residual spread tween
                missile.MovementSystem.StopTween();

                // Each missile homes independently
                homingTasks.Add(HomingSingleMissile(missile, ct));
            }

            // Wait for all homing missiles to finish (hit or timeout)
            await UniTask.WhenAll(homingTasks);
        }

        /// <summary>
        /// Per-frame homing loop for a single missile.
        /// Flies at constant speed, turns toward target with a max turn rate,
        /// and applies a decaying arc offset for the satisfying curved trajectory.
        /// </summary>
        private async UniTask HomingSingleMissile(PhotonBarrageSkillObject missile, CancellationToken ct)
        {
            Transform missileT = missile.transform;
            Transform target = FindClosestEnemy(missileT.position);

            // Initial fly direction: outward from owner (the spread direction)
            Vector2 flyDir = ((Vector2)missileT.position - (Vector2)owner.transform.position).normalized;
            if (flyDir.sqrMagnitude <= float.Epsilon) flyDir = Vector2.right;

            // Choose arc side: perpendicular to initial direction toward target
            float arcSign = 1f;
            if (target)
            {
                Vector2 toTarget = ((Vector2)target.position - (Vector2)missileT.position).normalized;
                float cross = flyDir.x * toTarget.y - flyDir.y * toTarget.x;
                arcSign = cross >= 0f ? 1f : -1f;
            }

            float elapsed = 0f;
            float maxLife = skillData.HomingMaxLifetime;
            float speed = skillData.HomingSpeed;
            float turnSpeed = skillData.HomingTurnSpeed;
            float arcStrength = skillData.HomingArcStrength;

            while (elapsed < maxLife)
            {
                if (ct.IsCancellationRequested) return;
                if (!missile || !missile.gameObject.activeSelf) return;

                float dt = Time.fixedDeltaTime;
                elapsed += dt;
                float lifeT = Mathf.Clamp01(elapsed / maxLife);

                // Re-evaluate target validity each frame
                if (target && (!target.gameObject.activeSelf || !IsTargetAimable(target)))
                {
                    bool shouldReacquire = false;

                    if (!target || !target.gameObject.activeSelf || !IsTargetAimable(target))
                    {
                        shouldReacquire = true;
                    }
                    else
                    {
                        float distSqr = ((Vector2)target.position - (Vector2)missileT.position).sqrMagnitude;
                        float detRadius = skillData.HomingDetectionRadius;
                        if (distSqr > detRadius * detRadius)
                        {
                            // Current target left detection range — drop it
                            shouldReacquire = true;
                        }
                    }

                    if (shouldReacquire)
                    {
                        target = FindClosestEnemy(missileT.position);

                        // Recalculate arc side for new target
                        if (target)
                        {
                            Vector2 toTarget = ((Vector2)target.position - (Vector2)missileT.position).normalized;
                            float cross = flyDir.x * toTarget.y - flyDir.y * toTarget.x;
                            arcSign = cross >= 0f ? 1f : -1f;
                        }
                    }
                }

                // Desired direction
                Vector2 desiredDir = flyDir;
                if (target && target.gameObject.activeSelf)
                {
                    desiredDir = ((Vector2)target.position - (Vector2)missileT.position).normalized;
                }

                // Rotate toward desired direction with max turn rate
                float maxTurnThisFrame = turnSpeed * dt;
                float angleCurrent = Mathf.Atan2(flyDir.y, flyDir.x) * Mathf.Rad2Deg;
                float angleDesired = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;
                float newAngle = Mathf.MoveTowardsAngle(angleCurrent, angleDesired, maxTurnThisFrame);
                float rad = newAngle * Mathf.Deg2Rad;
                flyDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                // Arc offset (decays over lifetime for the swoop)
                float arcDecay = skillData.HomingArcDecayCurve.Evaluate(lifeT);
                Vector2 perpendicular = Vector2.Perpendicular(flyDir) * (arcSign * arcStrength * arcDecay);

                // Move
                Vector2 velocity = (flyDir * speed + perpendicular) * dt;
                Vector2 newPos = (Vector2)missileT.position + velocity;
                missileT.position = newPos;

                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
            }

            // Lifetime expired — despawn
            DeactivateMissile(missile);
        }

        // ======================== TARGETING ========================

        private Transform FindClosestEnemy(Vector2 origin)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int found = Physics2D.OverlapCircleNonAlloc(
                origin, skillData.HomingDetectionRadius, _candidates, damageLayer);

            if (found <= 0) return null;

            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < found; i++)
            {
                var col = _candidates[i];
                if (!col) continue;
                if (!CombatManager.TryGetCharacterFromCache(col.gameObject, out var target)) continue;

                var health = target.HealthSystem;
                if (!health.CanAim) continue;
                if (health.transform == owner.transform) continue;

                float sqr = ((Vector2)health.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = health.transform;
                }
            }

            return best;
        }

        private bool IsTargetAimable(Transform target)
        {
            if (!target) return false;
            if (!CombatManager.TryGetCharacterFromCache(target.gameObject, out var controller)) return false;
            return controller.HealthSystem.CanAim;
        }

        // ======================== HIT / DEACTIVATION ========================

        private void HandleMissileHit(PhotonBarrageSkillObject missile) => DeactivateMissile(missile);

        private void DeactivateMissile(PhotonBarrageSkillObject missile)
        {
            if (!missile || !missile.gameObject.activeSelf) return;
            missile.MovementSystem.StopTween();
            missile.DamageOnTouch.DisableDamage(this);
            missile.gameObject.SetActive(false);
        }

        // ======================== UTILS ========================

        private static float[] BuildWeights(int count, AnimationCurve curve)
        {
            float[] w = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? i / (float)(count - 1) : 0f;
                w[i] = Mathf.Clamp01(curve.Evaluate(t));
            }
            return w;
        }

        private static float Sum(float[] arr)
        {
            float s = 0f;
            foreach (var v in arr) s += v;
            return s;
        }

        // ======================== POOL ========================

        private PhotonBarrageSkillObject CreatePoolInstance()
        {
            var obj = Instantiate(skillData.MissilePrefab);
            obj.gameObject.SetActive(false);
            obj.transform.position = owner.transform.position;
            return obj;
        }
    }
}