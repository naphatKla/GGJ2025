using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillLightStep : BaseSkillRuntime<SkillLightStepDataSo>
    {
        protected override void OnSkillStart()
        {
          
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnSkillExit()
        {
          
        }
    }
}
