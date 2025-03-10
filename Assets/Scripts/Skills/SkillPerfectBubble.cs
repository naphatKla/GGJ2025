using System.Collections;
using System.Linq;
using Characters;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillPerfectBubble : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPerfectBubble")] [SerializeField]
        private float iframeDuration = 1f;

        [SerializeField] private float counterDashRange = 30f;
        [SerializeField] private float counterMinDashDistance = 8;
        [SerializeField] private int counterDashTime = 4;
        [SerializeField] private float counterDashDuration = 0.125f;
        [SerializeField] private float restTimePerDash = 0f;
        [SerializeField] private float iframeDurationAfterCounterDash = 0.5f;
        [SerializeField] private bool iframeOnCounterDash = true;

        [SerializeField] [PropertyOrder(99)]
        private MMF_Player counterStartFeedback;
        [SerializeField] private ParticleSystem iframeParticle;

        private bool _gotHit;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods

        private IEnumerator StartSkill()
        {
            // Wait for counter
            float timer = 0;
            while (timer <= iframeDuration)
            {
                if (!OwnerCharacter) yield break;
                if (_gotHit) break;
                OwnerCharacter.IsIframe = true;
                timer += Time.deltaTime;
                yield return null;
            }

            if (!_gotHit)
            {
                ExitSkill();
                yield break;
            }

            // Counter Dash
            _gotHit = false;
            OwnerCharacter.CanUseSkill = false;
            OwnerCharacter.StopMovementController();
            for (int i = 0; i < counterDashTime; i++)
            {
                Collider2D[] enemies = Physics2D.OverlapCircleAll(OwnerCharacter.transform.position, counterDashRange,
                    OwnerCharacter.EnemyLayerMask);
                
                if (enemies.Length <= 0)
                {
                    ExitSkill();
                    yield break;
                }
                
                Transform closestEnemy = enemies
                    .Where(e => e.TryGetComponent(out CharacterBase character) && !character.IsDead)  
                    .OrderBy(e => Vector2.Distance(OwnerCharacter.transform.position, e.transform.position))  
                    .FirstOrDefault()?.transform;
                
                if (!closestEnemy) continue;
                counterStartFeedback?.PlayFeedbacks();
                float distance = Vector2.Distance(OwnerCharacter.transform.position, closestEnemy.transform.position);
                Vector2 direction = (closestEnemy.position - OwnerCharacter.transform.position).normalized;
                Vector2 dashPosition = distance > counterMinDashDistance
                    ? closestEnemy.transform.position
                    : OwnerCharacter.transform.position + (Vector3)direction * counterMinDashDistance;
                dashPosition = OwnerCharacter.ClampMovePositionToBound(dashPosition);
                OwnerCharacter.IsDash = true;
                yield return OwnerCharacter.transform.DOMove(dashPosition, counterDashDuration)
                    .SetEase(Ease.InOutSine).OnUpdate(() =>
                    {
                        if (iframeOnCounterDash)
                            OwnerCharacter.IsIframe = true;
                    }) .WaitForCompletion();
                yield return new WaitForSeconds(restTimePerDash);
            }

            if (iframeOnCounterDash)
                yield return new WaitForSeconds(iframeDurationAfterCounterDash);
            ExitSkill();
        }

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        public override void InitializeSkill(CharacterBase ownerCharacter)
        {
            base.InitializeSkill(ownerCharacter);
            OwnerCharacter.onHit.AddListener(() => _gotHit = true);
        }

        protected override void OnSkillStart()
        {
            _gotHit = false;
            iframeParticle?.Play();
            StartCoroutine(StartSkill());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsDash = false;
            OwnerCharacter.IsIframe = false;
            OwnerCharacter.CanUseSkill = true;
            iframeParticle?.Stop();
            OwnerCharacter.StartMovementController();
        }

        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}