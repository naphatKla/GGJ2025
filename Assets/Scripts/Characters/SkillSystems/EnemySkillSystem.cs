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

        public override async void PerformSkill(SkillType type)
        {
            if (!owner) return;
            
            if (type == SkillType.PrimarySkill)
            {
                if (_isCharging) return;
                var runtime = GetSkillRuntimeOrDefault(primarySkillData);
                if (!runtime) return;
                if (runtime.Cooldown > chargeMockupDelay) return;
                _isCharging = true;
                owner.FeedbackSystem.PlayFeedback(FeedbackName.Charge);
                await UniTask.WaitForSeconds(chargeMockupDelay);
                _isCharging = false;
            }
            
            base.PerformSkill(type);
        }
    }
}
