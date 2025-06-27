using Characters.FeedbackSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class EnemySkillSystem : SkillSystem
    {
        [SerializeField] private float chargeMockupDelay = 0.5f;
        
        // TODO: Delete this and implement system, this is for mockup test
        private bool _isCharging;
        public override async void PerformPrimarySkill()
        {
            if (!_owner) return;
            if (_isCharging) return;
            if (_primarySkillCooldown > chargeMockupDelay) return;

            _isCharging = true;
            _owner.FeedbackSystem.PlayFeedback(FeedbackName.Charge);
            await UniTask.WaitForSeconds(chargeMockupDelay);
            base.PerformPrimarySkill();
            _isCharging = false;
        }
    }
}
