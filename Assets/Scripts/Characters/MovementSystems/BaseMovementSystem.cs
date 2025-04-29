using DG.Tweening;
using Sirenix.OdinInspector;
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
        /// The default speed value used as the base for movement speed calculations.
        /// </summary>
        private float _baseSpeed;

        /// <summary>
        /// The current movement speed of the entity.
        /// Modify this value to increase or decrease speed of the entity.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float currentSpeed;

        /// <summary>
        /// Determines whether the entity is allowed to move.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _canMove = true;
        
        #endregion

        #region Methods

        /// <summary>
        /// Assigns the speed data for the character.
        /// This method is typically called by the character controller during initialization.
        /// </summary>
        /// <param name="baseSpeed">The default speed value to assign.</param>
        public virtual void AssignMovementData(float baseSpeed)
        {
            _baseSpeed = baseSpeed;
            currentSpeed = baseSpeed;
        }
        
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
        /// Sets whether the movement system is allowed to move.
        /// Passing false will completely lock movement.
        /// </summary>
        /// <param name="canMove">Whether movement is enabled.</param>
        public void SetCanMove(bool canMove)
        {
            _canMove = canMove;
        }
        
        /// <summary>
        /// Resets the current speed to the default base speed.
        /// </summary>
        protected void ResetSpeedToDefault()
        {
            currentSpeed = _baseSpeed;
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
