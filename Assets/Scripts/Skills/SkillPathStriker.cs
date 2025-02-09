using System.Collections;
using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillPathStriker : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPathStriker")] [SerializeField]
        protected float chargeDuration = 3f;
        [SerializeField] protected float dashDuration = 0.3f;
        [SerializeField] protected bool iframeOnCharging = true;
        [SerializeField] private ParticleSystem skillParticle;
        private GameObject particleGroup;
        
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        public override void InitializeSkill(CharacterBase ownerCharacter)
        {
            base.InitializeSkill(ownerCharacter);
            particleGroup = new GameObject("ParticleGroup");
        }
        
        protected override void OnSkillStart()
        {
            OwnerCharacter.StopMovementController();
            StartCoroutine(ChargeSkill());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.StartMovementController();
            OwnerCharacter.IsDash = false;
            OwnerCharacter.IsIframe = false;
        }

        private IEnumerator ChargeSkill()
        {
            OwnerCharacter.IsIframe = iframeOnCharging;
            skillParticle.transform.SetParent(particleGroup.transform);
            skillParticle.transform.localPosition = Vector3.zero;
            skillParticle.Play();

            float timer = 0;
            while (timer < chargeDuration)
            {
                particleGroup.transform.position = transform.position;
                particleGroup.transform.up = GetTargetDirection();
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!OwnerCharacter) yield break;
            OwnerCharacter.IsDash = true;
            Vector2 direction = GetTargetDirection();
            RaycastHit2D boundHit = Physics2D
                .Raycast(OwnerCharacter.transform.position, direction, 100f, LayerMask.GetMask("LevelBound"));
            float distance = Vector2.Distance(boundHit.point, OwnerCharacter.transform.position);
            Vector2 dashPosition = OwnerCharacter.transform.position + (Vector3)direction * distance;
            OwnerCharacter.transform.DOMove(dashPosition, dashDuration).SetEase(Ease.InOutSine).OnComplete(ExitSkill);
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
