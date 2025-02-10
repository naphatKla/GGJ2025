using System.Collections;
using Characters;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillPathStriker : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPathStriker")] [SerializeField]
        protected float chargeDuration = 3f;

        [SerializeField] protected float dashDistance = 100f;
        [SerializeField] protected float dashDuration = 0.5f;
        [SerializeField] protected bool iframeOnCharging = true;
        [SerializeField] private ParticleSystem skillParticle;
        [SerializeField] private MMF_Player chargeReleaseFeedback;
        private GameObject _particleGroup;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        public override void InitializeSkill(CharacterBase ownerCharacter)
        {
            base.InitializeSkill(ownerCharacter);
            _particleGroup = new GameObject("ParticleGroup");
        }

        protected override void OnSkillStart()
        {
            OwnerCharacter.StopMovementController();
            StartCoroutine(ChargeSkill());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.StartMovementController();
            OwnerCharacter.IsIframe = false;
        }

        private IEnumerator ChargeSkill()
        {
            OwnerCharacter.IsIframe = iframeOnCharging;
            skillParticle.transform.SetParent(_particleGroup.transform);
            skillParticle.transform.localPosition = Vector3.zero;
            skillParticle.Play();

            float timer = 0;
            while (timer < chargeDuration)
            {
                _particleGroup.transform.position = transform.position;
                _particleGroup.transform.up = GetTargetDirection();
                timer += Time.deltaTime;
                yield return null;
            }

            if (!OwnerCharacter) yield break;
            Vector2 direction = GetTargetDirection();
            Vector2 dashPosition = OwnerCharacter.transform.position + (Vector3)direction * dashDistance;
            dashPosition = OwnerCharacter.ClampMovePositionToBound(dashPosition);
            Collider2D[] enemiesInDashLine = CheckDashCollision();
            chargeReleaseFeedback?.PlayFeedbacks();
            OwnerCharacter.transform.DOMove(dashPosition, dashDuration).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                foreach (Collider2D enemy in enemiesInDashLine)
                {
                    if (!enemy) continue;
                    if (!enemy.TryGetComponent(out CharacterBase target)) continue;
                    target.TakeDamage(OwnerCharacter);
                }

                ExitSkill();
            });
        }

        private Collider2D[] CheckDashCollision()
        {
            Vector2 direction = GetTargetDirection();
            Vector2 boxCenter = (Vector2)transform.position + direction * (dashDistance / 2);
            float dashHeight = dashDistance;
            float dashWidth = 4f;

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
            float dashWidth = 4f;

            Quaternion boxRotation = Quaternion.FromToRotation(Vector2.right, direction);
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, boxRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(dashHeight, dashWidth, 1f));
        }

        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}