using System.Collections;
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
            owner.ToggleMovementInputController(false);
        }

        protected override IEnumerator OnSkillUpdate()
        {
            Vector2 dashPosition = (Vector2)transform.position + aimDirection * dashDistance;
            yield return owner.movementSystem.TryMoveToPositionOverTime(dashPosition, dashDuration).WaitForCompletion();
        }

        protected override void OnSkillExit()
        {
            owner.ToggleMovementInputController(true);
        }
    }
}