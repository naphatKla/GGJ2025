using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Handles 2D movement logic for physics-based entities using <see cref="Rigidbody2D"/>.
    /// Supports instant, inertia-based, and tweened movement, including curved paths and dynamic target following.
    /// Inherits from <see cref="BaseMovementSystem"/> and can be used for both player-controlled and AI-driven objects.
    /// </summary>
    public class RigidbodyMovementSystem : BaseMovementSystem
    {
        /// <summary>
        /// The Rigidbody2D component used for physics-based movement.
        /// Automatically assigned from the GameObject if not manually set via inspector.
        /// </summary>
        [Required] [SerializeField] private Rigidbody2D rb2D;

        /// <summary>
        /// Ensures that the Rigidbody2D component is assigned at runtime.
        /// If not already set via the inspector, it will attempt to retrieve it from the current GameObject.
        /// </summary>
        private void Awake()
        {
            if (rb2D) return;
            rb2D = GetComponent<Rigidbody2D>();
        }

        #region Methods
        
        /// <summary>
        /// Moves the entity smoothly toward the specified position using Rigidbody2D physics.
        /// Applies gradual acceleration and turning inertia based on configured movement rates.
        /// This method should be called in <c>FixedUpdate</c>.
        /// </summary>
        /// <param name="position">The world position the entity should move toward.</param>

        protected override void MoveWithInertia(Vector2 position)
        {
            Vector2 rawDirection = (position - (Vector2)transform.position).normalized;
            currentDirection = Vector2.Lerp(currentDirection, rawDirection, turnAccelerationRate * Time.fixedDeltaTime);
            
            Vector2 desiredVelocity = currentDirection * currentSpeed;
            currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, moveAccelerationRate * Time.fixedDeltaTime);
            rb2D.velocity = currentVelocity;
        }

        /// <summary>
        /// Instantly moves the entity's Rigidbody2D to the specified position using physics-based movement.
        /// </summary>
        /// <param name="position">The target position to move to.</param>
        protected override void MoveToPosition(Vector2 position)
        {
            rb2D.MovePosition(position);
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
            Vector2 startPos = rb2D.position;
            Vector2 direction = (position - startPos).normalized;
            Vector2 perpendicular = Vector2.Perpendicular(direction);

            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 basePos = Vector2.Lerp(startPos, position, linearT);

                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                rb2D.MovePosition(curvedPos);
            }, 1f, duration);

            return easeCurve != null? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }


        /// <summary>
        /// Smoothly moves the entity toward a moving target Transform over a duration using Rigidbody2D and DOTween.
        /// The movement speed is controlled by an optional easing AnimationCurve, and the path can be offset using a lateral curve.
        /// This allows for fully customizable motion in both speed and shape, such as arcing or wave-like trajectories.
        /// </summary>
        /// <param name="target">The Transform to follow during the tween.</param>
        /// <param name="duration">Total time (in seconds) to reach the target.</param>
        /// <param name="easeCurve">Optional AnimationCurve to control speed over time (replaces Ease). Defaults to Ease.InOutSine if null.</param>
        /// <param name="moveCurve">Optional AnimationCurve to apply perpendicular displacement, such as arcs or waves.</param>
        /// <returns>The DOTween Tween handling the movement over time.</returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;

            var tween = DOTween.To(() => 0f, t =>
                {
                    float linearT = Mathf.Clamp01(t);
                    Vector2 currentTargetPos = target.position;
                    Vector2 basePos = Vector2.Lerp(startPos, currentTargetPos, linearT);

                    Vector2 direction = (currentTargetPos - startPos).normalized;
                    Vector2 perpendicular = Vector2.Perpendicular(direction);

                    float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                    Vector2 curvedPos = basePos + perpendicular * offset;

                    rb2D.MovePosition(curvedPos);
                },
                1f,
                duration
            );
            
            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }
        
        #endregion
    }
}