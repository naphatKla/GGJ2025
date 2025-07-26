using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillParryRuntime : BaseSkillRuntime<SkillParryDataSo>
    {
        private bool _isParryTrigger;
        
        protected override void OnSkillStart()
        {
            _isParryTrigger = false;
            owner.HealthSystem.OnTakeDamage += TriggerParry;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask.WaitUntil(() => _isParryTrigger, cancellationToken: cancelToken);
            Debug.Log("Trigger Parry");
        }

        protected override void OnSkillExit()
        {
            _isParryTrigger = false;
            owner.HealthSystem.OnTakeDamage -= TriggerParry;
        }

        private void TriggerParry()
        {
            _isParryTrigger = true;
        }
    }
}
