using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillPhotonBarrageRuntime : BaseSkillRuntime<SkillPhotonBarrageDataSo>
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
