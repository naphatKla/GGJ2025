using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.StatusEffectSO;
using Manager;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public class StunEffect : BaseStatusEffect<StunEffectDataSo>
    {
        private bool _isStunSuccess;
        
        public override void OnStart(BaseController owner)
        {
            if (owner.HealthSystem.IsInvincible)
            {
                ClearThisEffect();
                return;
            }

            if (StatusEffectManager.TryGetEffect(owner.gameObject, StatusEffectName.IronBody, out BaseStatusEffect eff))
            {
                return;
            }
            
            _isStunSuccess = true;
            owner.MovementSystem.StopFromStun(true);
            owner.SkillSystem.SetCanUseSkills(false);
            owner.TryPlayFeedback(FeedbackName.Character.Stun);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
            
        }

        public override void OnExit(BaseController owner)
        {
            if (!_isStunSuccess) return;
  
            owner.MovementSystem.StopFromStun(false);
            owner.SkillSystem.SetCanUseSkills(true);
            owner.TryStopFeedback(FeedbackName.Character.Stun);
        }
    }
}
