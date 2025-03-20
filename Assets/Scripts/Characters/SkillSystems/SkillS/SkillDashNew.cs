using System.Collections;
using Characters.Controllers;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    public class SkillDashNew : BaseSkill
    {
        [Title("SkillDash")] [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float dashDistance = 8f;

        protected override void OnSkillStart()
        {
            throw new System.NotImplementedException();
        }

        protected override Tween OnSkillUpdate(BaseController owner, Vector2 direction)
        {
            DOVirtual.DelayedCall(2, () => { });
            owner.ToggleMovementInputController(false);
            Vector2 dashPosition = (Vector2)owner.transform.position + (direction * dashDistance);

            return owner.movementSystem.TryMoveToPositionOverTime(dashPosition, dashDuration);
        }

        protected override void OnSkillExit()
        {
            throw new System.NotImplementedException();
        }
    }
}