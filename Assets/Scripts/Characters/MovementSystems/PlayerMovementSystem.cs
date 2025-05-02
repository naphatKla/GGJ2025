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
        /// Smoothly moves the player to the specified position over a set duration using DOTween.
        /// </summary>
        /// <param name="position">The target position to reach.</param>
        /// <param name="duration">The time it takes to reach the target position.</param>
        /// <param name="ease">The easing function applied to the movement.</param>
        protected override Tween MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            return rb2D.DOMove(position, duration).SetEase(ease);
        }

        #endregion
    }
}