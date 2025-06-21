using System.Collections.Generic;
using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
using Characters.SO.SkillDataSo;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems
{
    /// <summary>
    /// Controls and manages the execution and cooldowns of primary and secondary skills.
    /// Handles cooldown countdowns, skill assignment, and triggering logic with directional input.
    /// Skill data and their associated runtime behaviours are paired automatically at runtime.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        #region Inspector & Variables

        [SerializeField, InfoBox("Default list of skill data. [0] is primary, [1] is secondary.")]
        private List<BaseSkillDataSo> allAvailableSkillData;

        /// <summary>
        /// Stores instantiated runtime components for each skill data.
        /// </summary>
        private Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillDictionary = new();

        private BaseSkillDataSo _primarySkillData;
        private BaseSkillDataSo _secondarySkillData;

        private BaseSkillRuntime _primarySkillRuntime;
        private BaseSkillRuntime _secondarySkillRuntime;

        /// <summary>
        /// Remaining cooldown time for the primary skill.
        /// </summary>
        private float _primarySkillCooldown;

        /// <summary>
        /// Remaining cooldown time for the secondary skill.
        /// </summary>
        private float _secondarySkillCooldown;

        /// <summary>
        /// Reference to the character who owns this skill system.
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
        /// Initializes the skill system with the owning controller, instantiates and pairs runtime components with skill data,
        /// and assigns the default primary/secondary skills.
        /// </summary>
        /// <param name="owner">The controller (e.g., player or enemy) that owns this skill system.</param>
        public void Initialize(BaseController owner)
        {
            _owner = owner;

            foreach (BaseSkillDataSo skillData in allAvailableSkillData)
            {
                BaseSkillRuntime skillRuntime = (BaseSkillRuntime)gameObject.AddComponent(skillData.SkillRuntime);
                skillRuntime.AssignSkillData(skillData);
                _skillDictionary.Add(skillData, skillRuntime);
            }

            AssignDefaultSkill();
        }

        /// <summary>
        /// Assigns default primary and secondary skills based on the available skill list.
        /// If only one skill exists, assigns it to primary and leaves secondary empty.
        /// Logs warnings if no or only one skill is available.
        /// </summary>
        private void AssignDefaultSkill()
        {
            switch (allAvailableSkillData.Count)
            {
                case <= 0:
                    Debug.LogWarning($"⚠️ [SkillSystem] No skill data assigned on {gameObject.name}.");
                    return;
                case 1:
                    Debug.LogWarning($"⚠️ [SkillSystem] Only one skill assigned on {gameObject.name}. Secondary will be empty.");
                    SetPrimarySkill(allAvailableSkillData[0]);
                    return;
                default:
                    SetPrimarySkill(allAvailableSkillData[0]);
                    SetSecondarySkill(allAvailableSkillData[1]);
                    break;
            }
        }

        /// <summary>
        /// Executes the primary skill if not on cooldown and the system is initialized.
        /// </summary>
        public void PerformPrimarySkill()
        {
            if (!_primarySkillRuntime || _primarySkillCooldown > 0) return;
            if (!_owner)
            {
                Debug.LogWarning("⚠️ [SkillSystem] Missing skill owner. Call Initialize(owner) before performing skills.");
                return;
            }

            _primarySkillRuntime.PerformSkill(_owner);
            ModifyPrimarySkillCooldown(_primarySkillData.Cooldown);
        }

        /// <summary>
        /// Executes the secondary skill if not on cooldown and the system is initialized.
        /// </summary>
        public void PerformSecondarySkill()
        {
            if (!_secondarySkillRuntime || _secondarySkillCooldown > 0) return;
            if (!_owner)
            {
                Debug.LogWarning("⚠️ [SkillSystem] Missing skill owner. Call Initialize(owner) before performing skills.");
                return;
            }

            _secondarySkillRuntime.PerformSkill(_owner);
            ModifySecondarySkillCooldown(_secondarySkillData.Cooldown);
        }

        /// <summary>
        /// Modifies and clamps the remaining cooldown time for the primary skill.
        /// </summary>
        /// <param name="value">New cooldown time (will be clamped between 0 and max cooldown).</param>
        public void ModifyPrimarySkillCooldown(float value)
        {
            if (!_primarySkillRuntime) return;
            _primarySkillCooldown = Mathf.Clamp(value, 0, _primarySkillData.Cooldown);
        }

        /// <summary>
        /// Modifies and clamps the remaining cooldown time for the secondary skill.
        /// </summary>
        /// <param name="value">New cooldown time (will be clamped between 0 and max cooldown).</param>
        public void ModifySecondarySkillCooldown(float value)
        {
            if (!_secondarySkillRuntime) return;
            _secondarySkillCooldown = Mathf.Clamp(value, 0, _secondarySkillData.Cooldown);
        }

        /// <summary>
        /// Instantly resets the primary skill's cooldown.
        /// </summary>
        public void ResetPrimarySkillCooldown() => _primarySkillCooldown = 0;

        /// <summary>
        /// Instantly resets the secondary skill's cooldown.
        /// </summary>
        public void ResetSecondarySkillCooldown() => _secondarySkillCooldown = 0;

        /// <summary>
        /// Assigns a new primary skill by referencing its runtime from the dictionary and resets cooldown.
        /// </summary>
        /// <param name="newSkillData">The skill data to assign as primary.</param>
        public void SetPrimarySkill(BaseSkillDataSo newSkillData)
        {
            _primarySkillData = newSkillData;
            _primarySkillRuntime = _skillDictionary[_primarySkillData];
            ResetPrimarySkillCooldown();
        }

        /// <summary>
        /// Assigns a new secondary skill by referencing its runtime from the dictionary and resets cooldown.
        /// </summary>
        /// <param name="newSkillData">The skill data to assign as secondary.</param>
        public void SetSecondarySkill(BaseSkillDataSo newSkillData)
        {
            _secondarySkillData = newSkillData;
            _secondarySkillRuntime = _skillDictionary[_secondarySkillData];
            ResetSecondarySkillCooldown();
        }

        /// <summary>
        /// Resets the skill system to its initial/default state.
        /// Cancels any running skills, reassigns default skills, and resets skill cooldowns.
        /// Typically used during events like respawn, revive, or full state reset.
        /// </summary>
        public void ResetSkillSystem()
        {
            _primarySkillRuntime?.CancelSkill();
            _secondarySkillRuntime?.CancelSkill();
            AssignDefaultSkill();
        }

        #endregion
    }
}
