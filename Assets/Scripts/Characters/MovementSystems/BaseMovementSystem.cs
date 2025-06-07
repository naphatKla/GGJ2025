using System;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
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
        /// Movement speed scaling factor applied when bouncing after collision.
        /// Affects how far the bounce reflects based on the current velocity.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float bounceMultiplier;

        /// <summary>
        /// mass of this entity, use to calculate bounce force and collision force.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        protected float mass;

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
        /// The duration of the bounce movement tween when a collision occurs.
        /// Determines how long the character takes to complete the bounce arc.
        /// </summary>
        private float _bounceDuration = 0.45f;

        /// <summary>
        /// The cooldown time after a bounce before another bounce can be triggered.
        /// Prevents repeated bounce execution from rapid collisions.
        /// </summary>
        private float _bounceCoolDown = 0.05f;

        /// <summary>
        /// Tracks whether the bounce is currently on cooldown.
        /// Used to prevent multiple bounce responses in quick succession.
        /// </summary>
        private bool _isBounceCooldown;

        /// <summary>
        /// Effects that are applied to the character when a bounce occurs
        /// </summary>
        private List<StatusEffectDataPayload> _effectOnBounce;

        /// <summary>
        /// Determine this entity is unstoppable or not.
        /// </summary>
        private bool _isUnStoppable;

        /// <summary>
        /// The current velocity of the entity movement.
        /// Typically updated per frame and applied to Rigidbody2D or NavMeshAgent.
        /// </summary>
        public Vector2 CurrentVelocity => currentVelocity;

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
        public virtual void AssignMovementData(float baseSpeed, float moveAccelerationRate, float turnAccelerationRate, float bounceMultiplier, float mass, List<StatusEffectDataPayload> effectOnBounce)
        {
            _baseSpeed = baseSpeed;
            currentSpeed = baseSpeed;
            this.moveAccelerationRate = moveAccelerationRate;
            this.turnAccelerationRate = turnAccelerationRate;
            this.bounceMultiplier = bounceMultiplier;
            this.mass = mass;
            _effectOnBounce = effectOnBounce;
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
                direction = Vector2.zero;
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
        /// Handles bounce behavior when a collision is detected during tween movement.
        /// Supports optional external force (e.g., from enemy impact) and adjusts bounce speed based on mass.
        /// Calculates combined momentum from self and external source to simulate head-on collisions.
        /// </summary>
        /// <param name="hitNormal">The normal vector from the collision surface.</param>
        /// <param name="externalForce">Optional force applied externally (e.g., enemy dash impact).</param>
        protected void ApplyBounceFromTweenCollision(Vector2 hitNormal, Vector2? externalForce = null)
        {
            float multiplier = bounceMultiplier;
            if (currentVelocity.magnitude <= currentSpeed)
            {
                // mini bounce != main bounce
                multiplier *= 0.5f;
            }
            else
            {
                //  main bounce
                StatusEffectManager.ApplyEffectTo(gameObject, _effectOnBounce);
            }
            
            _moveOverTimeTween?.Kill();
            
            Vector2 incomingForce = externalForce ?? Vector2.zero;
            Vector2 effectiveVelocity = currentVelocity - incomingForce;
            
            Vector2 reflectedDir = Vector2.Reflect(effectiveVelocity.normalized, hitNormal);
            float bounceSpeed = (effectiveVelocity.magnitude * multiplier) / Mathf.Max(0.1f, mass);
            Vector2 bounceTarget = (Vector2)transform.position + reflectedDir * bounceSpeed * 0.1f;
            
            _moveOverTimeTween = TryMoveToPositionOverTime(
                bounceTarget,
                _bounceDuration
            ).SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// Triggers bounce behavior if not in cooldown. Checks for valid target and applies bounce based on collision impact and enemy velocity.
        /// </summary>
        /// <param name="other">Collision data from Unity's collision callback.</param>
        public void BounceHandler(Collision2D other)
        {
            if (_isUnStoppable) return;
            if (_isBounceCooldown) return;
            if (other.contactCount == 0) return;
            
            Vector2 normal = other.GetContact(0).normal;
            Vector2? externalForce = null;

            if (other.gameObject.TryGetComponent(out BaseController otherController))
            {
                if (otherController.HealthSystem && otherController.HealthSystem.IsDead) return;
                externalForce = otherController.MovementSystem.currentVelocity;
            }
            
            ApplyBounceFromTweenCollision(normal, externalForce);
            StartBounceCooldown().Forget();
        }
        
        /// <summary>
        /// Begins a cooldown after a bounce to prevent repeated bounces in rapid succession.
        /// </summary>
        private async UniTask StartBounceCooldown()
        {
            _isBounceCooldown = true;
            await UniTask.WaitForSeconds(_bounceCoolDown);
            _isBounceCooldown = false;
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
        /// Sets whether the entity unstoppable or not
        /// Passing true will ignore status effect and bounce;
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetUnStoppableMode(bool value)
        {
            _isUnStoppable = value;
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