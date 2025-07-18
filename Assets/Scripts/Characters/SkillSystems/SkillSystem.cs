using System;
using System.Collections.Generic;
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
        [SerializeField] private List<BaseSkillDataSo> autoSkillDataList; // skill input from inspector
        
        private HashSet<BaseSkillDataSo> _autoSkillDatas = new(); // this is the real collection of auto skill system
        private BaseSkillDataSo _defaultPrimarySkillData;
        private BaseSkillDataSo _defaultSecondarySkillData;
        private HashSet<BaseSkillDataSo> _defaultAutoSkillDatas = new();

        private readonly Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillRuntimeDictionary = new();
        protected BaseController owner; // _owner == null; means not initialized;

        #endregion

        #region Inspector Duplicate Check (Odin)

#if UNITY_EDITOR
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
#endif

        #endregion

        #region Unity Methods

        private void FixedUpdate()
        {
            if (!owner) return;
            UpdateCooldown();
        }

        #endregion

        #region Methods

        public void Initialize(BaseController owner)
        {
            this.owner = owner;

            foreach (var baseSkillDataSo in autoSkillDataList)
                _autoSkillDatas.Add(baseSkillDataSo);

            _defaultPrimarySkillData = primarySkillData;
            _defaultSecondarySkillData = secondarySkillData;
            _defaultAutoSkillDatas = new(_autoSkillDatas);
            ResetToDefaultSkill();
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

        /// <summary>
        /// Add or set skill for the specified slot, with duplicate check (prevent duplicate between primary, secondary, auto)
        /// </summary>
        public virtual void SetOrAddSkill(BaseSkillDataSo newSkillData, SkillType type)
        {
            if (!owner) return;
            if (!newSkillData) return;

            // --- Duplicate check ---
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
