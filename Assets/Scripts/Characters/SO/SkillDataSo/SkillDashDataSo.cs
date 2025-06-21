using Sirenix.OdinInspector;
using Unity.VisualScripting;
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
        #region Inspector & Variables
        
        [Unit(Units.Second)]
        [PropertyTooltip("Duration of the dash movement in seconds.")]
        [SerializeField] private float dashDuration = 0.3f;

        [PropertyTooltip("Total distance the character will dash forward.")]
        [SerializeField] private float dashDistance = 8f;
        
        [PropertyTooltip("If true. dash's distant can be scalable depends on input length.")]
        [SerializeField] private bool isFlexibleDistance;

        [EnableIf(nameof(isFlexibleDistance))]
        [PropertyTooltip("Maximum length of input that will perform the maximum effective distance")]
        [SerializeField] private float maxInputLength = 6f;
        
        [EnableIf(nameof(isFlexibleDistance))]
        [PropertyTooltip("Minimum & Maximum Value tha dash can perform.")]
        [ValidateInput(nameof(IsMinMaxValid), "Values must be between 0 and 1")]
        [SerializeField] private Vector2 minMaxEffective = new Vector2(0.4f, 1f);
        
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
        /// If true. dash's distant can be scalable depends on input length.
        /// </summary>
        public bool IsFlexibleDistance => isFlexibleDistance;

        /// <summary>
        /// Minimum & Maximum Value tha dash can perform.
        /// </summary>
        public float MaxInputLength => maxInputLength;

        /// <summary>
        /// Minimum & Maximum Value tha dash can perform.
        /// </summary>
        public Vector2 MinMaxEffective => minMaxEffective;

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
        
        private bool IsMinMaxValid(Vector2 value)
        {
            return value.x >= 0f && value.x <= 1f && value.y >= 0f && value.y <= 1f;
        }
        
        #endregion
    }
}
