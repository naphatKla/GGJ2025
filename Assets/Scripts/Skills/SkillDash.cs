using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillDash : SkillBase
    {
        [Title("SkillDash")] [SerializeField] private float backStepForce = 3f;
        [SerializeField] [ValidateInput("@backStepDuration <= skillDuration")] private float backStepDuration = 0.25f;
        [SerializeField] private float dashForce = 10f;
        
        private void Start()
        {
            onSkillStart.AddListener(() => OwnerCharacter.IsModifyingMovement = true);
            onSkillEnd.AddListener(() => OwnerCharacter.IsModifyingMovement = false);
        }

        protected override void SkillAction()
        {
            Vector2 direction = OwnerCharacter.rigidbody2D.velocity.normalized;
            OwnerCharacter.rigidbody2D.velocity = Vector2.zero;
            OwnerCharacter.rigidbody2D.AddForce(-direction * backStepForce);
            transform.DOScale(Vector2.one * 1.5f, backStepDuration).OnComplete(() =>
            {
                OwnerCharacter.rigidbody2D.AddForce(direction * dashForce);
                float leftDuration = skillDuration - backStepDuration;
                transform.DOScale(Vector2.one,leftDuration);
            });
        }
    }
}
