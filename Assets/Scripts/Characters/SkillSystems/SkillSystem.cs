using System;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SkillSystems.SkillRuntimes;
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
        #region Inspector & Variables

        [SerializeField] protected BaseSkillDataSo primarySkillData;
        [SerializeField] private BaseSkillDataSo secondarySkillData;
        [SerializeField] private List<BaseSkillDataSo> autoSkillDataList; // skill input from inspector
        private HashSet<BaseSkillDataSo> _autoSkillDatas = new(); // this is the real collection of auto skill system

        private BaseSkillDataSo _defaultPrimarySkillData;
        private BaseSkillDataSo _defaultSecondarySkillData;
        private HashSet<BaseSkillDataSo> _defaultAutoSkillDatas = new();

        private readonly Dictionary<BaseSkillDataSo, BaseSkillRuntime> _skillRuntimeDictionary = new();
        protected BaseController owner; // _owner == null; means not initialized;

        #endregion

        #region Unity Methods
        
        private void FixedUpdate()
        {
            if (!owner) return;
           
            PerformSkill(SkillType.AutoSkill);
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
        
        public virtual void SetOrAddSkill(BaseSkillDataSo newSkillData, SkillType type)
        {
            if (!owner) return;
            if (!newSkillData) return;
            
            InstantiateSkillRuntime(newSkillData);
            
            switch (type)
            {
                case SkillType.PrimarySkill :
                    primarySkillData = newSkillData;
                    break;
                case SkillType.SecondarySkill :
                    secondarySkillData = newSkillData;
                    break;
                case SkillType.AutoSkill :
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
                case SkillType.PrimarySkill :
                    Debug.Log("Perform Primary");
                    GetSkillRuntimeOrDefault(primarySkillData)?.PerformSkill();
                    break;
                
                case SkillType.SecondarySkill :
                    Debug.Log("Perform Secondary");
                    GetSkillRuntimeOrDefault(secondarySkillData)?.PerformSkill();
                    break;
                
                case SkillType.AutoSkill :
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
            BaseSkillRuntime skillRuntime;
            
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
