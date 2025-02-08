using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillDanceDash : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPiercerDash")] [SerializeField]
        private int dashFrequency = 3;

        [SerializeField] private float dashAngle = 30f;
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashSpeed = 0.15f;

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
            Vector2 direction = GetDashDirection();

            float zigzagAngle = dashAngle;

            for (int i = 0; i < dashFrequency; i++)
            {
                float angle = (i % 2 == 0) ? zigzagAngle : -zigzagAngle;
                Vector2 rotatedDirection = Quaternion.Euler(0, 0, angle) * direction;
                Vector2 dashPosition = (Vector2)OwnerCharacter.transform.position +
                                       (rotatedDirection * (dashDistance / dashFrequency));
                yield return OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine)
                    .WaitForCompletion();
            }

            Vector2 finalDash = (Vector2)OwnerCharacter.transform.position + (direction * (dashDistance * 0.2f));
            yield return OwnerCharacter.transform.DOMove(finalDash, dashSpeed).SetEase(Ease.InOutSine)
                .WaitForCompletion();
            ExitSkill();
        }

        private Vector2 GetDashDirection()
        {
            if (IsPlayer)
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 direction = (mousePos - (Vector2)OwnerCharacter.transform.position).normalized;
                return direction;
            }

            OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
            return agent.velocity;
        }

        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}