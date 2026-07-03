using Characters.CombatSystems;
using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillEnemyProjectileData", menuName = "GameData/SkillData/EnemyProjectile")]
    public class SkillEnemyProjectileDataSo : BaseSkillDataSo
    {
        public enum FirePattern
        {
            Single,
            Shotgun,
            Ring,
            Spiral
        }

        public enum AimMode
        {
            TargetPlayer,
            SightDirection,
            FixedWorldDirection,
            FixedLocalDirection
        }

        [FoldoutGroup("Projectile")]
        [SerializeField, Required]
        private EnemyProjectileSkillObject projectilePrefab;

        [FoldoutGroup("Projectile")]
        [SerializeField, MinValue(0)]
        private int prewarmCount = 24;

        [FoldoutGroup("Projectile")]
        [SerializeField, MinValue(0.01f)]
        private float projectileSpeed = 8f;

        [FoldoutGroup("Projectile")]
        [SerializeField, MinValue(0.05f), Unit(Units.Second)]
        private float projectileLifetime = 4f;

        [FoldoutGroup("Projectile")]
        [SerializeField]
        private bool rotateProjectileToDirection = true;

        [FoldoutGroup("Projectile")]
        [SerializeField]
        private float projectileRotationOffsetDegrees;

        [FoldoutGroup("Fire Setup")]
        [SerializeField]
        private FirePattern firePattern = FirePattern.Single;

        [FoldoutGroup("Fire Setup")]
        [SerializeField]
        private AimMode aimMode = AimMode.TargetPlayer;

        [FoldoutGroup("Fire Setup")]
        [SerializeField]
        private Vector2 fixedDirection = Vector2.right;

        [FoldoutGroup("Fire Setup")]
        [SerializeField]
        private Vector2 spawnLocalOffset;

        [FoldoutGroup("Fire Setup")]
        [SerializeField]
        private float angleOffsetDegrees;

        [FoldoutGroup("Pattern")]
        [SerializeField, MinValue(1)]
        private int projectileCount = 1;

        [FoldoutGroup("Pattern")]
        [SerializeField, MinValue(0f)]
        private float spreadAngleDegrees = 30f;

        [FoldoutGroup("Pattern")]
        [SerializeField, MinValue(0f)]
        private float ringArcDegrees = 360f;

        [FoldoutGroup("Pattern")]
        [SerializeField]
        private float spiralStepDegrees = 15f;

        [FoldoutGroup("Pattern")]
        [SerializeField]
        private bool resetSpiralOnCast = true;

        [FoldoutGroup("Burst")]
        [SerializeField, MinValue(1)]
        private int burstCount = 1;

        [FoldoutGroup("Burst")]
        [SerializeField, MinValue(0f), Unit(Units.Second)]
        private float burstInterval = 0.1f;

        [FoldoutGroup("Burst")]
        [SerializeField, MinValue(0f), Unit(Units.Second)]
        private float projectileInterval = 0f;

        [FoldoutGroup("Damage")]
        [SerializeField, MinValue(0.01f)]
        private float damageHitPerSecond = 30f;

        [FoldoutGroup("Damage")]
        [SerializeField, MinValue(0f)]
        private float baseDamagePerHit;

        [FoldoutGroup("Damage")]
        [SerializeField, Unit(Units.Percent)]
        private float damageMultiplier = 100f;

        [FoldoutGroup("Damage")]
        [SerializeField]
        private bool releaseOnHit = true;

        [FoldoutGroup("Damage")]
        [SerializeField, MinValue(1)]
        private int maxHits = 1;

        [FoldoutGroup("Damage")]
        [SerializeField]
        private bool canHitWithDamageOnTouch;

        [FoldoutGroup("Hitbox")]
        [SerializeField]
        private DamageOnTouch.OverlapShape hitShape = DamageOnTouch.OverlapShape.Circle;

        [FoldoutGroup("Hitbox")]
        [SerializeField]
        private Vector2 hitboxSize = Vector2.one;

        [FoldoutGroup("Hitbox")]
        [SerializeField, MinValue(0.01f)]
        private float hitboxRadius = 0.25f;

        [FoldoutGroup("Hitbox")]
        [SerializeField]
        private bool overrideTargetLayer;

        [FoldoutGroup("Hitbox")]
        [ShowIf(nameof(overrideTargetLayer))]
        [SerializeField]
        private LayerMask targetLayer;

        public EnemyProjectileSkillObject ProjectilePrefab => projectilePrefab;
        public int PrewarmCount => prewarmCount;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public bool RotateProjectileToDirection => rotateProjectileToDirection;
        public float ProjectileRotationOffsetDegrees => projectileRotationOffsetDegrees;
        public FirePattern Pattern => firePattern;
        public AimMode ProjectileAimMode => aimMode;
        public Vector2 FixedDirection => fixedDirection;
        public Vector2 SpawnLocalOffset => spawnLocalOffset;
        public float AngleOffsetDegrees => angleOffsetDegrees;
        public int ProjectileCount => projectileCount;
        public float SpreadAngleDegrees => spreadAngleDegrees;
        public float RingArcDegrees => ringArcDegrees;
        public float SpiralStepDegrees => spiralStepDegrees;
        public bool ResetSpiralOnCast => resetSpiralOnCast;
        public int BurstCount => burstCount;
        public float BurstInterval => burstInterval;
        public float ProjectileInterval => projectileInterval;
        public float DamageHitPerSecond => damageHitPerSecond;
        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;
        public bool ReleaseOnHit => releaseOnHit;
        public int MaxHits => maxHits;
        public bool CanHitWithDamageOnTouch => canHitWithDamageOnTouch;
        public DamageOnTouch.OverlapShape HitShape => hitShape;
        public Vector2 HitboxSize => hitboxSize;
        public float HitboxRadius => hitboxRadius;
        public bool OverrideTargetLayer => overrideTargetLayer;
        public LayerMask TargetLayer => targetLayer;
    }
}
