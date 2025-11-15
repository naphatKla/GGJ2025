using Characters.CombatSystems;
using Characters.Controllers;
using Characters.FeedbackSystems;
using UnityEngine;

namespace Characters.HeathSystems
{
    public class PlayerHealthSystem : HealthSystem
    {
        private PlayerFeedbackSystem playerFeedback;
        private PlayerCombatSystem playerCombat;
        
        public override void AssignHealthData(float maxHealth, float invincibleTimePerHit, BaseController owner = null)
        {
            base.AssignHealthData(maxHealth, invincibleTimePerHit, owner);
            playerFeedback = owner.FeedbackSystem as PlayerFeedbackSystem;
            playerCombat = owner.CombatSystem as PlayerCombatSystem;
        }

        public override bool TakeDamage(float damage, BaseController attacker, GameObject realObjectAttack)
        {
            if (attacker && owner.DamageOnTouch.IsEnableDamage && attacker.DamageOnTouch.IsEnableDamage)
            {
                playerCombat.OnCounterAttackHandler();
                return false;
            }
                
            return base.TakeDamage(damage, attacker, realObjectAttack);
        }

        protected override void TakeDamageAction(float damage, BaseController attacker, GameObject realObjectAttack)
        {
            base.TakeDamageAction(damage, attacker, realObjectAttack);
            playerFeedback.OpenFocusBlackDropOnHit(0.4f, realObjectAttack);
        }
    }
}
