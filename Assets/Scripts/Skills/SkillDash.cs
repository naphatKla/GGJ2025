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
            onSKillStart.AddListener(() => OwnerCharacter.IsModifyingMovement = true);
            onSkillEnd.AddListener(() => OwnerCharacter.IsModifyingMovement = false);
        }

        protected override void SkillAction()
        {
            Vector2 direction = OwnerCharacter.rigidbody2D.velocity.normalized;
            OwnerCharacter.rigidbody2D.velocity = Vector2.zero;
            OwnerCharacter.rigidbody2D.AddForce(-direction * backStepForce);
            DOVirtual.DelayedCall(backStepDuration, () => OwnerCharacter.rigidbody2D.AddForce(direction * dashForce));
        }
    }
}
