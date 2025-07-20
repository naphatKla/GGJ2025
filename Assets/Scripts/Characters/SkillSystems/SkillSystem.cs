using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
using Characters.SO.CharacterDataSO;
using Characters.SO.SkillDataSo;
using UnityEngine;

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
        protected BaseSkillDataSo primarySkillData;
        private BaseSkillDataSo secondarySkillData;
        private int autoSkillSlot;
        private List<BaseSkillDataSo> autoSkillDataList = new();

        // Runtime only
        private HashSet<BaseSkillDataSo> _autoSkillDatas = new();
        private BaseSkillDataSo _defaultPrimarySkillData;
        private BaseSkillDataSo _defaultSecondarySkillData;
        private HashSet<BaseSkillDataSo> _defaultAutoSkillDatas = new();

        private readonly Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillRuntimeDictionary = new();
        protected BaseController owner;

        /// <summary>
        /// Assigns all config data from SO (call on spawn/init).
        /// </summary>
        public virtual void AssignData(BaseController owner, BaseSkillDataSo primary, BaseSkillDataSo secondary,
            List<BaseSkillDataSo> autoList, int autoSlot)
        {
            this.owner = owner;
            primarySkillData = primary;
            secondarySkillData = secondary;

            autoSkillSlot = Mathf.Max(0, autoSlot);
            autoSkillDataList = new List<BaseSkillDataSo>(autoList ?? new List<BaseSkillDataSo>());
            // trim list 
            while (autoSkillDataList.Count > autoSkillSlot)
                autoSkillDataList.RemoveAt(autoSkillDataList.Count - 1);

            // Build runtime collections
            _autoSkillDatas.Clear();
            foreach (var skill in autoSkillDataList)
                _autoSkillDatas.Add(skill);

            _defaultPrimarySkillData = primarySkillData;
            _defaultSecondarySkillData = secondarySkillData;
            _defaultAutoSkillDatas = new(_autoSkillDatas);
            ResetToDefaultSkill();
        }

        // Convenience: assign SO directly
        public virtual void AssignData(BaseController owner, BaseCharacterDataSo dataSO)
        {
            AssignData(owner, dataSO.PrimarySkillData, dataSO.SecondarySkillData, dataSO.AutoSkillDataList,
                dataSO.AutoSkillSlot);
        }

        public (BaseSkillDataSo Primary, BaseSkillDataSo Secondary, List<BaseSkillDataSo> Auto,
            IEnumerable<BaseSkillDataSo> All)
            GetAllCurrentSkillDatas()
        {
            var all = new List<BaseSkillDataSo>();
            if (primarySkillData != null) all.Add(primarySkillData);
            if (secondarySkillData != null) all.Add(secondarySkillData);
            all.AddRange(_autoSkillDatas);
            return (primarySkillData, secondarySkillData, _autoSkillDatas.ToList(), all);
        }

        public void UpgradeSkillSlot(BaseSkillDataSo upgradeSkill)
        {
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

            var oldAuto = _autoSkillDatas.FirstOrDefault(s => s.NextSkillDataUpgrade == upgradeSkill);
            if (oldAuto != null)
            {
                _autoSkillDatas.Remove(oldAuto);
                SetOrAddSkill(upgradeSkill, SkillType.AutoSkill);
                Debug.Log($"[SkillSystem] Upgraded auto skill to: {upgradeSkill.name}");
                return;
            }

            if (_autoSkillDatas.Count < autoSkillSlot)
            {
                SetOrAddSkill(upgradeSkill, SkillType.AutoSkill);
                Debug.Log($"[SkillSystem] Added new auto skill: {upgradeSkill.name}");
                return;
            }

            Debug.LogWarning(
                $"[SkillSystem] Cannot upgrade/add skill {upgradeSkill.name}: all slots full and no match for upgrade.");
        }

        public SkillType? GetSlotUpgradeTarget(BaseSkillDataSo upgradeSkill)
        {
            if (primarySkillData == null || primarySkillData.NextSkillDataUpgrade == upgradeSkill)
                return SkillType.PrimarySkill;
            if (secondarySkillData == null || secondarySkillData.NextSkillDataUpgrade == upgradeSkill)
                return SkillType.SecondarySkill;
            if (_autoSkillDatas.Any(s => s.NextSkillDataUpgrade == upgradeSkill) ||
                _autoSkillDatas.Count < autoSkillSlot)
                return SkillType.AutoSkill;
            return null;
        }

        public bool HasEmptySlot(SkillType type)
        {
            switch (type)
            {
                case SkillType.PrimarySkill: return primarySkillData == null;
                case SkillType.SecondarySkill: return secondarySkillData == null;
                case SkillType.AutoSkill: return _autoSkillDatas.Count < autoSkillSlot;
                default: return false;
            }
        }

        public bool ContainsSkill(BaseSkillDataSo skillData)
        {
            if (skillData == null) return false;
            return skillData == primarySkillData
                   || skillData == secondarySkillData
                   || _autoSkillDatas.Contains(skillData);
        }

        private void InstantiateSkillRuntime(BaseSkillDataSo skillData)
        {
            if (!skillData) return;
            if (!owner) return;
            if (_skillRuntimeDictionary.ContainsKey(skillData)) return;
            BaseSkillRuntime skillRuntime = (BaseSkillRuntime)gameObject.AddComponent(skillData.SkillRuntime);
            skillRuntime.AssignSkillData(skillData, owner);
            _skillRuntimeDictionary.Add(skillData, skillRuntime);
        }

        public virtual void SetOrAddSkill(BaseSkillDataSo newSkillData, SkillType type)
        {
            if (!owner || !newSkillData) return;
            switch (type)
            {
                case SkillType.PrimarySkill:
                    if ((secondarySkillData == newSkillData) || _autoSkillDatas.Contains(newSkillData))
                    {
                        Debug.LogWarning(
                            $"[SkillSystem] Duplicate skill ({newSkillData.name}) detected! Cannot assign as Primary because it already exists in Secondary or AutoSkill.");
                        return;
                    }

                    break;
                case SkillType.SecondarySkill:
                    if ((primarySkillData == newSkillData) || _autoSkillDatas.Contains(newSkillData))
                    {
                        Debug.LogWarning(
                            $"[SkillSystem] Duplicate skill ({newSkillData.name}) detected! Cannot assign as Secondary because it already exists in Primary or AutoSkill.");
                        return;
                    }

                    break;
                case SkillType.AutoSkill:
                    if ((primarySkillData == newSkillData) || (secondarySkillData == newSkillData) ||
                        _autoSkillDatas.Contains(newSkillData))
                    {
                        Debug.LogWarning(
                            $"[SkillSystem] Duplicate skill ({newSkillData.name}) detected! Cannot assign as AutoSkill because it already exists in Primary/Secondary/AutoSkill.");
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
                    GetSkillRuntimeOrDefault(primarySkillData)?.PerformSkill();
                    PerformSkill(SkillType.AutoSkill);
                    break;
                case SkillType.SecondarySkill:
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
                GetSkillRuntimeOrDefault(data)?.CancelSkill();
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

        private void FixedUpdate()
        {
            if (!owner) return;
            UpdateCooldown();
        }
    }
}