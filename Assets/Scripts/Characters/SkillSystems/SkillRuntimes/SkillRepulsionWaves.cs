using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillRepulsionWaveRuntime : BaseSkillRuntime<SkillRepulsionWaveDataSo>
    {
        protected override void OnSkillStart()
        {
           
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnSkillExit()
        {
            
        }
    }
}
