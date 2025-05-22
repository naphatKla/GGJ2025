
using System;
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
        /// 
        /// </summary>
        protected Vector2 inputDirection;
        
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

        #region Unity Methods

        /// <summary>
        /// 
        /// </summary>
        protected void Update()
        {
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
            MoveWithInertia(direction);
        }
        
        /// <summary>
        /// Attempts to move the raw position of the entity.
        /// </summary>
        public virtual void TryMoveRawPosition(Vector2 position)
        {
            if (!_canMove) return;
            MoveToPosition(position);
        }

        /// <summary>
        /// Attempts to smoothly move the entity to a specified position over a set duration, if movement is allowed.
        /// Cancels any existing tween before starting a new one.
        /// </summary>
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
        /// </summary>
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
        public virtual void SetCanMove(bool canMove)
        {
            _canMove = canMove;
            if (!_canMove) StopMovement();
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
        
        RaycastHit2D CheckMultiRaycast(Vector2 from, Vector2 to, int rayCount = 3)
        {
            Vector2 dir = (to - from).normalized;
            float dist = Vector2.Distance(from, to);

            // Try get collider size
            Vector2 perpOffset = Vector2.zero;
            if (TryGetComponent<Collider2D>(out var col))
            {
                // ใช้ bound จาก collider และยิง perpendicular จากทิศการเคลื่อนที่
                Vector2 perp = Vector2.Perpendicular(dir).normalized;
                float extent = col.bounds.extents.y; // หรือ x แล้วแต่แนว

                // ยิงหลายเส้นกระจายระยะ offset (เช่น -0.5, 0, +0.5)
                for (int i = 0; i < rayCount; i++)
                {
                    float lerp = rayCount == 1 ? 0 : (float)i / (rayCount - 1); // 0 to 1
                    float offsetAmount = Mathf.Lerp(-extent, extent, lerp);
                    Vector2 offset = perp * offsetAmount;
                    Vector2 origin = from + offset;

                    RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, LayerMask.GetMask("Enemy"));
                    Debug.DrawRay(origin, dir * dist, Color.red, 0.05f);

                    if (hit.collider) return hit;
                }
            }

            return default;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Moves continuously toward the specified position with acceleration and inertia.
        /// Called in FixedUpdate for consistent physics-based updates.
        /// </summary>
        protected abstract void MoveWithInertia(Vector2 direction);

        /// <summary>
        /// Instantly moves the entity to the specified world-space position.
        /// Implementation depends on movement system (e.g., Rigidbody, NavMesh).
        /// </summary>
        protected abstract void MoveToPosition(Vector2 position);

        /// <summary>
        /// Tween-moves the entity to a position with optional ease and curve.
        /// Used for dash, leaps, or stylish transitions.
        /// </summary>
        protected abstract Tween MoveToPositionOverTime(Vector2 position, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null);

        /// <summary>
        /// Tween-moves the entity to follow a dynamic Transform over time.
        /// </summary>
        protected abstract Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null, AnimationCurve moveCurve = null);

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
