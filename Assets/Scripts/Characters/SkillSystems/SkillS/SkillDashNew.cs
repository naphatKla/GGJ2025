using System.Collections;
using Characters.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public class SkillDashNew : BaseSkill
    {
        [Title("SkillDash")] [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float dashDistance = 8f;
        
        protected override void OnSkillStart(BaseController owner)
        {
               
        }

        protected override IEnumerator OnSkillUpdate(BaseController owner)
        {
            yield return null;
        }
    }
}
