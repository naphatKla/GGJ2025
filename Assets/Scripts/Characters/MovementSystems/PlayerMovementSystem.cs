using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Handles player movement using Rigidbody2D.
    /// Inherits from `BaseMovementSystem` and provides movement logic for player-controlled entities.
    /// </summary>
    public class PlayerMovementSystem : BaseMovementSystem
    {
        /// <summary>
        /// The Rigidbody2D component used for physics-based movement.
        /// </summary>
        [Required] [SerializeField] private Rigidbody2D rb2D;

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

        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.Linear, AnimationCurve moveCurve = null)
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

        #endregion
    }
}