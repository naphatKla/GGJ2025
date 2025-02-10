using System.Collections;
using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Skills
{
    public class SkillPiercerDash : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPiercerDash")]
        [SerializeField] private float chargeTime = 1.5f;
        [SerializeField] private float fleePlayertime = 0.5f;

        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private ParticleSystem skillParticle;
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
            skillParticle.transform.localPosition = Vector3.zero;
            skillParticle.Play();
            
            float timer = 0;
            while (timer < chargeTime)
            {
                skillParticle.transform.position = transform.position;
                skillParticle.transform.up = GetTargetDirection();
                timer += Time.deltaTime;
                yield return null;
            }
            
            Vector2 direction = GetTargetDirection();
            Vector2 targetPosition = PlayerCharacter.Instance.transform.position;

            timer = 0;
            while (timer < fleePlayertime)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            Vector2 dashPosition = targetPosition + (direction * dashDistance);
            dashPosition = OwnerCharacter.ClampMovePositionToBound(dashPosition);
            Collider2D[] enemiesInDashLine = CheckDashCollision(direction);
            Debug.Log(enemiesInDashLine.Length);
            OwnerCharacter.transform.DOMove(dashPosition, dashDuration).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                foreach (Collider2D enemy in enemiesInDashLine)
                {
                    if (enemy.TryGetComponent(out CharacterBase target))
                        target.TakeDamage(OwnerCharacter);
                }

                ExitSkill();
            });
        }
        
        private Collider2D[] CheckDashCollision(Vector2 direction)
        {
            Vector2 boxCenter = (Vector2)transform.position + direction * (dashDistance / 2);
            float dashHeight = dashDistance;
            float dashWidth = 2f;

            Quaternion boxRotation = Quaternion.FromToRotation(Vector2.right, direction);
            Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(boxCenter, new Vector2(dashHeight, dashWidth),
                boxRotation.eulerAngles.z, OwnerCharacter.EnemyLayerMask);
            return hitEnemies;
        }
        
        private void OnDrawGizmos()
        {
            return;
            if (!OwnerCharacter) return;
            Vector2 direction = GetTargetDirection();
            Vector2 boxCenter = (Vector2)transform.position + direction * (dashDistance / 2);
            float dashHeight = dashDistance;
            float dashWidth = 2f;

            Quaternion boxRotation = Quaternion.FromToRotation(Vector2.right, direction);
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, boxRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(dashHeight, dashWidth, 1f));
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}