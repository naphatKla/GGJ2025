using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Handles enemy movement using Unity's NavMeshAgent.
    /// Inherits from `BaseMovementSystem` and implements movement logic for AI-controlled entities.
    /// </summary>
    public class EnemyMovement : BaseMovementSystem
    {
        /// <summary>
        /// The NavMeshAgent component responsible for pathfinding and movement.
        /// </summary>
        [Required] [SerializeField] private NavMeshAgent agent;

        #region Unity Methods
        
        private void Start()
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        #endregion

        #region Movement Implementation

        /// <summary>
        /// Moves the enemy directly toward the specified position.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        protected override void Move(Vector2 position)
        {
            agent.SetDestination(position);
        }

        /// <summary>
        /// Moves the enemy toward the target position over a specified duration.
        /// Adjusts movement speed dynamically to ensure arrival within the given time.
        /// </summary>
        /// <param name="position">The target position to reach.</param>
        /// <param name="duration">The time it takes to reach the target position.</param>
        /// <param name="ease">The easing function applied to the movement.</param>
        protected override void MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            float distance = Vector3.Distance(agent.transform.position, position);
            float speed = distance / duration; // Adjusts speed dynamically

            agent.speed = speed;
            agent.SetDestination(position);

            // Resets speed after reaching destination
            DOVirtual.DelayedCall(duration, ResetSpeed);
        }

        #endregion
    }
}
