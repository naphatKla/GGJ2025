using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Handles player movement using Rigidbody2D.
    /// Inherits from `BaseMovementSystem` and provides movement logic for player-controlled entities.
    /// </summary>
    public class PlayerMovement : BaseMovementSystem
    {
        /// <summary>
        /// The Rigidbody2D component used for physics-based movement.
        /// </summary>
        [Required] [SerializeField] private Rigidbody2D rb2D;

        #region Protected Methods

        /// <summary>
        /// Moves the player continuously toward the specified position using Rigidbody2D.
        /// This method should be called in FixedUpdate.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        protected override void Move(Vector2 position)
        {
            Vector2 direction = position - (Vector2)transform.position;
            Vector2 newPos = (Vector2)transform.position + direction.normalized * (currentSpeed * Time.fixedDeltaTime);
            rb2D.MovePosition(newPos);
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