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
        /// Supports custom curved motion by applying lateral offset based on an AnimationCurve.
        /// This can simulate arcing, waving, or slashing-style paths for richer skill effects.
        /// </summary>
        /// <param name="position">Target position to move toward.</param>
        /// <param name="duration">Time in seconds to reach the position.</param>
        /// <param name="ease">Easing applied to the tween’s time progression (e.g. Ease.InOutSine).</param>
        /// <param name="moveCurve">Optional AnimationCurve to offset movement path perpendicularly.</param>
        /// <returns>A Tween instance managing interpolated motion.</returns>

        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.Linear, AnimationCurve moveCurve = null)
        {
            agent.ResetPath();

            Vector2 startPos = agent.transform.position;
            Vector2 direction = (position - startPos).normalized;
            Vector2 perpendicular = Vector2.Perpendicular(direction);

            return DOTween.To(() => 0f, t =>
                {
                    float linearT = Mathf.Clamp01(t);
                    Vector2 basePos = Vector2.Lerp(startPos, position, linearT);

                    float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                    Vector2 curvedPos = basePos + perpendicular * offset;

                    Vector2 delta = curvedPos - (Vector2)agent.transform.position;
                    agent.Move(delta);
                },
                1f,
                duration
            ).SetEase(ease);
        }

        /// <summary>
        /// Smoothly moves the enemy's NavMeshAgent toward a dynamic target over time using delta position and DOTween.
        /// Continuously follows the target's real-time position while optionally applying a perpendicular motion curve.
        /// </summary>
        /// <param name="target">The target Transform to move toward during the tween.</param>
        /// <param name="duration">Duration (in seconds) of the movement.</param>
        /// <param name="ease">Easing function applied to time progression.</param>
        /// <param name="moveCurve">Optional curve that offsets the path perpendicularly.</param>
        /// <returns>The Tween that handles interpolated movement.</returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, Ease ease = Ease.Linear, AnimationCurve moveCurve = null)
        {
            agent.ResetPath();

            Vector2 startPos = agent.transform.position;

            return DOTween.To(() => 0f, t =>
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
            ).SetEase(Ease.Linear); // ใช้ Linear เพื่อให้ AnimationCurve ทำงานตรงตามเส้นโค้ง
        }

        #endregion
    }
}
