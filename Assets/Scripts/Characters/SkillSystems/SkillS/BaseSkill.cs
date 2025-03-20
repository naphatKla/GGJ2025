using Characters.Controllers;
using DG.Tweening;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public abstract class BaseSkill : MonoBehaviour
    {
        public float cooldownDuration;
        private bool _isPerforming;
        
        public void PerformSkill(BaseController owner, Vector2 direction)
        {
            if (_isPerforming) return;
            HandleSkillStart();
            Tween skillTween = OnSkillUpdate(owner, direction).OnComplete(HandleSkillExit);
            if (skillTween == null) HandleSkillExit();
        }
        
        protected virtual void HandleSkillStart()
        {
            _isPerforming = true;
            OnSkillStart();
        }
        
        protected virtual void HandleSkillExit()
        {
            Debug.Log("EXIT");
            _isPerforming = false;
            OnSkillExit();
        }

        protected abstract void OnSkillStart();
        protected abstract Tween OnSkillUpdate(BaseController owner, Vector2 direction);
        protected abstract void OnSkillExit();
    }
}
