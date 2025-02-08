using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillPiercerDash : SkillBase
    {
        #region Inspectors & Fields
        [Title("SkillPiercerDash")] 
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashSpeed = 0.15f;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void OnSkillStart()
        {
            StartCoroutine(Targetlock());
        }

        private IEnumerator Targetlock()
        {
            Vector2 dashPosition;
            Vector2 direction;
            OwnerCharacter.IsModifyingMovement = true;
            OwnerCharacter.IsDash = true;

            var target = GameObject.FindGameObjectWithTag("Player");
            
            if (target == null) yield break;

            OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
            agent.velocity = Vector2.zero;
            agent.enabled = false;
            OwnerCharacter.TryGetComponent(out Rigidbody2D rigid2D);
            rigid2D.velocity = Vector2.zero;
            direction = agent.velocity.normalized;

            var waitTime = 1.5f;
            var timer = 0f;
            while (timer < waitTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            Vector2 targetPosition = target.transform.position;
            waitTime = 0.5f;
            timer = 0f;
            while (timer < waitTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            dashPosition = targetPosition + direction * dashDistance;
            OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine);
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
