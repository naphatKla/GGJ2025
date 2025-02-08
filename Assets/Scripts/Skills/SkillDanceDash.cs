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
        [Title("SkillPiercerDash")] 
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashSpeed = 0.15f;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void OnSkillStart()
        {
            StartCoroutine(DashSequence());
        }

        private IEnumerator DashSequence()
        {
            Vector2 dashPosition;
            Vector2 direction;
            OwnerCharacter.IsModifyingMovement = true;
            OwnerCharacter.IsDash = true;
            
            if (IsPlayer)
            {
                dashPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                direction = (dashPosition - (Vector2)OwnerCharacter.transform.position).normalized;
            }
            else
            {
                OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
                direction = agent.velocity.normalized;
                agent.enabled = false;
            }

            float zigzagAngle = 30f;
            
            for (int i = 0; i < 3; i++)
            {
                float angle = (i % 2 == 0) ? zigzagAngle : -zigzagAngle;
                Vector2 rotatedDirection = Quaternion.Euler(0, 0, angle) * direction;
                dashPosition = (Vector2)OwnerCharacter.transform.position + (rotatedDirection * (dashDistance / 3f));

                yield return OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine).WaitForCompletion();
            }
            Vector2 finalDash = (Vector2)OwnerCharacter.transform.position + (direction * (dashDistance * 0.2f));
            yield return OwnerCharacter.transform.DOMove(finalDash, dashSpeed).SetEase(Ease.InOutSine).WaitForCompletion();
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsModifyingMovement = false;
            OwnerCharacter.IsDash = false;
            if (IsPlayer) return;
            OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
            agent.enabled = true;
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
