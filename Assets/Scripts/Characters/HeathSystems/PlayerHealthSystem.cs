using Characters.CombatSystems;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Sirenix.OdinInspector;

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
            playerCombat   = owner.CombatSystem   as PlayerCombatSystem;
        }

        [Button]
        public override bool TakeDamage(HitInfo hitInfo)
        {
            // logic counter dash เดิม
            if (hitInfo.attacker &&
                owner.DamageOnTouch.IsEnableDamage &&
                hitInfo.attacker.DamageOnTouch.IsEnableDamage)
            {
                playerCombat.OnCounterAttackHandler();
                return false;
            }

            // ที่เหลือให้ base จัดการ (buffer attempt + pending + commit)
            return base.TakeDamage(hitInfo);
        }

        protected override void TakeDamageAction(HitInfo hitInfo)
        {
            // ทำตอนเลือดลดจริงแล้ว (หลังผ่าน BeforeHitDelay)
            base.TakeDamageAction(hitInfo);
            playerFeedback?.OpenFocusBlackDropOnHit(0.4f, hitInfo.realObjectAttack);
        }
    }
}