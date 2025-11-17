using Characters.Controllers;
using Characters.FeedbackSystems;
using UnityEngine;

namespace Characters.HeathSystems
{
    public class PlayerHealthSystem : HealthSystem
    {
        private PlayerFeedbackSystem playerFeedback;
        private const float _counterDashIgnoreDamageDuration = 0.075f;
        private float _lastTimeCounterDash;

        public override void AssignHealthData(float maxHealth, float invincibleTimePerHit, BaseController owner = null)
        {
            base.AssignHealthData(maxHealth, invincibleTimePerHit, owner);

            playerFeedback = owner.FeedbackSystem as PlayerFeedbackSystem;
        }

        public override bool TakeDamage(HitInfo hitInfo)
        {
            if (IsDead) return false;
            if (Time.time <= _lastTimeCounterDash + _counterDashIgnoreDamageDuration)
            {
                BufferHitAttempt(hitInfo);
                return false;
            }
            
            return base.TakeDamage(hitInfo);
        }

        protected override void TakeDamageAction(HitInfo hitInfo)
        {
            // ทำตอนเลือดลดจริงแล้ว (หลังผ่าน BeforeHitDelay)
            base.TakeDamageAction(hitInfo);
            playerFeedback?.OpenFocusBlackDropOnHit(0.4f, hitInfo.realObjectAttack);
        }

        public void OnCounterDash()
        {
            ConsumeHitAttempt();
            ConsumePendingHit();
            
            _lastTimeCounterDash = Time.time;
        }
    }
}