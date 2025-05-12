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
    public class EnemyMovementSystem : BaseMovementSystem
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
        /// Assigns base movement data to the character, including speed and acceleration settings.
        /// Typically called by the character controller during initialization to configure movement behavior.
        /// </summary>
        /// <param name="baseSpeed">The base movement speed of the character.</param>
        /// <param name="moveAccelerationRate">The rate at which the character accelerates toward its movement speed.</param>
        /// <param name="turnAccelerationRate">The rate at which the character changes its movement direction.</param>
        public override void AssignMovementData(float baseSpeed, float moveAccelerationRate, float turnAccelerationRate)
        {
            base.AssignMovementData(baseSpeed, moveAccelerationRate, turnAccelerationRate);
            agent.speed = baseSpeed;
            agent.acceleration = moveAccelerationRate;
            agent.angularSpeed = turnAccelerationRate * 180f;
        }
        
        /// <summary>
        /// Moves the enemy and path finding to the target's position with inertia, using naveMeshAgent
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        protected override void MoveWithInertia(Vector2 position)
        {
            agent.SetDestination(position);
        }

        /// <summary>
        /// Instantly warps the NavMeshAgent to the specified position, bypassing pathfinding and obstacle avoidance.
        /// </summary>
        /// <param name="position">The destination position to warp the agent to.</param>
        protected override void MoveToPosition(Vector2 position)
        {
            agent.Warp(position);
        }

        /// <summary>
        /// Smoothly moves the entity to a destination using DOTween over a specified duration.
        /// The movement speed is controlled by an optional easing AnimationCurve, and the path can be offset perpendicularly using a second curve.
        /// This allows for customizable speed and motion effects like arcs, slashes, or waves.
        /// If no easing curve is provided, Ease.InOutSine is used as the default.
        /// </summary>
        /// <param name="position">Target position to move toward.</param>
        /// <param name="duration">Time in seconds to reach the target position.</param>
        /// <param name="easeCurve">Optional AnimationCurve that defines how interpolation progresses over time. Defaults to Ease.InOutSine if null.</param>
        /// <param name="moveCurve">Optional AnimationCurve to apply perpendicular offset for curved or styled motion.</param>
        /// <returns>A Tween instance managing the interpolated motion over time.</returns>
        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            agent.ResetPath();

            Vector2 startPos = agent.transform.position;
            Vector2 direction = (position - startPos).normalized;
            Vector2 perpendicular = Vector2.Perpendicular(direction);

            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 basePos = Vector2.Lerp(startPos, position, linearT);

                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                Vector2 delta = curvedPos - (Vector2)agent.transform.position;
                agent.Move(delta);
            }, 1f, duration);
            
            return easeCurve != null? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// Smoothly moves the enemy's NavMeshAgent toward a dynamic target over time using DOTween.
        /// Continuously follows the target's real-time position while optionally applying custom easing and perpendicular motion.
        /// </summary>
        /// <param name="target">The target Transform to move toward during the tween.</param>
        /// <param name="duration">Duration (in seconds) of the movement.</param>
        /// <param name="easeCurve">Optional curve to control easing/speed progression.</param>
        /// <param name="moveCurve">Optional curve that offsets the path perpendicularly.</param>
        /// <returns>The Tween that handles interpolated movement.</returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            agent.ResetPath();

            Vector2 startPos = agent.transform.position;

            var tween = DOTween.To(() => 0f, t =>
                {
                    float linearT = Mathf.Clamp01(t);
                    Vector2 currentTargetPos = target.position;
                    Vector2 basePos = Vector2.Lerp(startPos, currentTargetPos, linearT);

                    Vector2 direction = (currentTargetPos - startPos).normalized;
                    Vector2 perpendicular = Vector2.Perpendicular(direction);

                    float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                    Vector2 curvedPos = basePos + perpendicular * offset;

                    Vector2 delta = curvedPos - (Vector2)agent.transform.position;
                    agent.Move(delta);
                },
                1f,
                duration
            );
            
            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }
        
        #endregion
    }
}
