using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillSmoothDash : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillDash")] [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashSpeed = 0.3f;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        protected override void OnSkillStart()
        {
            StartCoroutine(SmoothDash());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.StartMovementController();
            OwnerCharacter.IsDash = false;
        }

        private IEnumerator SmoothDash()
        {
            OwnerCharacter.IsDash = true;
            OwnerCharacter.StopMovementController();
            Vector2 direction = GetTargetDirection();
            Vector2 startPosition = OwnerCharacter.transform.position;
            Vector2 dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * dashDistance);
            dashPosition = OwnerCharacter.ClampMovePositionToBound(dashPosition);
            yield return OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine)
                .WaitForCompletion();
            yield return OwnerCharacter.transform.DOMove(startPosition, dashSpeed).SetEase(Ease.InOutSine)
                .WaitForCompletion();
            ExitSkill();
        }
        
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}