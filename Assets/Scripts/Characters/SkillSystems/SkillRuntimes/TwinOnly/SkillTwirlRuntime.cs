using System.Threading;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Characters.SO.SkillDataSo.TwinOnly;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes.TwinOnly
{
    public class SkillTwirlRuntime : BaseSkillRuntime<SkillTwirlDataSo>
    {
        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            if (owner is not TwinController)
            {
                Debug.LogError("Wrong Type of owner. Skill Twirl is made for The Twin Only!");
                return;
            }
            
            base.AssignSkillData(skillData, owner);
        }
        
        protected override void OnSkillStart()
        {
            owner.SkillSystem.SetCanUseSkills(false);
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnSkillExit()
        {
            owner.SkillSystem.SetCanUseSkills(true);
        }
    }
}
