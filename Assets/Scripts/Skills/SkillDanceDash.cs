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
            StartCoroutine(DanceDash());
        }

        private IEnumerator DanceDash()
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
            
            Vector2 targetPosition = target.transform.position;
            
            direction = (targetPosition - (Vector2)OwnerCharacter.transform.position).normalized;
    
            float zigzagAngle = 30f;
            float zigzagDistance = Vector2.Distance(targetPosition, OwnerCharacter.transform.position) / 3f; // แบ่งเป็น 3 Dash
    
            for (int i = 0; i < 3; i++)
            {
                float angle = (i % 2 == 0) ? zigzagAngle : -zigzagAngle;
                Vector2 rotatedDirection = Quaternion.Euler(0, 0, angle) * direction;
                dashPosition = (Vector2)OwnerCharacter.transform.position + rotatedDirection * zigzagDistance;
                yield return OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine).WaitForCompletion();
            }
            
            dashPosition = targetPosition + direction * (zigzagDistance * 0.5f);
            yield return OwnerCharacter.transform.DOMove(dashPosition, dashSpeed).SetEase(Ease.InOutSine).WaitForCompletion();
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
