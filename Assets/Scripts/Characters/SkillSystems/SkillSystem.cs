using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters.SkillSystems
{
    /// <summary>
    /// Controls and manages the execution and cooldowns of primary and secondary skills.
    /// Handles cooldown countdown, skill assignment, and skill triggering with direction input.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        #region Inspector & Variables

        /// <summary>
        /// The default primary skill assigned to this character.
        /// </summary>
        [FormerlySerializedAs("primarySkillRuntimes")] [FormerlySerializedAs("primarySkill")] [SerializeField] private BaseSkillRuntime primarySkillRuntime;

        /// <summary>
        /// The default secondary skill assigned to this character.
        /// </summary>
        [FormerlySerializedAs("secondarySkillRuntimes")] [FormerlySerializedAs("secondarySkill")] [SerializeField] private BaseSkillRuntime secondarySkillRuntime;

        /// <summary>
        /// Remaining cooldown time for the primary skill.
        /// </summary>
        private float _primarySkillCooldown;

        /// <summary>
        /// Remaining cooldown time for the secondary skill.
        /// </summary>
        private float _secondarySkillCooldown;

        /// <summary>
        /// Reference to the owning controller (usually the player or enemy).
        /// </summary>
        private BaseController _owner;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Decrements cooldown timers for both skills every frame.
        /// </summary>
        private void Update()
        {
            ModifyPrimarySkillCooldown(_primarySkillCooldown - Time.deltaTime);
            ModifySecondarySkillCooldown(_secondarySkillCooldown - Time.deltaTime);
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Initializes the skill system with a given owner and sets default skills.
        /// </summary>
        /// <param name="owner">The character that owns this skill system.</param>
        public void Initialize(BaseController owner)
        {
            _owner = owner;
            SetPrimarySkill(primarySkillRuntime);
            SetSecondarySkill(secondarySkillRuntime);
        }
        
        /// <summary>
        /// Triggers the primary skill if it is available and not on cooldown.
        /// </summary>
        /// <param name="direction">The direction to perform the skill toward.</param>
        public void PerformPrimarySkill(Vector2 direction)
        {
            if (!primarySkillRuntime) return;
            if (_primarySkillCooldown > 0) return;
            if (!_owner)
            {
                Debug.LogWarning("⚠️ [SkillSystem] Missing skill owner! Call Initialize(owner) before performing skills.");
                return;
            }

            primarySkillRuntime.PerformSkill(_owner, direction);
            ModifyPrimarySkillCooldown(primarySkillRuntime.cooldownDuration);
        }

        /// <summary>
        /// Triggers the secondary skill if it is available and not on cooldown.
        /// </summary>
        /// <param name="direction">The direction to perform the skill toward.</param>
        public void PerformSecondarySkill(Vector2 direction)
        {
            if (!secondarySkillRuntime) return;
            if (_secondarySkillCooldown > 0) return;
            if (!_owner)
            {
                Debug.LogWarning("⚠️ [SkillSystem] Missing skill owner! Call Initialize(owner) before performing skills.");
                return;
            }

            secondarySkillRuntime.PerformSkill(_owner, direction);
            ModifySecondarySkillCooldown(secondarySkillRuntime.cooldownDuration);
        }
        
        /// <summary>
        /// Updates the cooldown value for the primary skill, clamped to its max duration.
        /// </summary>
        /// <param name="value">The new cooldown value.</param>
        public void ModifyPrimarySkillCooldown(float value)
        {
            if (!primarySkillRuntime) return;

            _primarySkillCooldown =
                Mathf.Clamp(value, 0, primarySkillRuntime.cooldownDuration);
        }

        /// <summary>
        /// Updates the cooldown value for the secondary skill, clamped to its max duration.
        /// </summary>
        /// <param name="value">The new cooldown value.</param>
        public void ModifySecondarySkillCooldown(float value)
        {
            if (!secondarySkillRuntime) return;

            _secondarySkillCooldown =
                Mathf.Clamp(value, 0, secondarySkillRuntime.cooldownDuration);
        }

        /// <summary>
        /// Instantly resets the primary skill's cooldown to 0.
        /// </summary>
        public void ResetPrimarySkillCooldown()
        {
            _primarySkillCooldown = 0;
        }

        /// <summary>
        /// Instantly resets the secondary skill's cooldown to 0.
        /// </summary>
        public void ResetSecondarySkillCooldown()
        {
            _secondarySkillCooldown = 0;
        }
        
        /// <summary>
        /// Assigns a new primary skill and resets its cooldown.
        /// </summary>
        /// <param name="newSkillRuntimes">The new skill to assign as primary.</param>
        public void SetPrimarySkill(BaseSkillRuntime newSkillRuntime)
        {
            primarySkillRuntime = newSkillRuntime;
            ResetPrimarySkillCooldown();
        }

        /// <summary>
        /// Assigns a new secondary skill and resets its cooldown.
        /// </summary>
        /// <param name="newSkillRuntimes">The new skill to assign as secondary.</param>
        public void SetSecondarySkill(BaseSkillRuntime newSkillRuntime)
        {
            secondarySkillRuntime = newSkillRuntime;
            ResetSecondarySkillCooldown();
        }
        
        #endregion
    }
}
