using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
using Characters.SO.CharacterDataSO;
using Characters.SO.SkillDataSo;
using Sirenix.OdinInspector;
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
        // float = cool down progression 0 - 1
        // int index, 0 = primary skill, 1 = secondary, >2 = auto skill
        private event Action<float, int> OnSkillCooldownUpdate;
        private event Action<int> OnSkillCooldownReset;
        private event Action<BaseSkillDataSo, int> OnNewSkillAssign;

        protected BaseSkillDataSo primarySkillData;
        protected BaseSkillDataSo secondarySkillData;
        private int autoSkillSlot;
        private List<BaseSkillDataSo> autoSkillDataList = new();

        private HashSet<BaseSkillDataSo> _autoSkillDatas = new();
        private BaseSkillDataSo _defaultPrimarySkillData;
        private BaseSkillDataSo _defaultSecondarySkillData;
        private HashSet<BaseSkillDataSo> _defaultAutoSkillDatas = new();

        private readonly Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillRuntimeDictionary = new();
        protected BaseController owner;
        protected bool canUseSkills = true;

        public virtual void AssignData(BaseController owner, BaseSkillDataSo primary, BaseSkillDataSo secondary,
            List<BaseSkillDataSo> autoList, int autoSlot)
        {
            this.owner = owner;
            primarySkillData = primary;
            secondarySkillData = secondary;
            autoSkillSlot = Mathf.Max(0, autoSlot);
            autoSkillDataList = new List<BaseSkillDataSo>(autoList ?? new List<BaseSkillDataSo>());
            while (autoSkillDataList.Count > autoSkillSlot)
                autoSkillDataList.RemoveAt(autoSkillDataList.Count - 1);

            _autoSkillDatas.Clear();
            foreach (var skill in autoSkillDataList)
                _autoSkillDatas.Add(skill);

            _defaultPrimarySkillData = primarySkillData;
            _defaultSecondarySkillData = secondarySkillData;
            _defaultAutoSkillDatas = new(_autoSkillDatas);
            ResetToDefaultSkill();
        }

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

        public bool ContainsSkillWithSameRoot(BaseSkillDataSo skillData)
        {
            if (skillData == null) return false;
            var skillRoot = skillData.RootNode;
            if (primarySkillData != null && primarySkillData.RootNode == skillRoot) return true;
            if (secondarySkillData != null && secondarySkillData.RootNode == skillRoot) return true;
            foreach (var auto in _autoSkillDatas)
                if (auto != null && auto.RootNode == skillRoot)
                    return true;
            return false;
        }

        public void UpgradeSkillSlot(BaseSkillDataSo upgradeSkill)
        {
            var skillRoot = upgradeSkill.RootNode;

            if (primarySkillData == null && !ContainsSkillWithSameRoot(upgradeSkill))
            {
                SetOrAddSkill(upgradeSkill, SkillType.PrimarySkill);
                return;
            }

            if (primarySkillData?.RootNode == skillRoot && primarySkillData.NextSkillDataUpgrade == upgradeSkill)
            {
                SetOrAddSkill(upgradeSkill, SkillType.PrimarySkill);
                return;
            }

            if (secondarySkillData == null && !ContainsSkillWithSameRoot(upgradeSkill))
            {
                SetOrAddSkill(upgradeSkill, SkillType.SecondarySkill);
                return;
            }

            if (secondarySkillData?.RootNode == skillRoot && secondarySkillData.NextSkillDataUpgrade == upgradeSkill)
            {
                SetOrAddSkill(upgradeSkill, SkillType.SecondarySkill);
                return;
            }

            foreach (var autoSkill in _autoSkillDatas.ToList())
            {
                if (autoSkill.RootNode == skillRoot && autoSkill.NextSkillDataUpgrade == upgradeSkill)
                {
                    _autoSkillDatas.Remove(autoSkill);
                    SetOrAddSkill(upgradeSkill, SkillType.AutoSkill);
                    return;
                }
            }

            if (_autoSkillDatas.Count < autoSkillSlot && !ContainsSkillWithSameRoot(upgradeSkill))
            {
                SetOrAddSkill(upgradeSkill, SkillType.AutoSkill);
                return;
            }
        }

        [Button]
        public virtual void SetOrAddSkill(BaseSkillDataSo newSkillData, SkillType type)
        {
            if (!owner || !newSkillData) return;

            switch (type)
            {
                case SkillType.PrimarySkill:
                    if (secondarySkillData == newSkillData || _autoSkillDatas.Contains(newSkillData)) return;
                    break;
                case SkillType.SecondarySkill:
                    if (primarySkillData == newSkillData || _autoSkillDatas.Contains(newSkillData)) return;
                    break;
                case SkillType.AutoSkill:
                    if (primarySkillData == newSkillData || secondarySkillData == newSkillData ||
                        _autoSkillDatas.Contains(newSkillData)) return;
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

            var runtime = GetSkillRuntimeOrDefault(newSkillData);
            runtime?.SetCurrentCooldown(0);
            
            int index = GetSkillIndex(newSkillData);
            OnNewSkillAssign?.Invoke(newSkillData, index);
        }

        private void InstantiateSkillRuntime(BaseSkillDataSo skillData)
        {
            if (!skillData || !owner || _skillRuntimeDictionary.ContainsKey(skillData)) return;

            BaseSkillRuntime skillRuntime = (BaseSkillRuntime)gameObject.AddComponent(skillData.SkillRuntime);
            skillRuntime.AssignSkillData(skillData, owner);
            _skillRuntimeDictionary.Add(skillData, skillRuntime);
        }
        
        public virtual BaseSkillRuntime GetSkillRuntimeOrDefault(BaseSkillDataSo skillData)
        {
            return skillData && _skillRuntimeDictionary.TryGetValue(skillData, out var runtime) ? runtime : null;
        }

        public virtual void PerformSkill(SkillType type)
        {
            if (!owner || !canUseSkills) return;

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
            foreach (var kvp in _skillRuntimeDictionary)
            {
                var data = kvp.Key;
                var runtime = kvp.Value;
                int index = GetSkillIndex(data);
                bool isCooldownBeforeUpdate = runtime.IsCooldown;
                
                runtime.UpdateCoolDown(Time.fixedDeltaTime);
                
                if (!runtime.IsCooldown)
                {
                    if (isCooldownBeforeUpdate)
                        OnSkillCooldownReset?.Invoke(index);    
                    
                    continue;
                }
                
                float progression = 1f - Mathf.Clamp01(runtime.CurrentCooldown / runtime.Cooldown);
                OnSkillCooldownUpdate?.Invoke(progression, index);
            }
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

        public void SetCanUseSkills(bool enable) => canUseSkills = enable;

        public virtual void ResetSkillSystem()
        {
            CancelAllSkill();
            ResetToDefaultSkill();
        }

        private void FixedUpdate()
        {
            if (!owner) return;
            UpdateCooldown();
        }

        private int GetSkillIndex(BaseSkillDataSo data)
        {
            if (data == primarySkillData) return 0;
            if (data == secondarySkillData) return 1;
            var autoList = _autoSkillDatas.ToList();
            int autoIndex = autoList.IndexOf(data);
            return autoIndex >= 0 ? 2 + autoIndex : -1;
        }
    }
}