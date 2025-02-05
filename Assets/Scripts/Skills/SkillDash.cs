using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillDash : SkillBase
    {
        #region Inspectors & Fields
        [Title("SkillDash")] 
        [SerializeField] private float dashDistance = 8f;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void OnSkillStart()
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
            
            dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * dashDistance);
            OwnerCharacter.TryGetComponent(out Rigidbody2D rigid2D);
            rigid2D.velocity = Vector2.zero;
            OwnerCharacter.transform.DOMove(dashPosition, skillDuration).SetEase(Ease.InOutSine);
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
