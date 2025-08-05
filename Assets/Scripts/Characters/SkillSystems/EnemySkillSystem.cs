using System;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems
{
    public class EnemySkillSystem : SkillSystem
    {
        private float skillChargeDelay = 0.5f;
        
        // TODO: Delete this and implement system, this is for mockup test
        private bool _isCharging;

        public override void AssignData(BaseController owner, BaseCharacterDataSo dataSO)
        {
            base.AssignData(owner, dataSO);
            if (dataSO is EnemyDataSo enemyDataSo)
                skillChargeDelay = enemyDataSo.SkillChargeDelay;
            else throw new FormatException();
        }

        public override async void PerformSkill(SkillType type)
        {
            if (!owner) return;
            
            if (type == SkillType.PrimarySkill)
            {
                if (_isCharging) return;
                var runtime = GetSkillRuntimeOrDefault(primarySkillData);
                if (!runtime) return;
                if (runtime.Cooldown > skillChargeDelay) return;
                _isCharging = true;
                owner.FeedbackSystem.PlayFeedback(FeedbackName.Charge);
                await UniTask.WaitForSeconds(skillChargeDelay);
                _isCharging = false;
            }
            
            base.PerformSkill(type);
        }
    }
}
