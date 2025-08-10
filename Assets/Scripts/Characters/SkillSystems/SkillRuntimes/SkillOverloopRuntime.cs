using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillOverloopRuntime : BaseSkillRuntime<SkillOverloopDataSo>
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
