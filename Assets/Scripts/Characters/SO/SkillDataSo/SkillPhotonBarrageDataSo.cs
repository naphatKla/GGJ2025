using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillPhotonBarrageData", menuName = "GameData/SkillData/SkillPhotonBarrageData")]
    public class SkillPhotonBarrageDataSo : BaseSkillDataSo
    {
        // ─────────────── Skill Object ───────────────

        [FoldoutGroup("Skill Object")]
        [LabelText("Missile Count")]
        [PropertyTooltip("Number of missiles spawned per cast.")]
        [SerializeField]
        private int missileCount = 8;

        [FoldoutGroup("Skill Object")] [LabelText("Missile Prefab")] [SerializeField] [Required]
        private PhotonBarrageSkillObject missilePrefab;

        // ─────────────── Damage ───────────────

        [FoldoutGroup("Damage")] [SerializeField]
        private float damageHitPerSec = 5f;

        [FoldoutGroup("Damage")] [SerializeField]
        private float baseDamagePerHit = 8f;

        [FoldoutGroup("Damage")] [Unit(Units.Percent)] [SerializeField]
        private float damageMultiplier = 100f;

        // ─────────────── Spread Phase ───────────────

        [FoldoutGroup("Photon Barrage")]
        [LabelText("Spread Distance")]
        [PropertyTooltip("How far each missile travels outward from the caster.")]
        [SerializeField]
        private float spreadDistance = 3f;

        [FoldoutGroup("Photon Barrage/Spread Phase")]
        [LabelText("Spread Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time for all missiles to finish spreading outward.")]
        [SerializeField]
        private float spreadEntireDuration = 0.4f;

        [FoldoutGroup("Photon Barrage/Spread Phase")]
        [LabelText("Stagger Duration (sec)")]
        [Unit(Units.Second)]
        [ValidateInput("@spreadStaggerDuration < spreadEntireDuration",
            "Stagger must be less than entire spread duration.")]
        [SerializeField]
        private float spreadStaggerDuration = 0.2f;

        [FoldoutGroup("Photon Barrage/Spread Phase")] [LabelText("Stagger Curve")] [SerializeField]
        private AnimationCurve spreadStaggerCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [FoldoutGroup("Photon Barrage/Spread Phase")] [LabelText("Ease Curve")] [SerializeField]
        private AnimationCurve spreadEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [FoldoutGroup("Photon Barrage/Spread Phase")] [LabelText("Move Curve (lateral offset)")] [SerializeField]
        private AnimationCurve spreadMoveCurve;

        // ─────────────── Homing Phase ───────────────

        [FoldoutGroup("Photon Barrage/Homing Phase")] [LabelText("Detection Radius")] [SerializeField]
        private float homingDetectionRadius = 15f;

        [FoldoutGroup("Photon Barrage/Homing Phase")]
        [LabelText("Missile Speed")]
        [PropertyTooltip("Travel speed of each missile during homing.")]
        [SerializeField]
        private float homingSpeed = 18f;

        [FoldoutGroup("Photon Barrage/Homing Phase")]
        [LabelText("Max Homing Lifetime (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("If a missile hasn't hit anything after this time, it despawns.")]
        [SerializeField]
        private float homingMaxLifetime = 3f;

        [FoldoutGroup("Photon Barrage/Homing Phase")]
        [LabelText("Turn Speed (deg/sec)")]
        [PropertyTooltip("How fast the missile can turn toward its target per second. Higher = tighter tracking.")]
        [SerializeField]
        private float homingTurnSpeed = 360f;

        [FoldoutGroup("Photon Barrage/Homing Phase")]
        [LabelText("Arc Strength")]
        [PropertyTooltip(
            "Perpendicular offset strength for the initial arc curve. Decays over time for a satisfying 'swoop' effect.")]
        [SerializeField]
        private float homingArcStrength = 2f;

        [FoldoutGroup("Photon Barrage/Homing Phase")]
        [LabelText("Arc Decay Curve")]
        [PropertyTooltip(
            "How the arc offset decays over normalised lifetime (0→1). Typically starts at 1 and drops to 0.")]
        [SerializeField]
        private AnimationCurve homingArcDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [FoldoutGroup("Photon Barrage/Homing Phase")]
        [LabelText("Stagger Duration (sec)")]
        [Unit(Units.Second)]
        [SerializeField]
        private float homingStaggerDuration = 0.3f;

        [FoldoutGroup("Photon Barrage/Homing Phase")] [LabelText("Stagger Curve")] [SerializeField]
        private AnimationCurve homingStaggerCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [FoldoutGroup("Photon Barrage/Homing Phase")] [LabelText("No-Target Fly Distance")] [SerializeField]
        private float noTargetFlyDistance = 6f;

        // ─────────────── Properties ───────────────

        public int MissileCount => missileCount;
        public PhotonBarrageSkillObject MissilePrefab => missilePrefab;

        public float DamageHitPerSec => damageHitPerSec;
        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;

        public float SpreadDistance => spreadDistance;
        public float SpreadEntireDuration => spreadEntireDuration;
        public float SpreadStaggerDuration => spreadStaggerDuration;
        public AnimationCurve SpreadStaggerCurve => spreadStaggerCurve;
        public AnimationCurve SpreadEaseCurve => spreadEaseCurve;
        public AnimationCurve SpreadMoveCurve => spreadMoveCurve;

        public float HomingDetectionRadius => homingDetectionRadius;
        public float HomingSpeed => homingSpeed;
        public float HomingMaxLifetime => homingMaxLifetime;
        public float HomingTurnSpeed => homingTurnSpeed;
        public float HomingArcStrength => homingArcStrength;
        public AnimationCurve HomingArcDecayCurve => homingArcDecayCurve;
        public float HomingStaggerDuration => homingStaggerDuration;
        public AnimationCurve HomingStaggerCurve => homingStaggerCurve;
        public float NoTargetFlyDistance => noTargetFlyDistance;
    }
}