using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skills
{
    public class SkillDash : SkillBase
    {
        [Title("SkillDash")] [SerializeField] private float backStepForce = 3f;
        [SerializeField] [ValidateInput("@backStepDuration <= skillDuration")] private float backStepDuration = 0.25f;
        [SerializeField] private float minDashForce = 5f;
        [SerializeField] private float maxDashForce = 10f;
        
        private void Start()
        {
            onSkillStart.AddListener(() => OwnerCharacter.IsModifyingMovement = true);
            onSkillEnd.AddListener(() =>
            {
                OwnerCharacter.IsModifyingMovement = false;
                OwnerCharacter.UpdateScale();
            });
        }

        protected override void SkillAction()
        {
            float dashForce = maxDashForce * chargePercentage;
            Vector2 direction = new Vector2();
            if (OwnerCharacter.CompareTag("Player")) 
                direction = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - OwnerCharacter.transform.position).normalized;
            else 
                direction = OwnerCharacter.rigidbody2D.velocity.normalized;
            
            OwnerCharacter.rigidbody2D.velocity = Vector2.zero;
            OwnerCharacter.rigidbody2D.AddForce(-direction * backStepForce);
            DOVirtual.DelayedCall(backStepDuration, () =>
            {
                OwnerCharacter.rigidbody2D.AddForce(direction * (maxDashForce));
            });
        }
    }
}
