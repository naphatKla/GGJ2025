using System.Collections.Generic;
using Characters.StatusEffectSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Configuration for the Black Hole skill.
    /// Controls how many clones are spawned, how far and fast they move,
    /// and how their staggered start timing and motion are handled during both the explosion and merge phases.
    /// Includes timing, damage, movement curves, and delay logic via animation curves.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillBlackHoleData", menuName = "GameData/SkillData/SkillBlackHoleData")]
    public class SkillBlackHoleDataSo : BaseSkillDataSo
    {
        #region Inspector & Variables
        
        // ------------------ Clone Settings ------------------

        [BoxGroup("Clones", showLabel: true)]
        [LabelText("Clone Count")]
        [PropertyTooltip("Total number of clones spawned by the skill.")]
        [SerializeField]
        private int cloneAmount;

        [BoxGroup("Clones")]
        [LabelText("Clone Damage")]
        [PropertyTooltip("Damage dealt by each individual clone when it hits enemies.")]
        [SerializeField]
        private float cloneDamage;

        // ------------------ Explosion Phase ------------------

        [BoxGroup("Explosion Phase", showLabel: true)]
        [LabelText("Explosion Distance")]
        [PropertyTooltip("The distance each clone moves outward during the explosion phase.")]
        [SerializeField]
        private float explosionDistance;

        [BoxGroup("Explosion Phase")]
        [LabelText("Explosion Duration (Entire Phase, sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total duration of the explosion phase from first to last clone.")]
        [SerializeField]
        private float explosionEntireDuration;

        [BoxGroup("Explosion Phase")]
        [LabelText("Start Spread Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time span over which clone explosion start times are spread. This duration is part of the entire duration.")]
        [ValidateInput("@explosionStartDuration < explosionEntireDuration", "Start duration must be less than the entire explosion duration.")]
        [SerializeField]
        private float explosionStartDuration;

        [BoxGroup("Explosion Phase")]
        [LabelText("Start Curve")]
        [PropertyTooltip("Curve used to determine the relative start offset for each clone during the explosion phase (normalized from 0 to 1).")]
        [SerializeField]
        private AnimationCurve explosionStartCurve;

        [BoxGroup("Explosion Phase")]
        [LabelText("Ease Curve")]
        [PropertyTooltip("Controls the speed progression of each clone during the explosion phase.")]
        [SerializeField]
        private AnimationCurve explosionEaseCurve;

        [BoxGroup("Explosion Phase")]
        [LabelText("Move Curve")]
        [PropertyTooltip("Applies lateral offset to create arcing or wave-like motion during explosion movement.")]
        [SerializeField]
        private AnimationCurve explosionMoveCurve;

        // ------------------ Merge Phase ------------------

        [Space]
        [BoxGroup("Merge Phase", showLabel: true)]
        [LabelText("Merge Duration (Entire Phase, sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total duration of the merge phase from first to last clone.")]
        [SerializeField]
        private float mergeEntireDuration;

        [BoxGroup("Merge Phase")]
        [LabelText("Start Spread Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time span over which clone merge start times are spread. This duration is part of the entire duration.")]
        [ValidateInput("@mergeStartDuration < mergeEntireDuration", "Start duration must be less than the entire merge duration.")]
        [SerializeField]
        private float mergeStartDuration;

        [BoxGroup("Merge Phase")]
        [LabelText("Start Curve")]
        [PropertyTooltip("Curve used to determine the relative start offset for each clone during the merge phase (normalized from 0 to 1).")]
        [SerializeField]
        private AnimationCurve mergeStartCurve;

        [BoxGroup("Merge Phase")]
        [LabelText("Ease Curve")]
        [PropertyTooltip("Controls the speed progression of each clone during the merge phase.")]
        [SerializeField]
        private AnimationCurve mergeEaseCurve;

        [BoxGroup("Merge Phase")]
        [LabelText("Move Curve")]
        [PropertyTooltip("Applies lateral offset to create arcing or wave-like motion during merge movement.")]
        [SerializeField]
        private AnimationCurve mergeMoveCurve;
        
        // ------------------ Status Effect ------------------
        
        [PropertyTooltip("Status effects that will be applied to the clones when skill start.")]
        [PropertyOrder(9999)]
        [SerializeField] 
        private List<StatusEffectDataPayload> cloneEffects;
        
        // ------------------ Properties ------------------

        /// <summary>Total number of clones spawned during the skill.</summary>
        public int CloneAmount => cloneAmount;

        /// <summary>Damage dealt by each clone during the explosion or merge interaction.</summary>
        public float CloneDamage => cloneDamage;

        /// <summary>Distance each clone travels outward during the explosion phase.</summary>
        public float ExplosionDistance => explosionDistance;

        /// <summary>Total duration of the explosion phase (includes spread + motion time).</summary>
        public float ExplosionEntireDuration => explosionEntireDuration;

        /// <summary>Total time span used to spread explosion start times across all clones. This is part of ExplosionEntireDuration.</summary>
        public float ExplosionStartDuration => explosionStartDuration;

        /// <summary>Curve that controls how start times are distributed during the explosion phase.</summary>
        public AnimationCurve ExplosionStartCurve => explosionStartCurve;

        /// <summary>Speed easing curve applied to clones during explosion movement.</summary>
        public AnimationCurve ExplosionEaseCurve => explosionEaseCurve;

        /// <summary>Curve that offsets the path of clones for explosion movement (e.g., arcs or waves).</summary>
        public AnimationCurve ExplosionMoveCurve => explosionMoveCurve;

        /// <summary>Total duration of the merge phase (includes spread + motion time).</summary>
        public float MergeEntireDuration => mergeEntireDuration;

        /// <summary>Total time span used to spread merge start times across all clones. This is part of MergeEntireDuration.</summary>
        public float MergeStartDuration => mergeStartDuration;

        /// <summary>Curve that controls how start times are distributed during the merge phase.</summary>
        public AnimationCurve MergeStartCurve => mergeStartCurve;

        /// <summary>Speed easing curve applied to clones during merge movement.</summary>
        public AnimationCurve MergeEaseCurve => mergeEaseCurve;

        /// <summary>Curve that offsets the path of clones for merge movement (e.g., arcs or waves).</summary>
        public AnimationCurve MergeMoveCurve => mergeMoveCurve;

        /// <summary>Status effects that will be applied to the clones when skill start.</summary>
        public List<StatusEffectDataPayload> CloneEffects => cloneEffects;

        #endregion
    }
}