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
        
        protected override void OnSkillStart(BaseController owner, Vector2 direction)
        {
            owner.ToggleMovementInputController(false);
            Vector2 dashPosition = (Vector2)owner.transform.position + (direction * dashDistance);
            owner.movementSystem.TryMoveToPositionOverTime(dashPosition, dashDuration);
            DOVirtual.DelayedCall(dashDuration, () => owner.ToggleMovementInputController(true));
        }

        protected override IEnumerator OnSkillUpdate(BaseController owner, Vector2 direction)
        {
            yield return null;
        }
    }
}
