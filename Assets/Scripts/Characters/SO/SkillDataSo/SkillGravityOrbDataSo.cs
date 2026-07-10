using System.Collections.Generic;
using Characters.FeedbackSystems;
using Characters.SkillSystems.SkillObjects;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Data for the Gravity Orb auto skill.
    /// Fires several orbs that each fly to the farthest-in-range enemy that hasn't already been
    /// targeted by another orb this cast, and stop there. While alive, an orb continuously pulls
    /// nearby enemies toward itself and locks their Primary/Secondary skills (see
    /// <see cref="Characters.StatusEffectSystems.StatusEffects.GravityPulledEffect"/>). When its
    /// lifetime ends it explodes, dealing AOE damage in the same radius it was pulling from.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillGravityOrbData", menuName = "GameData/SkillData/SkillGravityOrbData")]
    public class SkillGravityOrbDataSo : BaseSkillDataSo
    {
        // ─────────────── Skill Object ───────────────

        [FoldoutGroup("Skill Object")]
        [LabelText("Orb Prefab")]
        [SerializeField] [Required]
        private GravityOrbSkillObject orbPrefab;

        [FoldoutGroup("Skill Object")]
        [LabelText("Orb Count (N)")]
        [PropertyTooltip("Number of orbs fired per cast. Each orb targets a different enemy - if fewer valid enemies exist than this, fewer orbs are fired.")]
        [MinValue(1)]
        [SerializeField] private int orbCount = 3;

        // ─────────────── Targeting ───────────────

        [FoldoutGroup("Targeting")]
        [LabelText("Target Search Radius")]
        [PropertyTooltip("Range (from the caster) to search for enemies. Each orb is assigned to the farthest still-unclaimed enemy within this range.")]
        [SerializeField] private float targetSearchRadius = 8f;

        [FoldoutGroup("Targeting")]
        [LabelText("Travel Speed")]
        [PropertyTooltip("How fast an orb flies from the caster to its assigned enemy's position.")]
        [SerializeField] private float travelSpeed = 14f;

        [FoldoutGroup("Targeting")]
        [LabelText("Travel Ease Curve")]
        [SerializeField] private AnimationCurve travelEaseCurve;

        [FoldoutGroup("Targeting")]
        [LabelText("Travel Move Curve")]
        [PropertyTooltip("Optional lateral offset curve applied while the orb travels to its stop position.")]
        [SerializeField] private AnimationCurve travelMoveCurve;

        // ─────────────── Gravity Field ───────────────

        [FoldoutGroup("Gravity Field")]
        [LabelText("Orb Effect Radius (N)")]
        [PropertyTooltip("Radius around the orb's stop position. Used both for the continuous pull field while alive and for the explosion damage radius on death.")]
        [SerializeField] private float orbEffectRadius = 3.5f;

        [FoldoutGroup("Gravity Field")]
        [LabelText("Orb Lifetime (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("How long the orb sits and pulls enemies before it explodes.")]
        [SerializeField] private float orbLifeTime = 3f;

        [FoldoutGroup("Gravity Field")]
        [LabelText("Pull Strength")]
        [SerializeField] private float pullStrength = 8f;

        [FoldoutGroup("Gravity Field")]
        [LabelText("Pull Stop Distance")]
        [PropertyTooltip("Enemies stop being pulled closer once within this distance of the orb's center.")]
        [SerializeField] private float pullStopDistance = 0.2f;

        [FoldoutGroup("Gravity Field")]
        [LabelText("Pull Falloff Curve")]
        [PropertyTooltip("Pull strength multiplier based on proximity to the orb (0 = at Orb Effect Radius edge, 1 = at center).")]
        [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [FoldoutGroup("Gravity Field")]
        [LabelText("Use Exponential Pull")]
        [SerializeField] private bool useExponentialPull = true;

        // ─────────────── Status Effects ───────────────

        [FoldoutGroup("Status Effects")]
        [PropertyTooltip("Status effect(s) applied to an enemy for as long as it's being pulled (e.g. GravityPulledEffectData). Applied when the enemy enters the pull field and removed when it leaves or the orb ends - use a large Override Duration on the payload so it can't expire early.")]
        [SerializeField] private List<StatusEffectDataPayload> pulledStatusEffects;

        // ─────────────── Explosion Damage ───────────────

        [FoldoutGroup("Explosion Damage")]
        [SerializeField] private float explosionBaseDamage = 15f;

        [FoldoutGroup("Explosion Damage")]
        [Unit(Units.Percent)]
        [SerializeField] private float explosionDamageMultiplier = 100f;

        [FoldoutGroup("Feedback")]
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")]
        [SerializeField] private string explodeFeedback;

        // ─────────────── Properties ───────────────

        public GravityOrbSkillObject OrbPrefab => orbPrefab;
        public int OrbCount => orbCount;

        public float TargetSearchRadius => targetSearchRadius;
        public float TravelSpeed => travelSpeed;
        public AnimationCurve TravelEaseCurve => travelEaseCurve;
        public AnimationCurve TravelMoveCurve => travelMoveCurve;

        public float OrbEffectRadius => orbEffectRadius;
        public float OrbLifeTime => orbLifeTime;
        public float PullStrength => pullStrength;
        public float PullStopDistance => pullStopDistance;
        public AnimationCurve PullCurve => pullCurve;
        public bool UseExponentialPull => useExponentialPull;

        public List<StatusEffectDataPayload> PulledStatusEffects => pulledStatusEffects;

        public float ExplosionBaseDamage => explosionBaseDamage;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;

        public string ExplodeFeedback =>
            string.IsNullOrEmpty(explodeFeedback) ? null : FeedbackName.ResolveFullKey("Skill", explodeFeedback);
    }
}
