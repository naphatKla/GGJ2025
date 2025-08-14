using System;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems
{
    public class EnemySkillSystem : SkillSystem
    {
        private float skillNotifyDelay = 0.5f;
        
        // TODO: Delete this and implement system, this is for mockup test
        private bool _isCharging;
        private CancellationTokenSource cts = new();

        public override void AssignData(BaseController owner, BaseCharacterDataSo dataSO)
        {
            base.AssignData(owner, dataSO);
            if (dataSO is EnemyDataSo enemyDataSo)
                skillNotifyDelay = enemyDataSo.DelayBeforePerformSkill;
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
                if (runtime.CurrentCooldown > skillNotifyDelay) return;
                _isCharging = true;
                owner.FeedbackSystem.PlayFeedback(FeedbackName.Character.NotifySkill);
                await UniTask
                    .WaitForSeconds(skillNotifyDelay, cancellationToken: cts.Token)
                    .SuppressCancellationThrow();
                _isCharging = false;
            }
            
            if (cts.IsCancellationRequested) return;
            base.PerformSkill(type);
        }

        public override void ResetSkillSystem()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            base.ResetSkillSystem();
        }
    }
}
