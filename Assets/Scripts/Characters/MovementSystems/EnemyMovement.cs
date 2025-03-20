using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.MovementSystems
{
    public class EnemyMovement : BaseMovementSystem
    {
        [Required] [SerializeField] private NavMeshAgent agent;

        private void Start()
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        protected override void Move(Vector2 position)
        {
            agent.SetDestination(position);
        }

        protected override void MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            float distance = Vector3.Distance(agent.transform.position, position);
            float speed = distance / duration; 

            agent.speed = speed; 
            agent.SetDestination(position);
            DOVirtual.DelayedCall(duration, ResetSpeed);
        }
    }
}

