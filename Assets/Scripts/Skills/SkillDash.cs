using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillDash : SkillBase
    {
        [Title("SkillDash")] [SerializeField] private float backStepForce = 3f;
        [SerializeField] [ValidateInput("@backStepDuration <= skillDuration")] private float backStepDuration = 0.25f;
        [SerializeField] private float maxDashForce = 10f;
        private Vector2 originalScale;
        
        private void Start()
        {
            onSkillStart.AddListener(() => OwnerCharacter.IsModifyingMovement = true);
            onSkillEnd.AddListener(() =>
            {
                OwnerCharacter.IsModifyingMovement = false;
                OwnerCharacter.transform.DOScale(Vector2.one, 0.1f).SetEase(Ease.OutBounce);
            });
            originalScale = OwnerCharacter.transform.localScale;
        }

        protected override void SkillAction()
        {
            Vector2 direction = OwnerCharacter.rigidbody2D.velocity.normalized;
            OwnerCharacter.rigidbody2D.velocity = Vector2.zero;
            OwnerCharacter.rigidbody2D.AddForce(-direction * backStepForce);
            DOVirtual.DelayedCall(backStepDuration, () =>
            {
                OwnerCharacter.rigidbody2D.AddForce(direction * (maxDashForce*chargeTime));
            });
        }
    }
}
