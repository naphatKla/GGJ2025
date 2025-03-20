using System;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public abstract class BaseSkill : ScriptableObject
    {
        #region Inspectors & Variables
        [SerializeField] private float cooldownDuration;
        public Action OnSkillActivated { get; set; }
        public Action OnSkillUpdated { get; set; }
        public Action OnSkillDeactivated { get; set; }
        #endregion
        
        public virtual void ActivateSkill()
        {
            OnSkillActivated?.Invoke();
            HandleSkillStart();
        }

        public virtual void UpdateSkill()
        {
            OnSkillUpdated?.Invoke();
            HandleSkillUpdate();
        }

        public virtual void DeactivateSkill()
        {
            OnSkillDeactivated?.Invoke();
            HandleSkillEnd();
        }
        
        protected abstract void HandleSkillStart();
        protected abstract void HandleSkillUpdate();
        protected abstract void HandleSkillEnd();
    }
}
