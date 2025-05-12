using DG.Tweening;
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
        
        [PropertyTooltip("AnimationCurve that applies lateral displacement during the dash allowing for custom arcing or wave-like motion paths instead of straight-line dashing.")]
        [SerializeField] private AnimationCurve dashCurve;

        /// <summary>
        /// Duration of the dash movement in seconds.
        /// </summary>
        public float DashDuration => dashDuration;

        /// <summary>
        /// Total distance the character will dash forward.
        /// </summary>
        public float DashDistance => dashDistance;
        
        /// <summary>
        /// Optional AnimationCurve that applies lateral displacement during the dash,
        /// allowing for custom arcing or wave-like motion paths instead of straight-line dashing.
        /// </summary>
        public AnimationCurve DashCurve => dashCurve;
    }
}
