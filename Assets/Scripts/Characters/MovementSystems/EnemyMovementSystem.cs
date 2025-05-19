using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Handles enemy movement using Unity's NavMeshAgent.
    /// Supports inertia-based movement, DOTween transitions, and velocity blending after tween.
    /// Mirrors Rigidbody-based movement for consistent feel across AI and player.
    /// </summary>
    public class EnemyMovementSystem : BaseMovementSystem
    {
        /// <summary>
        /// The NavMeshAgent component used for AI pathfinding and manual movement.
        /// Must be assigned via inspector or initialized at runtime.
        /// </summary>
        [Required] [SerializeField] private NavMeshAgent agent;
        
        /// <summary>
        /// Initializes the NavMeshAgent by disabling automatic rotation and up-axis adjustment,
        /// allowing 2D-style movement in a top-down or side-scrolling setup.
        /// </summary>
        private void Start()
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        /// <summary>
        /// Moves the enemy toward a target position using smooth inertia logic.
        /// If recovering from a tween, velocity will blend from previous tween speed
        /// to natural direction-based speed. Uses NavMeshAgent.Move internally for manual motion.
        /// </summary>
        /// <param name="position">Target position to move toward.</param>
        protected override void MoveWithInertia(Vector2 position)
        {
            Vector2 desiredVelocity;
            
            if (isRecoveringFromTween)
            {
                blendBackTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(blendBackTimer / blendBackDuration);

                desiredVelocity = currentDirection * currentSpeed;
                currentVelocity = Vector2.Lerp(postTweenVelocity, desiredVelocity, t);
                agent.Move(currentVelocity * Time.fixedDeltaTime);
                isRecoveringFromTween = t < 1;
                return;
            }

            Vector2 rawDirection = (position - (Vector2)transform.position).normalized;
            currentDirection = Vector2.Lerp(currentDirection, rawDirection, turnAccelerationRate * Time.fixedDeltaTime);

            desiredVelocity = currentDirection * currentSpeed;
            currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, moveAccelerationRate * Time.fixedDeltaTime);
            agent.Move(currentVelocity * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Instantly teleports the enemy to the specified world-space position,
        /// bypassing NavMeshAgent pathfinding. Useful for warping or respawn.
        /// </summary>
        /// <param name="position">The world-space position to teleport to.</param>
        protected override void MoveToPosition(Vector2 position)
        {
            agent.Warp(position);
        }

        /// <summary>
        /// Smoothly moves the enemy to a static position over time using DOTween.
        /// During the tween, <see cref="currentDirection"/> and <see cref="currentVelocity"/> are updated.
        /// After tween completion, velocity is blended back into inertia-based movement.
        /// Movement path can include lateral curves for stylized effects.
        /// </summary>
        /// <param name="position">Final world-space destination.</param>
        /// <param name="duration">Total duration (in seconds) for movement.</param>
        /// <param name="easeCurve">Optional easing AnimationCurve. Defaults to Ease.InOutSine.</param>
        /// <param name="moveCurve">Optional perpendicular displacement curve.</param>
        /// <returns>The tween responsible for managing movement.</returns>
        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            agent.ResetPath();

            Vector2 startPos = agent.transform.position;
            Vector2 direction = (position - startPos).normalized;
            Vector2 perpendicular = Vector2.Perpendicular(direction);
            Vector2 previousPosition = startPos;

            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 basePos = Vector2.Lerp(startPos, position, linearT);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                Vector2 frameDirection = (curvedPos - previousPosition).normalized;
                if (frameDirection != Vector2.zero)
                    currentDirection = frameDirection;

                float frameDistance = Vector2.Distance(previousPosition, curvedPos);
                currentVelocity = frameDirection * (frameDistance / Time.deltaTime);

                Vector2 delta = curvedPos - (Vector2)agent.transform.position;
                agent.Move(delta);
                previousPosition = curvedPos;

            }, 1f, duration);

            tween.OnComplete(() =>
            {
                postTweenVelocity = currentVelocity;
                blendBackTimer = 0f;
                isRecoveringFromTween = true;
            });

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// Smoothly moves the enemy toward a moving <see cref="Transform"/> target over time.
        /// Follows the target in real time while applying optional curved pathing and easing.
        /// Velocity and direction are updated every frame. After completion, blends into inertia.
        /// </summary>
        /// <param name="target">The dynamic target to follow.</param>
        /// <param name="duration">Duration in seconds to follow target.</param>
        /// <param name="easeCurve">Optional easing AnimationCurve. Defaults to Ease.InOutSine.</param>
        /// <param name="moveCurve">Optional perpendicular curve (arc/slash).</param>
        /// <returns>The tween managing movement execution.</returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            agent.ResetPath();

            Vector2 startPos = agent.transform.position;
            Vector2 previousPosition = startPos;

            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 currentTargetPos = target.position;
                Vector2 basePos = Vector2.Lerp(startPos, currentTargetPos, linearT);

                Vector2 direction = (currentTargetPos - startPos).normalized;
                Vector2 perpendicular = Vector2.Perpendicular(direction);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                Vector2 frameDirection = (curvedPos - previousPosition).normalized;
                if (frameDirection != Vector2.zero)
                    currentDirection = frameDirection;

                float frameDistance = Vector2.Distance(previousPosition, curvedPos);
                currentVelocity = frameDirection * (frameDistance / Time.deltaTime);

                Vector2 delta = curvedPos - (Vector2)agent.transform.position;
                agent.Move(delta);
                previousPosition = curvedPos;

            }, 1f, duration);

            tween.OnComplete(() =>
            {
                postTweenVelocity = currentVelocity;
                blendBackTimer = 0f;
                isRecoveringFromTween = true;
            });

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }
    }
}
