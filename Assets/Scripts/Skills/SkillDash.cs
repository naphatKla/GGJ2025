using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

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
            Vector2 direction = GetDashDirection();
            Vector2 dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * dashDistance);
            OwnerCharacter.transform.DOMove(dashPosition, dashDuration).SetEase(Ease.InOutSine).OnComplete(ExitSkill);
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsDash = false;
            OwnerCharacter.StartMovementController();
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