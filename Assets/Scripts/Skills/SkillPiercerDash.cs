using System.Collections;
using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillPiercerDash : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPiercerDash")] [SerializeField]
        private float chargeTime = 1.5f;

        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashSpeed = 0.15f;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        protected override void OnSkillStart()
        {
            StartCoroutine(TargetLock());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.StartMovementController();
            OwnerCharacter.IsDash = false;
        }

        private IEnumerator TargetLock()
        {
            OwnerCharacter.StopMovementController();
            OwnerCharacter.IsDash = true;
            Vector2 direction = GetDashDirection();

            float timer = 0f;
            while (timer < chargeTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            Vector2 targetPosition = IsPlayer ? direction * dashDistance : PlayerCharacter.Instance.transform.position;
            Vector2 dashPosition = targetPosition + (direction * dashDistance);
            OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine).OnComplete(ExitSkill);
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