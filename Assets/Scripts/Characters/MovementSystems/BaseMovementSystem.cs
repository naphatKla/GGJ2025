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
        /// Controls how quickly the entity accelerates toward its maximum movement speed in a straight line.
        /// Higher values result in quicker speed buildup during forward motion.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float moveAccelerationRate;

        /// <summary>
        /// Controls how quickly the entity changes its movement direction when turning.
        /// Higher values result in more responsive and sharper directional adjustments.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float turnAccelerationRate;

        /// <summary>
        /// The current velocity of the entity movement.
        /// Typically updated per frame and applied to Rigidbody2D for physical motion.
        /// </summary>
        protected Vector2 currentVelocity;

        /// <summary>
        /// The current normalized movement direction of the entity.
        /// Used to determine the facing or desired movement direction over time.
        /// </summary>
        protected Vector2 currentDirection;

        /// <summary>
        /// Determines whether the entity is allowed to move.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _canMove = true;
        
        #endregion

        #region Methods

        /// <summary>
        /// Assigns base movement data to the character, including speed and acceleration settings.
        /// Typically called by the character controller during initialization to configure movement behavior.
        /// </summary>
        /// <param name="baseSpeed">The base movement speed of the character.</param>
        /// <param name="moveAccelerationRate">The rate at which the character accelerates toward its movement speed.</param>
        /// <param name="turnAccelerationRate">The rate at which the character changes its movement direction.</param>
        public virtual void AssignMovementData(float baseSpeed, float moveAccelerationRate, float turnAccelerationRate)
        {
            _baseSpeed = baseSpeed;
            currentSpeed = baseSpeed;
            this.moveAccelerationRate = moveAccelerationRate;
            this.turnAccelerationRate = turnAccelerationRate;
        }
        
        /// <summary>
        /// Attempts to apply inertia-based movement toward the given position if movement is currently allowed.
        /// Commonly called by input systems or AI controllers to trigger motion with acceleration and turning behavior.
        /// </summary>
        /// <param name="position">The target world-space position to move toward.</param>
        public virtual void TryMoveWithInertia(Vector2 position)
        {
            if (!_canMove) return;
            MoveWithInertia(position);
        }

        /// <summary>
        /// Attempts to smoothly move the entity to a specified position over a set duration, if movement is allowed.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        /// <param name="duration">The time it takes to reach the target position.</param>
        /// <param name="ease">The easing function that determines the movement behavior.</param>
        public virtual Tween TryMoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
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
        protected abstract void MoveWithInertia(Vector2 position);

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
