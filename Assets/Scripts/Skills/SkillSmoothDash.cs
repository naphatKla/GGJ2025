using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillSmoothDash : SkillBase
    {
        #region Inspectors & Fields
        [Title("SkillDash")] 
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashSpeed = 0.15f;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void OnSkillStart()
        {
            StartCoroutine(SmoothDash());
        }

        private IEnumerator SmoothDash()
        {
            Vector2 startPosition = OwnerCharacter.transform.position;
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
            
            float dashSpeed = 0.3f;
            dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * dashDistance);
            
            yield return OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine).WaitForCompletion();
            yield return OwnerCharacter.transform.DOMove(startPosition, dashSpeed).SetEase(Ease.InOutSine).WaitForCompletion();
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
