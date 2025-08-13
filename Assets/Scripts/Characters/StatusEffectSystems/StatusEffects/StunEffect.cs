using Characters.Controllers;
using Characters.SO.StatusEffectSO;

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

            _isStunSuccess = true;
            owner.MovementSystem.StopFromStun(true);
            owner.SkillSystem.SetCanUseSkills(false);
        }

        public override void OnUpdate(BaseController owner, float deltaTime)
        {
            
        }

        public override void OnExit(BaseController owner)
        {
            if (!_isStunSuccess) return;
  
            owner.MovementSystem.StopFromStun(false);
            owner.SkillSystem.SetCanUseSkills(true);
        }
    }
}
