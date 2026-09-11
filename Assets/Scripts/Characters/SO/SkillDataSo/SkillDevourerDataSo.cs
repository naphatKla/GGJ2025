using System.Collections.Generic;
using Characters.FeedbackSystems;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Data for Bright2's primary skill "Devourer" (Design Ver 0.0.20) - the boss acts as a living black
    /// hole: it drags everything around it inward and grinds it down for a set duration.
    /// <para/>
    /// The skill runs as a small state machine: <b>Pull</b> -> (phase-3 variant only) <b>Flee</b> ->
    /// <b>Explode</b>. Phase gating is not stored here - <c>Bright2Controller</c> assigns a different
    /// Devourer asset per phase, so the phase-3 variant is just this same type with
    /// <see cref="EnableFleeAndExplode"/> ticked.
    /// <para/>
    /// Devourer is also the only skill that can set off a <b>Special Interaction</b>: anything inside its
    /// damage radius implementing <c>ISpecialInteractionTarget</c> (Black Hole, Border pieces) is consumed
    /// and reacts with its own effect. There is no whitelist to configure here - implementing that
    /// interface is the opt-in, and the reaction's numbers live on the target.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDevourerData", menuName = "GameData/SkillData/TheBright2Only/Devourer")]
    public class SkillDevourerDataSo : BaseSkillDataSo
    {
        // ---------------- State: Pull ----------------

        [FoldoutGroup("State: Pull")]
        [LabelText("Pull Duration")]
        [Unit(Units.Second), MinValue(0f)]
        [PropertyTooltip("How long the boss keeps pulling and grinding. Spec: 5s")]
        [SerializeField] private float pullDuration = 5f;

        [FoldoutGroup("State: Pull")]
        [LabelText("Pull Radius")]
        [PropertyTooltip("Range that drags characters inward. Spec: 90 - much wider than the damage radius, "
                         + "so things get dragged in long before they start taking damage.")]
        [SerializeField] private float pullRadius = 90f;

        [FoldoutGroup("State: Pull")]
        [LabelText("Pull Force")]
        [PropertyTooltip("Spec: 30")]
        [SerializeField] private float pullStrength = 30f;

        [FoldoutGroup("State: Pull")]
        [LabelText("Pull Stop Distance")]
        [PropertyTooltip("Targets stop being dragged closer once within this distance of the boss.")]
        [SerializeField] private float pullStopDistance = 0.2f;

        [FoldoutGroup("State: Pull")]
        [LabelText("Pull Falloff Curve")]
        [PropertyTooltip("Pull multiplier by proximity (0 = at the pull radius edge, 1 = at the centre).")]
        [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [FoldoutGroup("State: Pull")]
        [LabelText("Use Exponential Pull")]
        [SerializeField] private bool useExponentialPull = true;

        [FoldoutGroup("State: Pull")]
        [LabelText("Max Pull Speed")]
        [MinValue(0f)]
        [PropertyTooltip("Hard cap on how fast the pull may drag a target, in units per second. 0 = uncapped. "
                         + "The spec's Pull Force 30 is an exponential RATE, not a speed - uncapped it drags "
                         + "targets hundreds of units per second and nothing can ever escape. "
                         + "Player base speed is 10, so a cap slightly above that still stops anyone from "
                         + "simply walking out while leaving dashes a way through.")]
        [SerializeField] private float maxPullSpeed = 12f;

        [FoldoutGroup("State: Pull")]
        [LabelText("Ignore Dashing Targets")]
        [PropertyTooltip("Leave targets alone while they have an active movement tween (a dash). The pull "
                         + "writes raw positions every FixedUpdate, so without this it overwrites the dash "
                         + "frame by frame and dashing out is impossible no matter how weak the pull is.")]
        [SerializeField] private bool ignoreDashingTargets = true;

        [FoldoutGroup("State: Pull")]
        [LabelText("Immune To External Pull")]
        [PropertyTooltip("While channelling, the owner ignores other pull effects. Without this a Black Hole "
                         + "drags the boss in far faster than Devourer drags the Black Hole (map event pulls "
                         + "have no speed cap), so the boss visibly loses the tug-of-war against the thing it "
                         + "is supposed to be swallowing.")]
        [SerializeField] private bool immuneToExternalPull = true;

        // ---------------- Pull Damage ----------------

        [FoldoutGroup("Pull Damage")]
        [LabelText("Damage Radius")]
        [PropertyTooltip("Inner radius that actually deals damage. Spec: 35")]
        [SerializeField] private float damageRadius = 35f;

        [FoldoutGroup("Pull Damage")]
        [LabelText("Special Interaction Radius")]
        [MinValue(0f)]
        [PropertyTooltip("How close a Black Hole / Border piece must be dragged before it is consumed. The doc "
                         + "ties this to the Damage Radius (35), but Black Holes spawn near the player, who is "
                         + "usually already inside 35 of the boss - so they vanished the instant they appeared "
                         + "and the pull was never visible. Keep this small so the drag can be seen.")]
        [SerializeField] private float specialInteractionRadius = 10f;

        [FoldoutGroup("Pull Damage")]
        [LabelText("Hit / Second")]
        [PropertyTooltip("Spec: 1")]
        [MinValue(0.01f)]
        [SerializeField] private float hitPerSecond = 1f;

        [FoldoutGroup("Pull Damage")]
        [LabelText("Base Damage / Hit")]
        [PropertyTooltip("Flat damage added on top of the multiplier. Spec uses 0 - all of Devourer's "
                         + "damage comes from the multiplier below.")]
        [SerializeField] private float baseDamagePerHit;

        [FoldoutGroup("Pull Damage")]
        [LabelText("Damage Multiplier")]
        [Unit(Units.Percent)]
        [PropertyTooltip("Percent of the owner's base damage per hit. Spec: 300% (= 15 at base damage 5)")]
        [SerializeField] private float damageMultiplier = 300f;

        // ---------------- Status Effects ----------------

        [FoldoutGroup("Status Effects")]
        [PropertyTooltip("Applied to a target for as long as it is inside the pull field, removed when it "
                         + "leaves or the skill ends. Use a large Override Duration so it cannot expire early.")]
        [SerializeField] private List<StatusEffectDataPayload> pulledStatusEffects;

        // ---------------- Phase-3 Variant ----------------

        [FoldoutGroup("Phase 3 Variant")]
        [LabelText("Enable Flee & Explode")]
        [PropertyTooltip("Phase-3 Devourer only: instead of just ending, the boss stops pulling, flees the "
                         + "player briefly, then blows up its surroundings.")]
        [SerializeField] private bool enableFleeAndExplode;

        [FoldoutGroup("Phase 3 Variant")]
        [ShowIf(nameof(enableFleeAndExplode))]
        [LabelText("Flee Duration")]
        [Unit(Units.Second)]
        [PropertyTooltip("Spec: 1s")]
        [SerializeField] private float fleeDuration = 1f;

        [FoldoutGroup("Phase 3 Variant")]
        [ShowIf(nameof(enableFleeAndExplode))]
        [LabelText("Flee Distance")]
        [PropertyTooltip("How far the boss backs away from the player during the flee state.")]
        [SerializeField] private float fleeDistance = 30f;

        [FoldoutGroup("Phase 3 Variant")]
        [ShowIf(nameof(enableFleeAndExplode))]
        [LabelText("Explode Radius")]
        [PropertyTooltip("Spec: 60")]
        [SerializeField] private float explodeRadius = 60f;

        [FoldoutGroup("Phase 3 Variant")]
        [ShowIf(nameof(enableFleeAndExplode))]
        [LabelText("Explode Base Damage")]
        [SerializeField] private float explodeBaseDamage;

        [FoldoutGroup("Phase 3 Variant")]
        [ShowIf(nameof(enableFleeAndExplode))]
        [LabelText("Explode Damage Multiplier")]
        [Unit(Units.Percent)]
        [PropertyTooltip("Spec: 800% (= 40 at base damage 5)")]
        [SerializeField] private float explodeDamageMultiplier = 800f;

        [FoldoutGroup("Feedback")]
        [ValueDropdown("@FeedbackName.Odin.ShortGroupWithNone(\"Skill\")")]
        [SerializeField] private string explodeFeedback;

        // ---------------- Properties ----------------

        public float PullDuration => pullDuration;
        public float PullRadius => pullRadius;
        public float PullStrength => pullStrength;
        public float PullStopDistance => pullStopDistance;
        public AnimationCurve PullCurve => pullCurve;
        public bool UseExponentialPull => useExponentialPull;
        public float MaxPullSpeed => maxPullSpeed;
        public bool IgnoreDashingTargets => ignoreDashingTargets;
        public bool ImmuneToExternalPull => immuneToExternalPull;

        public float DamageRadius => damageRadius;
        public float SpecialInteractionRadius => specialInteractionRadius;
        public float HitPerSecond => hitPerSecond;
        public float BaseDamagePerHit => baseDamagePerHit;
        public float DamageMultiplier => damageMultiplier;

        public List<StatusEffectDataPayload> PulledStatusEffects => pulledStatusEffects;

        public bool EnableFleeAndExplode => enableFleeAndExplode;
        public float FleeDuration => fleeDuration;
        public float FleeDistance => fleeDistance;
        public float ExplodeRadius => explodeRadius;
        public float ExplodeBaseDamage => explodeBaseDamage;
        public float ExplodeDamageMultiplier => explodeDamageMultiplier;

        public string ExplodeFeedback =>
            string.IsNullOrEmpty(explodeFeedback) ? null : FeedbackName.ResolveFullKey("Skill", explodeFeedback);
    }
}
