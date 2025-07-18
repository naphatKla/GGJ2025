using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
using Characters.SO.SkillDataSo;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace Characters.SkillSystems
{
    [Serializable]
    public enum SkillType
    {
        PrimarySkill = 0,
        SecondarySkill = 1,
        AutoSkill = 2,
    }

    public class SkillSystem : MonoBehaviour
    {
        #region Inspector & Variables

        [ValidateInput(nameof(IsSkillDataUnique), "Primary/Secondary/Auto skill must not duplicate!")]
        [SerializeField] protected BaseSkillDataSo primarySkillData;

        [ValidateInput(nameof(IsSkillDataUnique), "Primary/Secondary/Auto skill must not duplicate!")]
        [SerializeField] private BaseSkillDataSo secondarySkillData;

        [ValidateInput(nameof(IsSkillListUnique), "Duplicate skill in autoSkillDataList is not allowed!")]
        [SerializeField] private List<BaseSkillDataSo> autoSkillDataList;

        private HashSet<BaseSkillDataSo> _autoSkillDatas = new(); // Runtime collection for auto skills
        private BaseSkillDataSo _defaultPrimarySkillData;
        private BaseSkillDataSo _defaultSecondarySkillData;
        private HashSet<BaseSkillDataSo> _defaultAutoSkillDatas = new();

        private readonly Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillRuntimeDictionary = new();
        protected BaseController owner;

        #endregion

        #region Inspector Duplicate Check (Odin)

        private bool IsSkillListUnique(List<BaseSkillDataSo> list)
        {
            if (list == null) return true;
            var hashSet = new HashSet<BaseSkillDataSo>();
            foreach (var skill in list)
            {
                if (skill == null) continue;
                if (!hashSet.Add(skill))
                    return false;
            }
            return true;
        }

        private bool IsSkillDataUnique(BaseSkillDataSo _)
        {
            var set = new HashSet<BaseSkillDataSo>();
            if (primarySkillData && !set.Add(primarySkillData)) return false;
            if (secondarySkillData && !set.Add(secondarySkillData)) return false;
            foreach (var skill in autoSkillDataList)
            {
                if (skill == null) continue;
                if (!set.Add(skill))
                    return false;
            }
            return true;
        }

        #endregion

        #region Unity Methods

        private void FixedUpdate()
        {
            if (!owner) return;
            UpdateCooldown();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initializes the SkillSystem with a controller/owner.
        /// </summary>
        public void Initialize(BaseController owner)
        {
            this.owner = owner;

            _autoSkillDatas.Clear();
            foreach (var baseSkillDataSo in autoSkillDataList)
                _autoSkillDatas.Add(baseSkillDataSo);

            _defaultPrimarySkillData = primarySkillData;
            _defaultSecondarySkillData = secondarySkillData;
            _defaultAutoSkillDatas = new(_autoSkillDatas);
            ResetToDefaultSkill();
        }

        /// <summary>
        /// Returns all currently equipped skills, organized by slot.
        /// </summary>
        public (BaseSkillDataSo Primary, BaseSkillDataSo Secondary, List<BaseSkillDataSo> Auto, IEnumerable<BaseSkillDataSo> All)
            GetAllCurrentSkillDatas()
        {
            var all = new List<BaseSkillDataSo>();
            if (primarySkillData != null) all.Add(primarySkillData);
            if (secondarySkillData != null) all.Add(secondarySkillData);
            all.AddRange(_autoSkillDatas);
            return (primarySkillData, secondarySkillData, _autoSkillDatas.ToList(), all);
        }

        /// <summary>
        /// Adds or upgrades a skill in the appropriate slot (Primary → Secondary → Auto).
        /// - If the slot is empty, assigns the skill.
        /// - If an upgrade is available, replaces the skill in that slot.
        /// - If all auto slots are full, upgrades the appropriate auto skill.
        /// </summary>
        public void UpgradeSkillSlot(BaseSkillDataSo upgradeSkill)
        {
            // Try Primary slot
            if (primarySkillData == null)
            {
                SetOrAddSkill(upgradeSkill, SkillType.PrimarySkill);
                Debug.Log($"[SkillSystem] Added new primary skill: {upgradeSkill.name}");
                return;
            }
            if (primarySkillData.NextSkillDataUpgrade == upgradeSkill)
            {
                SetOrAddSkill(upgradeSkill, SkillType.PrimarySkill);
                Debug.Log($"[SkillSystem] Upgraded primary skill to: {upgradeSkill.name}");
                return;
            }

            // Try Secondary slot
            if (secondarySkillData == null)
            {
                SetOrAddSkill(upgradeSkill, SkillType.SecondarySkill);
                Debug.Log($"[SkillSystem] Added new secondary skill: {upgradeSkill.name}");
                return;
            }
            if (secondarySkillData.NextSkillDataUpgrade == upgradeSkill)
            {
                SetOrAddSkill(upgradeSkill, SkillType.SecondarySkill);
                Debug.Log($"[SkillSystem] Upgraded secondary skill to: {upgradeSkill.name}");
                return;
            }

            // Try upgrading an auto skill
            var oldAuto = _autoSkillDatas.FirstOrDefault(s => s.NextSkillDataUpgrade == upgradeSkill);
            if (oldAuto != null)
            {
                _autoSkillDatas.Remove(oldAuto);
                SetOrAddSkill(upgradeSkill, SkillType.AutoSkill);
                Debug.Log($"[SkillSystem] Upgraded auto skill to: {upgradeSkill.name}");
                return;
            }

            // Add as new auto skill if slots available
            if (_autoSkillDatas.Count < autoSkillDataList.Count)
            {
                SetOrAddSkill(upgradeSkill, SkillType.AutoSkill);
                Debug.Log($"[SkillSystem] Added new auto skill: {upgradeSkill.name}");
                return;
            }

            Debug.LogWarning($"[SkillSystem] Cannot upgrade/add skill {upgradeSkill.name}: all slots full and no match for upgrade.");
        }

        /// <summary>
        /// Determines which slot would be affected by upgrading to the given skill.
        /// Useful for UI highlighting or validation.
        /// </summary>
        public SkillType? GetSlotUpgradeTarget(BaseSkillDataSo upgradeSkill)
        {
            if (primarySkillData == null || primarySkillData.NextSkillDataUpgrade == upgradeSkill)
                return SkillType.PrimarySkill;
            if (secondarySkillData == null || secondarySkillData.NextSkillDataUpgrade == upgradeSkill)
                return SkillType.SecondarySkill;
            if (_autoSkillDatas.Any(s => s.NextSkillDataUpgrade == upgradeSkill) || _autoSkillDatas.Count < autoSkillDataList.Count)
                return SkillType.AutoSkill;
            return null;
        }

        /// <summary>
        /// Returns true if there is an empty slot for the given skill type.
        /// </summary>
        public bool HasEmptySlot(SkillType type)
        {
            switch (type)
            {
                case SkillType.PrimarySkill: return primarySkillData == null;
                case SkillType.SecondarySkill: return secondarySkillData == null;
                case SkillType.AutoSkill: return _autoSkillDatas.Count < autoSkillDataList.Count;
                default: return false;
            }
        }

        /// <summary>
        /// Checks if a given skill is already equipped in any slot.
        /// </summary>
        public bool ContainsSkill(BaseSkillDataSo skillData)
        {
            if (skillData == null) return false;
            return skillData == primarySkillData
                || skillData == secondarySkillData
                || _autoSkillDatas.Contains(skillData);
        }

        #endregion

        #region Internal Methods

        private void InstantiateSkillRuntime(BaseSkillDataSo skillData)
        {
            if (!skillData) return;
            if (!owner) return;
            if (_skillRuntimeDictionary.ContainsKey(skillData)) return;

            BaseSkillRuntime skillRuntime = (BaseSkillRuntime)gameObject.AddComponent(skillData.SkillRuntime);
            skillRuntime.AssignSkillData(skillData, owner);
            _skillRuntimeDictionary.Add(skillData, skillRuntime);
        }

        /// <summary>
        /// Adds or sets a skill for the specified slot, with duplicate validation.
        /// </summary>
        public virtual void SetOrAddSkill(BaseSkillDataSo newSkillData, SkillType type)
        {
            if (!owner) return;
            if (!newSkillData) return;

            // Prevent duplicate assignments
            switch (type)
            {
                case SkillType.PrimarySkill:
                    if ((secondarySkillData == newSkillData) || _autoSkillDatas.Contains(newSkillData))
                    {
                        Debug.LogWarning($"[SkillSystem] Duplicate skill ({newSkillData.name}) detected! Cannot assign as Primary because it already exists in Secondary or AutoSkill.");
                        return;
                    }
                    break;
                case SkillType.SecondarySkill:
                    if ((primarySkillData == newSkillData) || _autoSkillDatas.Contains(newSkillData))
                    {
                        Debug.LogWarning($"[SkillSystem] Duplicate skill ({newSkillData.name}) detected! Cannot assign as Secondary because it already exists in Primary or AutoSkill.");
                        return;
                    }
                    break;
                case SkillType.AutoSkill:
                    if ((primarySkillData == newSkillData) || (secondarySkillData == newSkillData) || _autoSkillDatas.Contains(newSkillData))
                    {
                        Debug.LogWarning($"[SkillSystem] Duplicate skill ({newSkillData.name}) detected! Cannot assign as AutoSkill because it already exists in Primary/Secondary/AutoSkill.");
                        return;
                    }
                    break;
            }

            InstantiateSkillRuntime(newSkillData);

            switch (type)
            {
                case SkillType.PrimarySkill:
                    primarySkillData = newSkillData;
                    break;
                case SkillType.SecondarySkill:
                    secondarySkillData = newSkillData;
                    break;
                case SkillType.AutoSkill:
                    _autoSkillDatas.Add(newSkillData);
                    break;
            }

            GetSkillRuntimeOrDefault(newSkillData)?.SetCooldown(0);
        }

        public virtual BaseSkillRuntime GetSkillRuntimeOrDefault(BaseSkillDataSo skillData)
        {
            if (skillData && _skillRuntimeDictionary.TryGetValue(skillData, out var runtime))
                return runtime;
            return null;
        }

        public virtual void PerformSkill(SkillType type)
        {
            if (!owner) return;

            switch (type)
            {
                case SkillType.PrimarySkill:
                    Debug.Log("Perform Primary");
                    GetSkillRuntimeOrDefault(primarySkillData)?.PerformSkill();
                    PerformSkill(SkillType.AutoSkill);
                    break;

                case SkillType.SecondarySkill:
                    Debug.Log("Perform Secondary");
                    GetSkillRuntimeOrDefault(secondarySkillData)?.PerformSkill();
                    break;

                case SkillType.AutoSkill:
                    foreach (var data in _autoSkillDatas)
                        GetSkillRuntimeOrDefault(data)?.PerformSkill();
                    break;
            }
        }

        protected virtual void UpdateCooldown()
        {
            foreach (var runtime in _skillRuntimeDictionary)
                runtime.Value.UpdateCoolDown(Time.fixedDeltaTime);
        }

        public virtual void CancelAllSkill()
        {
            GetSkillRuntimeOrDefault(primarySkillData)?.CancelSkill();

            foreach (var data in _autoSkillDatas)
            {
                GetSkillRuntimeOrDefault(data)?.CancelSkill();
            }
        }

        private void ResetToDefaultSkill()
        {
            if (!owner) return;

            SetOrAddSkill(_defaultPrimarySkillData, SkillType.PrimarySkill);
            SetOrAddSkill(_defaultSecondarySkillData, SkillType.SecondarySkill);

            _autoSkillDatas.Clear();
            foreach (var data in _defaultAutoSkillDatas)
                SetOrAddSkill(data, SkillType.AutoSkill);
        }

        public void ResetSkillSystem()
        {
            CancelAllSkill();
            ResetToDefaultSkill();
        }

        #endregion
    }
}
