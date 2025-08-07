using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
using Characters.SO.CharacterDataSO;
using Characters.SO.SkillDataSo;
using Manager;
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

    public class SkillSystem : MonoBehaviour, IFixedUpdateable
    {
        public event Action<float, int> OnSkillCooldownUpdate;
        public event Action<int> OnSkillCooldownReset;
        public event Action<BaseSkillDataSo, int> OnNewSkillAssign;

        protected BaseSkillDataSo primarySkillData;
        protected BaseSkillDataSo secondarySkillData;
        private int autoSkillSlot;
        private List<BaseSkillDataSo> autoSkillDataList = new();

        private HashSet<BaseSkillDataSo> _autoSkillDatas = new();
        private BaseSkillDataSo _defaultPrimarySkillData;
        private BaseSkillDataSo _defaultSecondarySkillData;
        private HashSet<BaseSkillDataSo> _defaultAutoSkillDatas = new();

        private readonly Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillRuntimeDictionary = new();
        private readonly List<BaseSkillDataSo> _pendingRuntimeRemoval = new();

        protected BaseController owner;
        protected bool canUseSkills = true;
        protected bool canUsePrimary = true;
        protected bool canUseSecondary = true;

        public virtual void AssignData(BaseController owner, BaseSkillDataSo primary, BaseSkillDataSo secondary,
            List<BaseSkillDataSo> autoList, int autoSlot)
        {
            this.owner = owner;

            autoSkillSlot = Mathf.Max(0, autoSlot);
            autoSkillDataList = new List<BaseSkillDataSo>(autoList ?? new());

            while (autoSkillDataList.Count > autoSkillSlot)
                autoSkillDataList.RemoveAt(autoSkillDataList.Count - 1);

            _defaultPrimarySkillData = primary;
            _defaultSecondarySkillData = secondary;
            _defaultAutoSkillDatas = new(autoSkillDataList);

            ResetToDefaultSkill();
        }

        public virtual void AssignData(BaseController owner, BaseCharacterDataSo dataSO)
        {
            AssignData(owner, dataSO.PrimarySkillData, dataSO.SecondarySkillData, dataSO.AutoSkillDataList,
                dataSO.AutoSkillSlot);
        }

        public (BaseSkillDataSo Primary, BaseSkillDataSo Secondary, List<BaseSkillDataSo> Auto,
            IEnumerable<BaseSkillDataSo> All) GetAllCurrentSkillDatas()
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
                    RemoveUnusedSkillRuntime(autoSkill);
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

            BaseSkillRuntime runtime = null;

            switch (type)
            {
                case SkillType.PrimarySkill:
                    if (secondarySkillData == newSkillData || _autoSkillDatas.Contains(newSkillData)) return;
                    if (primarySkillData != null && primarySkillData != newSkillData)
                        RemoveUnusedSkillRuntime(primarySkillData);
                    primarySkillData = newSkillData;
                    
                    InstantiateSkillRuntime(primarySkillData);
                    runtime = GetSkillRuntimeOrDefault(newSkillData);

                    if (runtime is IAutoSkillTriggerSource primaryTriggerSource)
                    {
                        primaryTriggerSource.OnTriggerAutoSkill -= OnTriggerAutoSkill;
                        primaryTriggerSource.OnTriggerAutoSkill += OnTriggerAutoSkill;
                    }
                    else
                    {
                        Debug.LogError("Primary skill need IAutoSkillTriggerSource to trigger auto skill");
                        throw new NotImplementedException();
                    }

                    break;
                case SkillType.SecondarySkill:
                    if (primarySkillData == newSkillData || _autoSkillDatas.Contains(newSkillData)) return;
                    if (secondarySkillData != null && secondarySkillData != newSkillData)
                        RemoveUnusedSkillRuntime(secondarySkillData);
                    secondarySkillData = newSkillData;
                    
                    InstantiateSkillRuntime(secondarySkillData);
                    runtime = GetSkillRuntimeOrDefault(newSkillData);

                    if (runtime is IAutoSkillTriggerSource secondarySkillTriggerSource)
                    {
                        secondarySkillTriggerSource.OnTriggerAutoSkill -= OnTriggerAutoSkill;
                        secondarySkillTriggerSource.OnTriggerAutoSkill += OnTriggerAutoSkill;
                    }
                    else
                    {
                        Debug.LogError("Secondary skill need IAutoSkillTriggerSource to trigger auto skill");
                        throw new NotImplementedException();
                    }
                    break;
                case SkillType.AutoSkill:
                    if (primarySkillData == newSkillData || secondarySkillData == newSkillData ||
                        _autoSkillDatas.Contains(newSkillData)) return;
                    _autoSkillDatas.Add(newSkillData);
                    InstantiateSkillRuntime(newSkillData);
                    runtime = GetSkillRuntimeOrDefault(newSkillData);
                    break;
            }

            runtime.SetCurrentCooldown(0);
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

        private void RemoveUnusedSkillRuntime(BaseSkillDataSo oldSkill)
        {
            if (!_skillRuntimeDictionary.TryGetValue(oldSkill, out var runtime)) return;
            if (runtime.IsPerforming)
            {
                if (runtime is ISpecialConditionSkill { IsWaitForCondition: true })
                {
                    runtime.CancelSkill();
                    Destroy(runtime);
                    if (runtime is IAutoSkillTriggerSource autoSkillTriggerSource)
                        autoSkillTriggerSource.OnTriggerAutoSkill -= OnTriggerAutoSkill;
                    _skillRuntimeDictionary.Remove(oldSkill);
                }
                
                if (!_pendingRuntimeRemoval.Contains(oldSkill))
                    _pendingRuntimeRemoval.Add(oldSkill);
            }
            else
            {
                Destroy(runtime);
                if (runtime is IAutoSkillTriggerSource autoSkillTriggerSource)
                    autoSkillTriggerSource.OnTriggerAutoSkill -= OnTriggerAutoSkill;
                _skillRuntimeDictionary.Remove(oldSkill);
            }
        }

        private void CleanupPendingRuntimes()
        {
            for (int i = _pendingRuntimeRemoval.Count - 1; i >= 0; i--)
            {
                var skill = _pendingRuntimeRemoval[i];
                var runtime = GetSkillRuntimeOrDefault(skill);
                if (runtime == null || !runtime.IsPerforming)
                {
                    Destroy(runtime);
                    if (runtime is IAutoSkillTriggerSource autoSkillTriggerSource)
                        autoSkillTriggerSource.OnTriggerAutoSkill -= OnTriggerAutoSkill;
                    _skillRuntimeDictionary.Remove(skill);
                    _pendingRuntimeRemoval.RemoveAt(i);
                }
            }
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
                    if (!canUsePrimary) return;
                    GetSkillRuntimeOrDefault(primarySkillData)?.PerformSkill();
                    break;
                case SkillType.SecondarySkill:
                    if (!canUseSecondary) return;
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
                bool wasCooling = runtime.IsCooldown;
                runtime.UpdateCoolDown(Time.fixedDeltaTime);

                if (!runtime.IsCooldown)
                {
                    if (wasCooling)
                        OnSkillCooldownReset?.Invoke(index);
                    continue;
                }

                float progress = 1f - Mathf.Clamp01(runtime.CurrentCooldown / runtime.Cooldown);
                OnSkillCooldownUpdate?.Invoke(progress, index);
            }
        }

        private void OnTriggerAutoSkill()
        {
            PerformSkill(SkillType.AutoSkill);
        }

        public virtual void CancelAllSkill()
        {
            GetSkillRuntimeOrDefault(primarySkillData)?.CancelSkill();
            foreach (var data in _autoSkillDatas)
                GetSkillRuntimeOrDefault(data)?.CancelSkill();
        }

        private void CleanupUnusedRuntimesAfterReset()
        {
            var usedSkills = new HashSet<BaseSkillDataSo>
            {
                primarySkillData,
                secondarySkillData
            };
            usedSkills.UnionWith(_autoSkillDatas);

            foreach (var kvp in _skillRuntimeDictionary.ToList())
            {
                var data = kvp.Key;
                var runtime = kvp.Value;
                if (usedSkills.Contains(data)) continue;

                runtime.CancelSkill();
                if (runtime.IsPerforming)
                {
                    if (!_pendingRuntimeRemoval.Contains(data))
                        _pendingRuntimeRemoval.Add(data);
                }
                else
                {
                    Destroy(runtime);
                    if (runtime is IAutoSkillTriggerSource autoSkillTriggerSource)
                        autoSkillTriggerSource.OnTriggerAutoSkill -= OnTriggerAutoSkill;
                    _skillRuntimeDictionary.Remove(data);
                }
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

        public void SetCanUseSkills(bool enable) => canUseSkills = enable;
        public void SetCanUsePrimary(bool enable) => canUsePrimary = enable;
        public void SetCanUseSecondary(bool enable) => canUseSecondary = enable;

        public virtual void ResetSkillSystem()
        {
            CancelAllSkill();
            ResetToDefaultSkill();
            CleanupUnusedRuntimesAfterReset();
            SetCanUsePrimary(true);
            SetCanUseSecondary(true);
            SetCanUseSkills(true);
        }
        
        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            FixedUpdateManager.Instance.Unregister(this);
        }

        public void OnFixedUpdate()
        {
            if (!owner) return;
            UpdateCooldown();
            CleanupPendingRuntimes();
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