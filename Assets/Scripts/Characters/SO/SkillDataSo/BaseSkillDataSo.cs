using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    /// <summary>
    /// Abstract base class for all skill data ScriptableObjects.
    /// Defines common configuration values shared by all skills, such as cooldown duration.
    /// Inherit from this class to create custom data containers for specific skill types.
    /// </summary>
    public abstract class BaseSkillDataSo : ScriptableObject
    {
        [PropertyTooltip("The cooldown duration (in seconds) before the skill can be used again.")]
        [SerializeField] private float cooldown = 1f;

        /// <summary>
        /// Gets the cooldown duration (in seconds) for this skill.
        /// </summary>
        public float Cooldown => cooldown;
    }
}