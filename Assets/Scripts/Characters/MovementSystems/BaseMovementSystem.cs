using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Base class for handling movement logic of an entity.
    /// Supports both inertia-based directional movement and tween-based position transitions.
    /// Includes collision handling logic that applies bounce behavior when moving with tween,
    /// using the current velocity and surface normal to reflect direction.
    /// Intended to be extended by specific character or enemy movement implementations (e.g., Rigidbody or NavMesh-based).
    /// </summary>
    public abstract class BaseMovementSystem : MonoBehaviour
    {
        #region Inspector & Variables

        /// <summary>
        /// Enables or disables movement direction/velocity Gizmo rendering in the Scene view (for debugging).
        /// </summary>
        [SerializeField] [PropertyOrder(9999)] private bool enableGizmos;

        /// <summary>
        /// The default speed value used as the base for movement speed calculations.
        /// </summary>
        private float _baseSpeed;

        /// <summary>
        /// The current movement speed of the entity.
        /// Modify this value to increase or decrease speed of the entity.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float currentSpeed;

        /// <summary>
        /// Controls how quickly the entity accelerates toward its maximum movement speed in a straight line.
        /// Higher values result in quicker speed buildup during forward motion.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float moveAccelerationRate;

        /// <summary>
        /// Controls how quickly the entity changes its movement direction when turning.
        /// Higher values result in more responsive and sharper directional adjustments.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
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
        /// raw input direction from input reader.
        /// </summary>
        protected Vector2 inputDirection = Vector2.zero;

        /// <summary>
        /// Determines whether the entity is allowed to move.
        /// If false, movement commands will be ignored.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _canMove = true;

        /// <summary>
        /// Internal reference to the currently active DOTween tween used for time-based movement.
        /// This is killed and replaced when a new tween is triggered, ensuring smooth override behavior.
        /// </summary>
        private Tween _moveOverTimeTween;

        /// <summary>
        /// Controls whether the primary movement system (e.g., player input or AI) is active.
        /// If false, automatic or manual movement is disabled.
        /// </summary>
        private bool _enablePrimaryMovement = true;
        
        /// <summary>
        /// The current velocity of the entity movement.
        /// Typically updated per frame and applied to Rigidbody2D or NavMeshAgent.
        /// </summary>
        public Vector2 CurrentVelocity => currentVelocity;

        public Vector2 CurrentDirection => currentDirection;

        #endregion

        #region Unity Methods

        /// <summary>
        /// 
        /// </summary>
        protected void FixedUpdate()
        {
            if (inputDirection == Vector2.zero) return;
            TryMoveWithInertia(inputDirection);
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Assigns base movement data to the character, including speed and acceleration settings.
        /// Typically called by the character controller during initialization to configure movement behavior.
        /// </summary>
        public virtual void AssignMovementData(float baseSpeed, float moveAccelerationRate, float turnAccelerationRate)
        {
            _baseSpeed = baseSpeed;
            currentSpeed = baseSpeed;
            this.moveAccelerationRate = moveAccelerationRate;
            this.turnAccelerationRate = turnAccelerationRate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        public virtual void AssignInputDirection(Vector2 direction)
        {
            inputDirection = direction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        protected virtual void TryMoveWithInertia(Vector2 direction)
        {
            if (!_canMove) return;
            if (_moveOverTimeTween.IsActive()) return;
            if (currentSpeed == 0) return;
            if (!_enablePrimaryMovement)
            {
                MoveWithInertia(direction, Vector2.zero);
                return;
            }
            
            MoveWithInertia(direction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        public virtual void TryMoveRawPosition(Vector2 position)
        {
            if (!_canMove) return;
            MoveToPosition(position);
        }
        
        /// <summary>
        /// Attempts to smoothly move the entity to a specified position over a set duration, if movement is allowed.
        /// Cancels any existing tween before starting a new one.
        /// </summary>
        public virtual Tween TryMoveToPositionOverTime(Vector2 position, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            if (!_canMove) return null;
            _moveOverTimeTween?.Kill();
            _moveOverTimeTween = MoveToPositionOverTime(position, duration, easeCurve, moveCurve);
            return _moveOverTimeTween;
        }

        /// <summary>
        /// Attempts to move the entity smoothly toward a dynamic target Transform over the specified duration.
        /// Cancels any existing movement tween before starting a new one.
        /// </summary>
        public virtual Tween TryMoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null,
            AnimationCurve moveCurve = null)
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
        public virtual void SetCanMove(bool canMove)
        {
            _canMove = canMove;
            if (!_canMove) StopAllMovement();
        }
        
        /// <summary>
        /// Immediately stops the entity's movement and cancels any ongoing tween.
        /// Also resets speed to zero.
        /// </summary>
        public virtual void StopAllMovement()
        {
            _moveOverTimeTween?.Kill();
            currentSpeed = 0;
        }
        
        public virtual void TogglePrimaryMovement(bool enableMovement)
        {
            _enablePrimaryMovement = enableMovement;
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
        protected abstract void MoveWithInertia(Vector2 direction, Vector2? overrideVelocity = null);

        /// <summary>
        /// Instantly moves the entity to the specified world-space position.
        /// Implementation depends on movement system (e.g., Rigidbody, NavMesh).
        /// </summary>
        protected abstract void MoveToPosition(Vector2 position);

        /// <summary>
        /// Tween-moves the entity to a position with optional ease and curve.
        /// Used for dash, leaps, or stylish transitions.
        /// </summary>
        protected abstract Tween MoveToPositionOverTime(Vector2 position, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null);

        /// <summary>
        /// Tween-moves the entity to follow a dynamic Transform over time.
        /// </summary>
        protected abstract Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null,
            AnimationCurve moveCurve = null);

        #endregion

        /// <summary>
        /// Draws movement direction (green) and velocity vector (cyan) gizmos in the Scene view.
        /// Used during development to debug motion behavior.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enableGizmos) return;
            Vector3 origin = transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + (Vector3)(currentDirection.normalized));
            Gizmos.DrawSphere(origin + (Vector3)(currentDirection.normalized), 0.05f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + (Vector3)currentVelocity);
            Gizmos.DrawSphere(origin + (Vector3)currentVelocity, 0.05f);
        }
    }
}