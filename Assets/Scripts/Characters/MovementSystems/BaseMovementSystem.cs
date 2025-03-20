using DG.Tweening;
using UnityEngine;

namespace Characters.MovementSystems
{
    public abstract class BaseMovementSystem : MonoBehaviour
    {
        #region Variables
        private float normalSpeed = 6f;
        protected float currentSpeed = 6f;
        private bool _canMove = true;
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to move if movement is allowed.
        /// </summary>
        /// <param name="position">Target position.</param>
        public virtual void TryMove(Vector2 position)
        {
            if (!_canMove) return;
            Move(position);
        }

        /// <summary>
        /// Attempts to smoothly move the entity to the specified position over a set duration, if movement is allowed.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        /// <param name="duration">The time it takes to reach the target position.</param>
        /// <param name="ease">The easing function that determines the movement behavior.</param>
        public virtual void TryMoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            if (!_canMove) return;
            MoveToPositionOverTime(position, duration, ease);
        }

        /// <summary>
        /// Resets the current speed to the normal speed.
        /// </summary>
        protected void ResetSpeed()
        {
            currentSpeed = normalSpeed;
        }
        
        /// <summary>
        /// Moves continuously with the current speed to the new position. 
        /// This method should be called in FixedUpdate.
        /// </summary>
        /// <param name="position">Target position.</param>
        protected abstract void Move(Vector2 position);

        /// <summary>
        /// Moves toward the destination over a specified duration using easing.
        /// </summary>
        /// <param name="position">Target position.</param>
        /// <param name="duration">Time taken to reach the target.</param>
        /// <param name="ease">Easing function for smooth movement.</param>
        protected abstract void MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine);
        #endregion
    }
}