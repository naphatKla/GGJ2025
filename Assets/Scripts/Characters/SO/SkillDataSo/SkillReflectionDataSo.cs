using Characters.MovementSystems;
using Characters.SkillSystems.SkillObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Configuration for the Black Hole skill.
    /// Controls how many skill objects are spawned, how far and fast they move,
    /// and how their staggered start timing and motion are handled during both the explosion and merge phases.
    /// Includes timing, damage, movement curves, and delay logic via animation curves.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillBlackHoleData", menuName = "GameData/SkillData/SkillBlackHoleData")]
    public class SkillReflectionDataSo : BaseSkillDataSo
    {
        #region Inspector & Variables
        
        // ------------------ skill object Settings ------------------
        
        [FoldoutGroup("Skill Object")]
        [LabelText("SKill Object Count")]
        [PropertyTooltip("Total number of skill objects spawned by the skill.")]
        [SerializeField]
        private int skillObjectAmount;
        
        [FoldoutGroup("Skill Object")]
        [LabelText("Skill Object Prefab")]
        [PropertyTooltip("Prefab of skill object instances.")]
        [SerializeField] [Required]
        private ReflectionSkillObject reflectionSkillObject;
        
        [FoldoutGroup("Skill Object")]
        [PropertyTooltip("Total number of skill objects hit per second")]
        [SerializeField]
        private float damageHitPerSec = 3;

        // ------------------ Explosion Phase ------------------

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Explosion Phase", showLabel: true)]
        [LabelText("Explosion Distance")]
        [PropertyTooltip("The distance each skill object moves outward during the explosion phase.")]
        [SerializeField]
        private float explosionDistance;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Explosion Phase")]
        [LabelText("Explosion Duration (Entire Phase, sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total duration of the explosion phase from first to last skill object.")]
        [SerializeField]
        private float explosionEntireDuration;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Explosion Phase")]
        [LabelText("Start Spread Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time span over which skill object explosion start times are spread. This duration is part of the entire duration.")]
        [ValidateInput("@explosionStartDuration < explosionEntireDuration", "Start duration must be less than the entire explosion duration.")]
        [SerializeField]
        private float explosionStartDuration;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Explosion Phase")]
        [LabelText("Start Curve")]
        [PropertyTooltip("Curve used to determine the relative start offset for each skill object during the explosion phase (normalized from 0 to 1).")]
        [SerializeField]
        private AnimationCurve explosionStartCurve;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Explosion Phase")]
        [LabelText("Ease Curve")]
        [PropertyTooltip("Controls the speed progression of each clone during the explosion phase.")]
        [SerializeField]
        private AnimationCurve explosionEaseCurve;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Explosion Phase")]
        [LabelText("Move Curve")]
        [PropertyTooltip("Applies lateral offset to create arcing or wave-like motion during explosion movement.")]
        [SerializeField]
        private AnimationCurve explosionMoveCurve;

        // ------------------ Merge Phase ------------------

        [Space]
        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Merge Phase")]
        [LabelText("Merge Duration (Entire Phase, sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total duration of the merge phase from first to last skill ojbect.")]
        [SerializeField]
        private float mergeEntireDuration;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Merge Phase")]
        [LabelText("Start Spread Duration (sec)")]
        [Unit(Units.Second)]
        [PropertyTooltip("Total time span over which skill object merge start times are spread. This duration is part of the entire duration.")]
        [ValidateInput("@mergeStartDuration < mergeEntireDuration", "Start duration must be less than the entire merge duration.")]
        [SerializeField]
        private float mergeStartDuration;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Merge Phase")]
        [LabelText("Start Curve")]
        [PropertyTooltip("Curve used to determine the relative start offset for each skill object during the merge phase (normalized from 0 to 1).")]
        [SerializeField]
        private AnimationCurve mergeStartCurve;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Merge Phase")]
        [LabelText("Ease Curve")]
        [PropertyTooltip("Controls the speed progression of each skill object during the merge phase.")]
        [SerializeField]
        private AnimationCurve mergeEaseCurve;

        [FoldoutGroup("Reflection Configs")]
        [BoxGroup("Reflection Configs/Merge Phase")]
        [LabelText("Move Curve")]
        [PropertyTooltip("Applies lateral offset to create arcing or wave-like motion during merge movement.")]
        [SerializeField]
        private AnimationCurve mergeMoveCurve;
        
        // ------------------ Properties ------------------

        /// <summary>Total number of skill object spawned during the skill.</summary>
        public int SkillObjectAmount => skillObjectAmount;

        public float DamageHitPerSec => damageHitPerSec;

        /// <summary>Prefab of skill object to create the instances.</summary>
        public ReflectionSkillObject ReflectionSkillObject => reflectionSkillObject;

        /// <summary>Distance each skill object travels outward during the explosion phase.</summary>
        public float ExplosionDistance => explosionDistance;

        /// <summary>Total duration of the explosion phase (includes spread + motion time).</summary>
        public float ExplosionEntireDuration => explosionEntireDuration;

        /// <summary>Total time span used to spread explosion start times across all skill objects. This is part of ExplosionEntireDuration.</summary>
        public float ExplosionStartDuration => explosionStartDuration;

        /// <summary>Curve that controls how start times are distributed during the explosion phase.</summary>
        public AnimationCurve ExplosionStartCurve => explosionStartCurve;

        /// <summary>Speed easing curve applied to skill objects during explosion movement.</summary>
        public AnimationCurve ExplosionEaseCurve => explosionEaseCurve;

        /// <summary>Curve that offsets the path of skill objects for explosion movement (e.g., arcs or waves).</summary>
        public AnimationCurve ExplosionMoveCurve => explosionMoveCurve;

        /// <summary>Total duration of the merge phase (includes spread + motion time).</summary>
        public float MergeEntireDuration => mergeEntireDuration;

        /// <summary>Total time span used to spread merge start times across all skill objects. This is part of MergeEntireDuration.</summary>
        public float MergeStartDuration => mergeStartDuration;

        /// <summary>Curve that controls how start times are distributed during the merge phase.</summary>
        public AnimationCurve MergeStartCurve => mergeStartCurve;

        /// <summary>Speed easing curve applied to skill objects during merge movement.</summary>
        public AnimationCurve MergeEaseCurve => mergeEaseCurve;

        /// <summary>Curve that offsets the path of skill objects for merge movement (e.g., arcs or waves).</summary>
        public AnimationCurve MergeMoveCurve => mergeMoveCurve;
        
        #endregion
    }
}