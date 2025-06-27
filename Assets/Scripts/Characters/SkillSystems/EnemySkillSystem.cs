using Characters.FeedbackSystems;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems
{
    public class EnemySkillSystem : SkillSystem
    {
        // TODO: Delete this and implement system, this is for mockup test
        private bool _isCharging;
        public override async void PerformPrimarySkill()
        {
            if (!_owner) return;
            if (_isCharging) return;
            if (_primarySkillCooldown > 0.5f) return;

            _isCharging = true;
            _owner.FeedbackSystem.PlayFeedback(FeedbackName.Charge);
            await UniTask.Delay(500);
            base.PerformPrimarySkill();
            _isCharging = false;
        }
    }
}
