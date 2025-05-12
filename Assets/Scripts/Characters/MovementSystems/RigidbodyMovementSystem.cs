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
        /// Supports custom curved motion by applying lateral offset based on an AnimationCurve.
        /// This can simulate arcing, waving, or slashing-style paths for richer skill effects.
        /// </summary>
        /// <param name="position">Target position to move toward.</param>
        /// <param name="duration">Time in seconds to reach the position.</param>
        /// <param name="ease">Easing applied to the tween’s time progression (e.g. Ease.InOutSine).</param>
        /// <param name="moveCurve">Optional AnimationCurve to offset movement path perpendicularly.</param>
        /// <returns>A Tween instance managing interpolated motion.</returns>

        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;
            Vector2 direction = (position - startPos).normalized;
            Vector2 perpendicular = Vector2.Perpendicular(direction); // แนวตั้งฉากกับทิศทางหลัก

            return DOTween.To(() => 0f, t =>
                {
                    float linearT = Mathf.Clamp01(t);
                    Vector2 basePos = Vector2.Lerp(startPos, position, linearT);
                    
                    float offset = moveCurve?.Evaluate(linearT) ?? 0f;

                    Vector2 curvedPos = basePos + perpendicular * offset;
                    rb2D.MovePosition(curvedPos);
                },
                1f, duration).SetEase(ease);
        }

        /// <summary>
        /// Smoothly moves the entity toward a moving target Transform over a duration using Rigidbody2D and DOTween.
        /// Updates the target position every frame and supports optional curve-based perpendicular offset.
        /// </summary>
        /// <param name="target">The target Transform to follow during the tween.</param>
        /// <param name="duration">Total movement duration in seconds.</param>
        /// <param name="ease">Easing function applied to time progression.</param>
        /// <param name="moveCurve">Optional animation curve used to create arcing or wave-like paths.</param>
        /// <returns>The DOTween Tween handling the movement.</returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, Ease ease = Ease.InOutSine, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;

            return DOTween.To(() => 0f, t =>
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
            ).SetEase(ease);
        }

        #endregion
    }
}