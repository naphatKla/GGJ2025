using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Base class for handling movement logic of an entity.
    /// Includes support for inertia-based movement and tweened movement toward a specific target.
    /// Meant to be extended by character or enemy movement classes.
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
        /// Typically updated per frame and applied to Rigidbody2D or NavMeshAgent.
        /// </summary>
        protected Vector2 currentVelocity;

        /// <summary>
        /// The current normalized movement direction of the entity.
        /// Used to determine the facing or intended direction.
        /// </summary>
        protected Vector2 currentDirection;

        /// <summary>
        /// Determines whether the entity is allowed to move.
        /// If false, movement commands will be ignored.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _canMove = true;

        /// <summary>
        /// Reference to the active tween used for movement over time (via DOTween).
        /// If a new tween is triggered, this one will be killed to avoid overlap.
        /// </summary>
        private Tween _moveOverTimeTween;

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
        /// Attempts to move the raw position of the entity.
        /// </summary>
        /// <param name="position"></param>
        public virtual void TryMoveRawPosition(Vector2 position)
        {
            if (!_canMove) return;
            MoveToPosition(position);
        }

        /// <summary>
        /// Attempts to smoothly move the entity to a specified position over a set duration, if movement is allowed.
        /// Cancels any existing tween before starting a new one. Uses an optional easing AnimationCurve to control speed progression,
        /// and a separate motion curve to apply perpendicular motion for effects like arcs or slashes.
        /// </summary>
        /// <param name="position">The target position to move towards.</param>
        /// <param name="duration">The time it takes to reach the target position.</param>
        /// <param name="easeCurve">Optional AnimationCurve controlling easing over time. Defaults to Ease.InOutSine if null or invalid.</param>
        /// <param name="moveCurve">Optional AnimationCurve used to apply offset-based curved motion.</param>
        /// <returns>The DOTween tween used for this movement.</returns>
        public virtual Tween TryMoveToPositionOverTime(Vector2 position, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            if (!_canMove) return null;
            _moveOverTimeTween?.Kill();
            _moveOverTimeTween = MoveToPositionOverTime(position, duration, easeCurve, moveCurve);
            return _moveOverTimeTween;
        }

        /// <summary>
        /// Attempts to move the entity smoothly toward a dynamic target Transform over the specified duration.
        /// Cancels any existing movement tween before starting a new one.
        /// Uses an optional easing AnimationCurve to control speed, and a separate motion curve to apply perpendicular offset.
        /// </summary>
        /// <param name="target">The Transform to move toward (can be moving).</param>
        /// <param name="duration">Time (in seconds) it should take to reach the target.</param>
        /// <param name="easeCurve">Optional AnimationCurve to control easing/speed progression. If null, defaults to Ease.InOutSine.</param>
        /// <param name="moveCurve">Optional AnimationCurve used to apply perpendicular motion during travel.</param>
        /// <returns>The Tween handling the movement operation, or null if movement is disabled.</returns>
        public virtual Tween TryMoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            if (!_canMove) return null;

            _moveOverTimeTween?.Kill();
            _moveOverTimeTween = MoveToTargetOverTime(target, duration, easeCurve, moveCurve);

            return _moveOverTimeTween;
        }
        
        /// <summary>
        /// Sets whether the movement system is allowed to move.
        /// Passing false will stop movement immediately.
        /// </summary>
        /// <param name="canMove">Whether movement is enabled.</param>
        public virtual void SetCanMove(bool canMove)
        {
            _canMove = canMove;

            if (!_canMove)
                StopMovement();
        }

        /// <summary>
        /// Immediately stops the entity's movement and cancels any ongoing tween.
        /// Also resets speed to zero.
        /// </summary>
        public virtual void StopMovement()
        {
            _moveOverTimeTween?.Kill();
            currentSpeed = 0;
        }

        /// <summary>
        /// Resets the current speed to the default base speed.
        /// Useful after temporary speed modifications.
        /// </summary>
        public void ResetSpeedToDefault()
        {
            currentSpeed = _baseSpeed;
        }

        /// <summary>
        /// Resets the movement system to its default state, as it was at start.
        /// Commonly used during events like respawn or revive.
        /// </summary>
        public void ResetMovementSystem()
        {
            _moveOverTimeTween?.Kill();
            ResetSpeedToDefault();
            SetCanMove(true);
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Moves continuously toward the specified position with acceleration and inertia.
        /// Called in FixedUpdate for consistent physics-based updates.
        /// </summary>
        /// <param name="position">The target position to move toward.</param>
        protected abstract void MoveWithInertia(Vector2 position);

        /// <summary>
        /// Instantly moves the entity to the specified world-space position.
        /// This method should be implemented differently depending on the movement system (e.g., Rigidbody2D, NavMeshAgent).
        /// </summary>
        /// <param name="position">The target world-space position to move to.</param>
        protected abstract void MoveToPosition(Vector2 position);
        
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
        protected abstract Tween MoveToPositionOverTime(Vector2 position, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null);

        /// <summary>
        /// Smoothly moves the entity toward a dynamic Transform target over a given duration using DOTween.
        /// Supports custom AnimationCurve for both speed (ease) and lateral movement.
        /// </summary>
        /// <param name="target">The Transform that the entity should follow.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="easeCurve">Optional curve to control easing/speed progression.</param>
        /// <param name="moveCurve">Optional curve to control lateral offset.</param>
        /// <returns>A DOTween Tween that moves the entity over time.</returns>
        protected abstract Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null);

        #endregion
    }
}
