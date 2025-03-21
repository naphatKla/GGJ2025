using DG.Tweening;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Base class for handling movement logic of an entity.
    /// </summary>
    public abstract class BaseMovementSystem : MonoBehaviour
    {
        #region Inspector & Variables

        /// <summary>
        /// Default movement speed of the entity.
        /// </summary>
        private float normalSpeed = 6f;

        /// <summary>
        /// The current movement speed of the entity.
        /// </summary>
        protected float currentSpeed = 6f;

        /// <summary>
        /// Determines whether the entity is allowed to move.
        /// </summary>
        private bool _canMove = true;

        #endregion

        #region Methods

        /// <summary>
        /// Attempts to move the entity if movement is allowed.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        public virtual void TryMove(Vector2 position)
        {
            if (!_canMove) return;
            Move(position);
        }

        /// <summary>
        /// Attempts to smoothly move the entity to a specified position over a set duration, if movement is allowed.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        /// <param name="duration">The time it takes to reach the target position.</param>
        /// <param name="ease">The easing function that determines the movement behavior.</param>
        public virtual Tween  TryMoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            if (!_canMove) return null;
            return MoveToPositionOverTime(position, duration, ease);
        }

        /// <summary>
        /// Resets the current speed to the default normal speed.
        /// </summary>
        protected void ResetSpeed()
        {
            currentSpeed = normalSpeed;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Moves continuously with the current speed to the new position.
        /// This method should be called in FixedUpdate.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        protected abstract void Move(Vector2 position);

        /// <summary>
        /// Moves toward the target position over a specified duration using easing.
        /// </summary>
        /// <param name="position">The target position to reach.</param>
        /// <param name="duration">The time taken to reach the target.</param>
        /// <param name="ease">The easing function applied to the movement.</param>
        protected abstract Tween  MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine);

        #endregion
    }
}
