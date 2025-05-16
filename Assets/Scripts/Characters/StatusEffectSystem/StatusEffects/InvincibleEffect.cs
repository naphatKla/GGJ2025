using Characters.HeathSystems;
using UnityEngine;

namespace Characters.StatusEffectSystem.StatusEffects
{
    public class InvincibleEffect : BaseStatusEffect
    {
        private HealthSystem _ownerHealthSystem;
        
        public override void OnStart(GameObject owner)
        {
            if (!owner.TryGetComponent(out _ownerHealthSystem))
            {
                ClearThisEffect();
                return;
            }   
            
            Debug.Log($"Start Iframe {CurrentDuration} sec, Level {Level}");
            _ownerHealthSystem.SetInvincible(true);
        }
        
        public override void OnUpdate(float deltaTime) {}

        public override void OnExit()
        {
            _ownerHealthSystem.SetInvincible(false);
            Debug.Log("End Iframe");
        }
    }
}
