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

        #region Methods

        /// <summary>
        /// Moves the enemy directly toward the specified position.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        protected override void Move(Vector2 position)
        {
            agent.SetDestination(position);
        }

        /// <summary>
        /// Smoothly moves the enemy toward a specified position over a set duration using DOTween interpolation.
        /// This method uses NavMeshAgent.Move with delta position calculated per frame.
        /// 
        /// ⚠️ Note: For 2D games, make sure the NavMeshAgent's Base Offset is set to 0 
        /// to prevent unwanted vertical displacement.
        /// </summary>
        /// <param name="position">The world position the agent should reach.</param>
        /// <param name="duration">The time in seconds it should take to reach the destination.</param>
        /// <param name="ease">The easing curve used to interpolate the movement.</param>
        /// <returns>A Tween representing the interpolated movement operation.</returns>

        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            agent.ResetPath();
            
            return DOVirtual.Vector2(transform.position, position, duration, (pos) =>
            {
                Vector2 deltaPos = pos - (Vector2)agent.transform.position;
                agent.Move(deltaPos);
            }).SetEase(ease);
        }
        #endregion
    }
}
