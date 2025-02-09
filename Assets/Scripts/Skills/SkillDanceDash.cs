using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Skills
{
    public class SkillDanceDash : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPiercerDash")] [SerializeField]
        private int dashFrequency = 3;

        [SerializeField] private float dashAngle = 30f;
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashDuration = 0.15f;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        protected override void OnSkillStart()
        {
            StartCoroutine(DashSequence());
        }
        
        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsDash = false;
            OwnerCharacter.StartMovementController();
        }
        
        private IEnumerator DashSequence()
        {
            OwnerCharacter.IsDash = true;
            OwnerCharacter.StopMovementController();
            Vector2 direction = GetTargetDirection();
            
            float zigzagAngle = dashAngle;
            
            for (int i = 0; i < dashFrequency; i++)
            {
                float angle = (i % 2 == 0) ? zigzagAngle : -zigzagAngle;
                Vector2 rotatedDirection = Quaternion.Euler(0, 0, angle) * direction;
                Vector2 dashPosition = (Vector2)OwnerCharacter.transform.position +
                                       (rotatedDirection * (dashDistance / dashFrequency));
                dashPosition = OwnerCharacter.ClampMovePositionToBound(dashPosition);
                yield return OwnerCharacter.transform.DOMove(dashPosition, dashDuration).SetEase(Ease.InOutSine)
                    .WaitForCompletion();
            }

            Vector2 finalDash = (Vector2)OwnerCharacter.transform.position + (direction * (dashDistance * 0.2f));
            finalDash = OwnerCharacter.ClampMovePositionToBound(finalDash);
            yield return OwnerCharacter.transform.DOMove(finalDash, dashDuration).SetEase(Ease.InOutSine)
                .WaitForCompletion();
            ExitSkill();
        }
        
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}