using System.Collections;
using Characters.Controllers;
using ProjectExtensions;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public abstract class BaseSkill : MonoBehaviour
    {
        public float cooldownDuration;
        private bool _isPerforming;
        protected BaseController owner;
        protected Vector2 aimDirection;
        
        
        public void PerformSkill(BaseController owner, Vector2 direction)
        {
            if (_isPerforming) return;
            this.owner = owner;
            aimDirection = direction;
            
            HandleSkillStart();
            StartCoroutine(OnSkillUpdate().WithCallback(HandleSkillExit));
        }
        
        protected virtual void HandleSkillStart()
        {
            _isPerforming = true;
            OnSkillStart();
        }
        
        protected virtual void HandleSkillExit()
        {
            _isPerforming = false;
            OnSkillExit();
        }

        protected abstract void OnSkillStart();
        protected abstract IEnumerator OnSkillUpdate();
        protected abstract void OnSkillExit();
        
    }
}
