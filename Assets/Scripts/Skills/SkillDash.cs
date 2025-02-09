using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillDash : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillDash")] [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float dashDistance = 8f;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        protected override void OnSkillStart()
        {
            OwnerCharacter.IsDash = true;
            OwnerCharacter.StopMovementController();
            Vector2 direction = GetTargetDirection();
            Vector2 dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * dashDistance);
            dashPosition = OwnerCharacter.ClampMovePositionToBound(dashPosition);
            OwnerCharacter.ClampMovePositionToBound(dashPosition);
            OwnerCharacter.transform.DOMove(dashPosition, dashDuration).SetEase(Ease.InOutSine).OnComplete(ExitSkill);
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsDash = false;
            OwnerCharacter.StartMovementController();
        }
        
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}