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
    /// Fires several orbs, each launched toward the direction of a different farthest-in-range enemy
    /// (no two orbs share a target). Every orb then flies in a straight line at a constant speed for its
    /// whole lifetime (fire-and-forget, it does not keep homing) - while alive it continuously pulls
    /// nearby enemies within <c>PullRadius</c> toward itself and locks their Primary/Secondary skills
    /// (see <see cref="Characters.StatusEffectSystems.StatusEffects.GravityPulledEffect"/>). When its
    /// lifetime ends it explodes wherever it currently is, dealing AOE damage within <c>ExplosionRadius</c>
    /// - a separate value from the pull radius, so the two can be tuned independently.
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
        [PropertyTooltip("Number of orbs fired per cast. Each orb is launched toward a different enemy's direction - if fewer valid enemies exist than this, fewer orbs are fired.")]
        [MinValue(1)]
        [SerializeField] private int orbCount = 3;

        // ─────────────── Targeting ───────────────

        [FoldoutGroup("Targeting")]
        [LabelText("Target Search Radius")]
        [PropertyTooltip("Range (from the caster) to search for enemies. Each orb is launched toward the direction of the farthest still-unclaimed enemy within this range.")]
        [SerializeField] private float targetSearchRadius = 8f;

        [FoldoutGroup("Targeting")]
        [LabelText("Orb Speed")]
        [PropertyTooltip("Constant speed the orb flies at, in a straight line, for its entire lifetime. The direction is locked in at launch (toward its assigned target) and does not keep homing afterward.")]
        [SerializeField] private float orbSpeed = 14f;

        // ─────────────── Gravity Field ───────────────

        [FoldoutGroup("Gravity Field")]
        [LabelText("Pull Radius")]
        [PropertyTooltip("Radius around the orb's current position that continuously pulls enemies in while it flies. Independent from Explosion Radius.")]
        [SerializeField] private float pullRadius = 3.5f;

        [FoldoutGroup("Gravity Field")]
        [LabelText("Orb Lifetime (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("How long the orb flies and pulls enemies before it explodes.")]
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
        [PropertyTooltip("Pull strength multiplier based on proximity to the orb (0 = at Pull Radius edge, 1 = at center).")]
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
        [LabelText("Explosion Radius")]
        [PropertyTooltip("AOE radius around the orb's final position when its lifetime ends. Independent from Pull Radius.")]
        [SerializeField] private float explosionRadius = 3.5f;

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
        public float OrbSpeed => orbSpeed;

        public float PullRadius => pullRadius;
        public float OrbLifeTime => orbLifeTime;
        public float PullStrength => pullStrength;
        public float PullStopDistance => pullStopDistance;
        public AnimationCurve PullCurve => pullCurve;
        public bool UseExponentialPull => useExponentialPull;

        public List<StatusEffectDataPayload> PulledStatusEffects => pulledStatusEffects;

        public float ExplosionRadius => explosionRadius;
        public float ExplosionBaseDamage => explosionBaseDamage;
        public float ExplosionDamageMultiplier => explosionDamageMultiplier;

        public string ExplodeFeedback =>
            string.IsNullOrEmpty(explodeFeedback) ? null : FeedbackName.ResolveFullKey("Skill", explodeFeedback);
    }
}
