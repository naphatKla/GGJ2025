using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Contains configuration for the Black Hole skill.
    /// Controls clone count, explosion and merge behavior, including movement curves and durations.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillBlackHoleData", menuName = "GameData/SkillData/SkillBlackHoleData")]
    public class SkillBlackHoleDataSo : BaseSkillDataSo
    {
        [BoxGroup("Clones", showLabel: true)]
        [LabelText("Clone Count")]
        [PropertyTooltip("Number of clones spawned during the black hole skill.")]
        [SerializeField] private int cloneAmount;

        [BoxGroup("Clones")]
        [LabelText("Clone Damage")]
        [PropertyTooltip("Damage each clone deals when interacting with enemies.")]
        [SerializeField] private float cloneDamage;

        [BoxGroup("Explosion Phase", showLabel: true)]
        [LabelText("Explosion Distance")]
        [PropertyTooltip("Distance each clone travels outward during the explosion phase.")]
        [SerializeField] private float explosionDistance;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Duration of the explosion phase in seconds.")]
        [SerializeField] private float explosionDuration;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Ease Curve")]
        [PropertyTooltip("Easing curve that controls clone speed during the explosion phase.")]
        [SerializeField] private AnimationCurve explosionEaseCurve;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Move Curve")]
        [PropertyTooltip("Curve to control curved or wave-like motion during explosion movement.")]
        [SerializeField] private AnimationCurve explosionMoveCurve;

        [Space]
        [BoxGroup("Merge Phase", showLabel: true)]
        [LabelText("Merge Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Duration of the merge phase (returning clones) in seconds.")]
        [SerializeField] private float mergeDuration;

        [BoxGroup("Merge Phase")]
        [LabelText("Merge Ease Curve")]
        [PropertyTooltip("Easing curve that controls clone speed during the merge phase.")]
        [SerializeField] private AnimationCurve mergeEaseCurve;

        [BoxGroup("Merge Phase")]
        [LabelText("Merge Move Curve")]
        [PropertyTooltip("Curve to control curved or wave-like motion during clone merging.")]
        [SerializeField] private AnimationCurve mergeMoveCurve;

        /// <summary>
        /// Number of clones spawned during the skill.
        /// </summary>
        public int CloneAmount => cloneAmount;

        /// <summary>
        /// The damage dealt by each clone.
        /// </summary>
        public float CloneDamage => cloneDamage;

        /// <summary>
        /// Distance each clone explodes outward from the caster.
        /// </summary>
        public float ExplosionDistance => explosionDistance;

        /// <summary>
        /// Duration in seconds for the explosion phase.
        /// </summary>
        public float ExplosionDuration => explosionDuration;

        /// <summary>
        /// Curve that defines easing/speed of explosion movement.
        /// </summary>
        public AnimationCurve ExplosionEaseCurve => explosionEaseCurve;

        /// <summary>
        /// Curve that defines path offset during the explosion.
        /// </summary>
        public AnimationCurve ExplosionMoveCurve => explosionMoveCurve;

        /// <summary>
        /// Duration in seconds for clones to return to the caster.
        /// </summary>
        public float MergeDuration => mergeDuration;

        /// <summary>
        /// Curve that defines easing/speed of merge movement.
        /// </summary>
        public AnimationCurve MergeEaseCurve => mergeEaseCurve;

        /// <summary>
        /// Curve that defines path offset during the merge.
        /// </summary>
        public AnimationCurve MergeMoveCurve => mergeMoveCurve;
    }
}
