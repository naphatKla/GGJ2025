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
        
        protected override void OnSkillStart(BaseController owner, Vector2 direction)
        {
            owner.EnableMovementInputController(false);
            owner.movementSystem
        }

        protected override IEnumerator OnSkillUpdate(BaseController owner, Vector2 direction)
        {
            throw new System.NotImplementedException();
        }
    }
}
