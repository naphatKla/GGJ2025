using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillSpaceMineRuntime : BaseSkillRuntime<SkillSpaceMineDataSo>
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
