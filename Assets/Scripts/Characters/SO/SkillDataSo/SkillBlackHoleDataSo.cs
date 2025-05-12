using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Contains configuration for the Black Hole skill.
    /// Controls clone count, damage, and movement behaviors during the explosion and merge phases,
    /// including curves for easing, motion shaping, and per-clone delay staggering.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillBlackHoleData", menuName = "GameData/SkillData/SkillBlackHoleData")]
    public class SkillBlackHoleDataSo : BaseSkillDataSo
    {
        // ------------------ Clone Settings ------------------

        [BoxGroup("Clones", showLabel: true)]
        [LabelText("Clone Count")]
        [PropertyTooltip("Number of clones spawned during the black hole skill.")]
        [SerializeField] private int cloneAmount;

        [BoxGroup("Clones")]
        [LabelText("Clone Damage")]
        [PropertyTooltip("Damage each clone deals when interacting with enemies.")]
        [SerializeField] private float cloneDamage;

        // ------------------ Explosion Phase ------------------

        [BoxGroup("Explosion Phase", showLabel: true)]
        [LabelText("Explosion Distance")]
        [PropertyTooltip("Distance each clone travels outward during the explosion phase.")]
        [SerializeField] private float explosionDistance;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time for a clone to complete the explosion movement.")]
        [SerializeField] private float explosionDuration;

        [BoxGroup("Explosion Phase")]
        [LabelText("Delay Per Clone (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Base delay applied per clone index for staggered explosion timing.")]
        [SerializeField] private float explosionDelayPerClone;

        [BoxGroup("Explosion Phase")]
        [LabelText("Per-Clone Delay Curve")]
        [PropertyTooltip("AnimationCurve controlling how the delay scales across clone indices (normalized 0 to 1).")]
        [SerializeField] private AnimationCurve explosionPerCloneCurve;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Ease Curve")]
        [PropertyTooltip("Easing curve that controls clone speed during the explosion phase.")]
        [SerializeField] private AnimationCurve explosionEaseCurve;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Move Curve")]
        [PropertyTooltip("Curve to control curved or wave-like motion during explosion movement.")]
        [SerializeField] private AnimationCurve explosionMoveCurve;

        // ------------------ Merge Phase ------------------

        [Space]
        [BoxGroup("Merge Phase", showLabel: true)]
        [LabelText("Merge Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time for a clone to complete the merge (return) movement.")]
        [SerializeField] private float mergeDuration;

        [BoxGroup("Merge Phase")]
        [LabelText("Delay Per Clone (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Base delay applied per clone index for staggered merging.")]
        [SerializeField] private float mergeDelayPerClone;

        [BoxGroup("Merge Phase")]
        [LabelText("Per-Clone Delay Curve")]
        [PropertyTooltip("AnimationCurve controlling how the delay scales across clone indices (normalized 0 to 1).")]
        [SerializeField] private AnimationCurve mergePerCloneCurve;

        [BoxGroup("Merge Phase")]
        [LabelText("Merge Ease Curve")]
        [PropertyTooltip("Easing curve that controls clone speed during the merge phase.")]
        [SerializeField] private AnimationCurve mergeEaseCurve;

        [BoxGroup("Merge Phase")]
        [LabelText("Merge Move Curve")]
        [PropertyTooltip("Curve to control curved or wave-like motion during clone merging.")]
        [SerializeField] private AnimationCurve mergeMoveCurve;

        // ------------------ Properties ------------------

        /// <summary>Number of clones spawned during the skill.</summary>
        public int CloneAmount => cloneAmount;

        /// <summary>The damage dealt by each clone.</summary>
        public float CloneDamage => cloneDamage;

        /// <summary>Distance each clone explodes outward from the caster.</summary>
        public float ExplosionDistance => explosionDistance;

        /// <summary>Total time for explosion movement.</summary>
        public float ExplosionDuration => explosionDuration;

        /// <summary>Base stagger delay (in seconds) per clone for the explosion phase.</summary>
        public float ExplosionDelayPerClone => explosionDelayPerClone;

        /// <summary>AnimationCurve that controls stagger delay scaling across explosion clones.</summary>
        public AnimationCurve ExplosionPerCloneCurve => explosionPerCloneCurve;

        /// <summary>Easing curve used during explosion movement.</summary>
        public AnimationCurve ExplosionEaseCurve => explosionEaseCurve;

        /// <summary>Curve to define curved paths during explosion.</summary>
        public AnimationCurve ExplosionMoveCurve => explosionMoveCurve;

        /// <summary>Total time for merge (return) movement.</summary>
        public float MergeDuration => mergeDuration;

        /// <summary>Base stagger delay (in seconds) per clone for the merge phase.</summary>
        public float MergeDelayPerClone => mergeDelayPerClone;

        /// <summary>AnimationCurve that controls stagger delay scaling across merging clones.</summary>
        public AnimationCurve MergePerCloneCurve => mergePerCloneCurve;

        /// <summary>Easing curve used during merge movement.</summary>
        public AnimationCurve MergeEaseCurve => mergeEaseCurve;

        /// <summary>Curve to define curved paths during merging.</summary>
        public AnimationCurve MergeMoveCurve => mergeMoveCurve;
    }
}
