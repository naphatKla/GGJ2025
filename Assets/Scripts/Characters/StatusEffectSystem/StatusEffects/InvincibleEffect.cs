using Characters.HeathSystems;
using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public class InvincibleEffect : BaseStatusEffect
    {
        private HealthSystem _healthSystem;
        
        protected override void OnStart(GameObject owner)
        {
            if (owner.TryGetComponent<HealthSystem>(out _healthSystem))
            {
                ClearEffect();
                return;
            }   
            
            _healthSystem.SetInvincible(true);
        }
        
        protected override void OnUpdate(GameObject owner, float deltaTime) { }

        protected override void OnExit(GameObject owner)
        {
            _healthSystem.SetInvincible(false);
        }
    }
}
