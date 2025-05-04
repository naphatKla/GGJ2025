using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Contains configurable data for the dash skill.
    /// Inherits base cooldown configuration from <see cref="BaseSkillDataSo"/>.
    /// This asset defines how far and how long the dash lasts.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDashData", menuName = "GameData/SkillData/SkillDashData")]
    public class SkillDashDataSo : BaseSkillDataSo
    {
        [Unit(Units.Second)]
        [PropertyTooltip("The duration (in seconds) of the dash movement.")]
        [SerializeField] private float dashDuration = 0.3f;
        
        [PropertyTooltip("The total distance the character will dash forward.")]
        [SerializeField] private float dashDistance = 8f;

        /// <summary>
        /// Gets the duration (in seconds) of the dash movement.
        /// </summary>
        public float DashDuration => dashDuration;

        /// <summary>
        /// Gets the total distance the character will dash.
        /// </summary>
        public float DashDistance => dashDistance;
    }
}