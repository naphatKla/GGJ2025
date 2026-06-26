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
    public class SkillEnemyProjectileRuntime : BaseSkillRuntime<SkillEnemyProjectileDataSo>
    {
        private readonly HashSet<EnemyProjectileSkillObject> _activeProjectiles = new();
        private float _spiralAngle;
        private bool _releaseProjectilesOnExit;

        private string PoolKey => skillData.ProjectilePrefab ? skillData.ProjectilePrefab.name : string.Empty;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);

            if (!this.skillData.ProjectilePrefab)
            {
                Debug.LogWarning("[EnemyProjectile] Missing projectile prefab.");
                return;
            }

            PoolingManager.Instance.Create<EnemyProjectileSkillObject>(
                PoolKey,
                PoolingGroupName.Projectile,
                CreatePoolInstance,
                onRelease: OnPoolRelease,
                prewarmCount: this.skillData.PrewarmCount);
        }

        protected override void OnSkillStart()
        {
            if (skillData.ResetSpiralOnCast)
                _spiralAngle = 0f;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            int burstCount = Mathf.Max(1, skillData.BurstCount);

            for (int burst = 0; burst < burstCount; burst++)
            {
                await FireBurst(cancelToken);

                if (burst < burstCount - 1 && skillData.BurstInterval > 0f)
                    await UniTask.Delay((int)(skillData.BurstInterval * 1000f), cancellationToken: cancelToken);
            }
        }

        protected override void OnSkillExit()
        {
            if (!_releaseProjectilesOnExit) return;

            ReleaseAllActiveProjectiles();
            _releaseProjectilesOnExit = false;
        }

        public override void CancelSkill(int milliSecondDelay = 0)
        {
            _releaseProjectilesOnExit = true;
            base.CancelSkill(milliSecondDelay);
        }

        private async UniTask FireBurst(CancellationToken cancelToken)
        {
            var angles = BuildShotAngles(GetBaseAngleDegrees());

            for (int i = 0; i < angles.Count; i++)
            {
                SpawnProjectile(AngleToDirection(angles[i]));

                if (i < angles.Count - 1 && skillData.ProjectileInterval > 0f)
                    await UniTask.Delay((int)(skillData.ProjectileInterval * 1000f), cancellationToken: cancelToken);
            }
        }

        private List<float> BuildShotAngles(float baseAngle)
        {
            int count = Mathf.Max(1, skillData.ProjectileCount);
            var angles = new List<float>(count);

            switch (skillData.Pattern)
            {
                case SkillEnemyProjectileDataSo.FirePattern.Single:
                    angles.Add(baseAngle + skillData.AngleOffsetDegrees);
                    break;

                case SkillEnemyProjectileDataSo.FirePattern.Shotgun:
                    if (count == 1)
                    {
                        angles.Add(baseAngle + skillData.AngleOffsetDegrees);
                        break;
                    }

                    float start = -skillData.SpreadAngleDegrees * 0.5f;
                    float step = skillData.SpreadAngleDegrees / (count - 1);
                    for (int i = 0; i < count; i++)
                        angles.Add(baseAngle + skillData.AngleOffsetDegrees + start + step * i);
                    break;

                case SkillEnemyProjectileDataSo.FirePattern.Ring:
                    float arc = Mathf.Max(0f, skillData.RingArcDegrees);
                    float ringStep = arc >= 360f ? arc / count : count > 1 ? arc / (count - 1) : 0f;
                    float ringStart = arc >= 360f ? 0f : -arc * 0.5f;
                    for (int i = 0; i < count; i++)
                        angles.Add(baseAngle + skillData.AngleOffsetDegrees + ringStart + ringStep * i);
                    break;

                case SkillEnemyProjectileDataSo.FirePattern.Spiral:
                    for (int i = 0; i < count; i++)
                    {
                        angles.Add(baseAngle + skillData.AngleOffsetDegrees + _spiralAngle);
                        _spiralAngle += skillData.SpiralStepDegrees;
                    }
                    break;
            }

            return angles;
        }

        private float GetBaseAngleDegrees()
        {
            Vector2 direction = skillData.ProjectileAimMode switch
            {
                SkillEnemyProjectileDataSo.AimMode.TargetPlayer => DirectionToPlayer(),
                SkillEnemyProjectileDataSo.AimMode.SightDirection => aimDirection.direction,
                SkillEnemyProjectileDataSo.AimMode.FixedLocalDirection => owner.transform.TransformDirection(skillData.FixedDirection),
                _ => skillData.FixedDirection
            };

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private Vector2 DirectionToPlayer()
        {
            var player = PlayerController.Instance;
            if (!player)
                return aimDirection.direction;

            return (player.transform.position - owner.transform.position).normalized;
        }

        private static Vector2 AngleToDirection(float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void SpawnProjectile(Vector2 direction)
        {
            if (string.IsNullOrEmpty(PoolKey)) return;

            var projectile = PoolingManager.Instance.Get<EnemyProjectileSkillObject>(PoolKey);
            if (!projectile)
            {
                Debug.LogWarning($"[EnemyProjectile] Pool '{PoolKey}' is missing.");
                return;
            }

            projectile.OnReleaseRequested -= ReleaseProjectile;
            projectile.OnReleaseRequested += ReleaseProjectile;
            _activeProjectiles.Add(projectile);

            Vector2 spawnPosition = owner.transform.TransformPoint(skillData.SpawnLocalOffset);
            projectile.Launch(new EnemyProjectileSkillObject.LaunchPayload(
                owner.gameObject,
                owner.CharacterData.CharacterId,
                spawnPosition,
                direction,
                skillData.ProjectileSpeed,
                skillData.ProjectileLifetime,
                skillData.HitShape,
                skillData.HitboxSize,
                skillData.HitboxRadius,
                ResolveTargetLayer(),
                skillData.DamageHitPerSecond,
                skillData.BaseDamagePerHit,
                skillData.DamageMultiplier,
                skillData.MaxHits,
                skillData.ReleaseOnHit,
                skillData.RotateProjectileToDirection,
                skillData.ProjectileRotationOffsetDegrees,
                skillData.CanHitWithDamageOnTouch));
        }

        private LayerMask ResolveTargetLayer()
        {
            if (skillData.OverrideTargetLayer)
                return skillData.TargetLayer;

            var settings = CharacterGlobalSettings.Instance;
            if (settings && settings.EnemyLayerDictionary.TryGetValue(owner.tag, out var mask))
                return mask;

            Debug.LogWarning($"[EnemyProjectile] No target layer configured for tag '{owner.tag}'.");
            return default;
        }

        private void ReleaseProjectile(EnemyProjectileSkillObject projectile)
        {
            if (!projectile) return;

            projectile.OnReleaseRequested -= ReleaseProjectile;
            _activeProjectiles.Remove(projectile);
            PoolingManager.Current?.Release(PoolKey, projectile);
        }

        private void OnPoolRelease(EnemyProjectileSkillObject projectile)
        {
            if (!projectile) return;

            projectile.OnReleaseRequested -= ReleaseProjectile;
            projectile.gameObject.SetActive(false);
        }

        private void ReleaseAllActiveProjectiles()
        {
            foreach (var projectile in new List<EnemyProjectileSkillObject>(_activeProjectiles))
                projectile?.ForceRelease();

            _activeProjectiles.Clear();
        }

        private EnemyProjectileSkillObject CreatePoolInstance()
        {
            var obj = Instantiate(skillData.ProjectilePrefab);
            obj.gameObject.SetActive(false);
            return obj;
        }

        private void OnDestroy()
        {
            ReleaseAllActiveProjectiles();
        }
    }
}
