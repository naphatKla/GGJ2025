using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillDash : SkillBase
    {
        [Title("SkillDash")] [SerializeField] private float dashDistance = 8f;
        //[SerializeField] private float cameraPanOutMultiplier = 1.5f;
        //private float startOrthographicSize;
        
        private void Start()
        {
            onSkillStart.AddListener(() =>
            {
                OwnerCharacter.IsModifyingMovement = true;
                OwnerCharacter.isDash = true;
            });
            onSkillEnd.AddListener(() =>
            {
                OwnerCharacter.IsModifyingMovement = false;
                OwnerCharacter.isDash = false;
                if (!IsPlayer) OwnerCharacter.GetComponent<NavMeshAgent>().enabled = true;
            });
        }
        
        protected override void SkillAction()
        {
            float distance = 0f;
            Vector2 dashPosition = new Vector2();
            Vector2 direction = new Vector2();
            float lostOxygenValue = OwnerCharacter.BubbleSize * (oxygenCost / 100);
            
            if (IsPlayer)
            {
                dashPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                direction = (dashPosition - (Vector2)OwnerCharacter.transform.position).normalized;
                distance = Vector2.Distance(OwnerCharacter.transform.position, dashPosition);
            }
            else
            {
                NavMeshAgent agent = OwnerCharacter.GetComponent<NavMeshAgent>();
                direction = agent.velocity.normalized;
                agent.enabled = false;
            }
            
            if (distance > dashDistance || !IsPlayer)
                dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * dashDistance);
            
            OwnerCharacter.rigidbody2D.velocity = Vector2.zero;
            OwnerCharacter.transform.DOMove(dashPosition, skillDuration).SetEase(Ease.InOutSine);
        }
    }
}
