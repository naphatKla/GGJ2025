using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Contains configurable parameters specific to the dash skill.
    /// Defines the distance, duration, motion curve, and optional noise used during the dash.
    /// Inherits common properties like cooldown and runtime binding from <see cref="BaseSkillDataSo"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDashData", menuName = "GameData/SkillData/SkillDashData")]
    public class SkillDashDataSo : BaseSkillDataSo
    {
        [Unit(Units.Second)]
        [PropertyTooltip("Duration of the dash movement in seconds.")]
        [SerializeField] private float dashDuration = 0.3f;

        [PropertyTooltip("Total distance the character will dash forward.")]
        [SerializeField] private float dashDistance = 8f;
        
        [PropertyTooltip("AnimationCurve controlling the dash's easing over time. Used to shape the dash speed progression (e.g., accelerate then decelerate).")]
        [SerializeField] private AnimationCurve dashEaseCurve;
        
        [PropertyTooltip("AnimationCurve that applies lateral displacement during the dash allowing for custom arcing or wave-like motion paths instead of straight-line dashing.")]
        [SerializeField] private AnimationCurve dashMoveCurve;
        
        /// <summary>
        /// Duration of the dash movement in seconds.
        /// Determines how long the dash takes from start to finish.
        /// </summary>
        public float DashDuration => dashDuration;

        /// <summary>
        /// Total distance the character will dash forward.
        /// The dash moves the character forward by this amount in world space.
        /// </summary>
        public float DashDistance => dashDistance;

        /// <summary>
        /// AnimationCurve controlling the dash's easing over time.
        /// Defines how the speed changes across the dash duration (e.g., ease in/out).
        /// </summary>
        public AnimationCurve DashEaseCurve => dashEaseCurve;

        /// <summary>
        /// AnimationCurve that applies lateral displacement during the dash.
        /// Allows curved, arcing, or wave-like motion paths instead of a straight line.
        /// </summary>
        public AnimationCurve DashMoveCurve => dashMoveCurve;
    }
}
