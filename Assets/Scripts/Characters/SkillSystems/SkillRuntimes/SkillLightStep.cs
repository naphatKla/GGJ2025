using System;
using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillLightStep : BaseSkillRuntime<SkillLightStepDataSo>, ISpecialConditionSkill
    {
        public bool IsWaitForCondition { get; private set; }
        
        protected override void OnSkillStart()
        {
            IsWaitForCondition = true;
            owner.CombatSystem.OnCounterAttack += TriggerCondition;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask.WaitUntil(() => !IsWaitForCondition, cancellationToken: cancelToken);
            if (cancelToken.IsCancellationRequested) return;
            
        }

        protected override void OnSkillExit()
        {
            owner.CombatSystem.OnCounterAttack -= TriggerCondition;
        }

        private void OnDisable()
        {
            owner.CombatSystem.OnCounterAttack -= TriggerCondition;
        }

        protected void TriggerCondition() => IsWaitForCondition = false;

    }
}
