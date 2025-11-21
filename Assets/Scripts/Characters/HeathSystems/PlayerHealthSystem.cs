using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, int> _takeDamageAmountDictionary = new(); // attacker id, hit amount
        private readonly Dictionary<string, int> _diedAmountDictionary = new();
        public Dictionary<string, int> TakeDamageAmountDictionary => _takeDamageAmountDictionary;
        public Dictionary<string, int> DiedAmountDictionary => _diedAmountDictionary;


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

            if (String.IsNullOrEmpty(hitInfo.attackerId)) return;
            _takeDamageAmountDictionary.TryAdd(hitInfo.attackerId, 0);
            _takeDamageAmountDictionary[hitInfo.attackerId]++;
            
            if (currentHealth > 0) return;
            _diedAmountDictionary.TryAdd(hitInfo.attackerId, 0);
            _diedAmountDictionary[hitInfo.attackerId]++;
        }

        public void OnCounterDash()
        {
            ConsumeHitAttempt();
            ConsumePendingHit();
            
            _lastTimeCounterDash = Time.time;
        }
    }
}