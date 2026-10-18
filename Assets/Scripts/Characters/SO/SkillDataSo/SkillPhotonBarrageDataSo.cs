using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillPhotonBarrageData", menuName = "GameData/SkillData/SkillPhotonBarrageData")]
    public class SkillPhotonBarrageDataSo : BaseSkillDataSo
    {
        // ───────── Skill Object ─────────
        [FoldoutGroup("Skill Object")] [SerializeField]
        private int missileCount = 8;

        [FoldoutGroup("Skill Object")] [SerializeField] [Required]
        private PhotonBarrageSkillObject missilePrefab;

        // ───────── Damage ─────────
        [FoldoutGroup("Damage")] [SerializeField]
        private float damageHitPerSec = 5f;

        [FoldoutGroup("Damage")] [SerializeField]
        private float baseDamagePerHit = 8f;

        [FoldoutGroup("Damage")] [Unit(Units.Percent)] [SerializeField]
        private float damageMultiplier = 100f;

        // ───────── Launch (ไม่มี damage / ไม่ล็อกเป้า) ─────────
        [FoldoutGroup("Launch Phase")] [LabelText("Launch Duration (sec)")] [Unit(Units.Second)]
        [PropertyTooltip("ช่วงที่กระสุนพุ่งออกรอบตัว ยังไม่ทำดาเมจและยังไม่ล็อกเป้า จบช่วงนี้ถึงเปิด damage")]
        [ValidateInput("@launchDuration < maxFlightLifetime", "ต้องน้อยกว่า Max Flight Lifetime")]
        [SerializeField]
        private float launchDuration = 0.4f;

        [FoldoutGroup("Launch Phase")] [SerializeField]
        private float launchSpeed = 12f;

        [FoldoutGroup("Launch Phase")] [LabelText("Launch Speed Curve (0→1 ของ launch)")] [SerializeField]
        private AnimationCurve launchSpeedCurve = AnimationCurve.Linear(0, 1, 1, 0.2f);

        [FoldoutGroup("Launch Phase")] [LabelText("Angle Jitter (deg)")] [SerializeField]
        private float launchAngleJitter = 10f;

        // ───────── Seek / Rocket flight ─────────
        [FoldoutGroup("Flight Phase")] [LabelText("Max Flight Lifetime (sec)")] [Unit(Units.Second)]
        [PropertyTooltip("อายุกระสุนรวมนับตั้งแต่ปล่อย ถ้าไม่โดนอะไรจะหายเอง")]
        [SerializeField]
        private float maxFlightLifetime = 4f;

        [FoldoutGroup("Flight Phase")] [SerializeField]
        private float cruiseSpeed = 22f;

        [FoldoutGroup("Flight Phase")] [PropertyTooltip("ความเร่งจากความเร็วท้าย launch ไปถึง cruise speed")] [SerializeField]
        private float acceleration = 30f;

        [FoldoutGroup("Flight Phase")] [SerializeField]
        private float detectionRadius = 12f;

        [FoldoutGroup("Flight Phase")] [LabelText("Retarget Interval (sec)")] [SerializeField]
        private float retargetInterval = 0.15f;

        [FoldoutGroup("Flight Phase")] [LabelText("Allow Shared Targets")]
        [PropertyTooltip("ถ้าเป้าทุกตัวถูกจองหมดแล้ว ให้กระสุนไปซ้ำเป้าได้ไหม")]
        [SerializeField]
        private bool allowSharedTargets = true;

        [FoldoutGroup("Flight Phase")] [LabelText("Turn Speed (deg/sec)")] [SerializeField]
        private float turnSpeed = 240f;

        [FoldoutGroup("Flight Phase")] [LabelText("Turn Ramp Curve (0→1 ของช่วง seek)")]
        [PropertyTooltip("ตัวคูณ turn speed ตามเวลา ยิ่งนานยิ่งเลี้ยวแคบ กันกระสุนวนรอบเป้า")]
        [SerializeField]
        private AnimationCurve turnRampCurve = AnimationCurve.Linear(0, 1, 1, 3);

        [FoldoutGroup("Flight Phase")] [SerializeField]
        private float arcStrength = 2f;

        [FoldoutGroup("Flight Phase")] [LabelText("Arc Decay Curve (0→1 ของช่วง seek)")] [SerializeField]
        private AnimationCurve arcDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        // ───────── Properties ─────────
        public int MissileCount => missileCount;
        public PhotonBarrageSkillObject MissilePrefab => missilePrefab;
        public float DamageHitPerSec => damageHitPerSec;
        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;

        public float LaunchDuration => launchDuration;
        public float LaunchSpeed => launchSpeed;
        public AnimationCurve LaunchSpeedCurve => launchSpeedCurve;
        public float LaunchAngleJitter => launchAngleJitter;

        public float MaxFlightLifetime => maxFlightLifetime;
        public float CruiseSpeed => cruiseSpeed;
        public float Acceleration => acceleration;
        public float DetectionRadius => detectionRadius;
        public float RetargetInterval => retargetInterval;
        public bool AllowSharedTargets => allowSharedTargets;
        public float TurnSpeed => turnSpeed;
        public AnimationCurve TurnRampCurve => turnRampCurve;
        public float ArcStrength => arcStrength;
        public AnimationCurve ArcDecayCurve => arcDecayCurve;
    }
}