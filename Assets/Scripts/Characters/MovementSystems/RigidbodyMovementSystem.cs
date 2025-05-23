using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Movement system implementation for Rigidbody2D-based characters.
    /// Inherits shared movement logic from BaseMovementSystem and provides physics-aware motion handling.
    /// - Handles continuous movement using inertia with acceleration and turning rates.
    /// - Supports tween-based movement (e.g., dash, leap) with optional directional curve effects.
    /// - Calculates velocity and direction dynamically to ensure smooth transitions and accurate bounce reactions.
    /// </summary>
    public class RigidbodyMovementSystem : BaseMovementSystem
    {
        [Required] [SerializeField] private Rigidbody2D rb2D;

        private void Awake()
        {
            if (rb2D) return;
            rb2D = GetComponent<Rigidbody2D>();
        }

        #region Methods
        
        /// <summary>
        /// Performs frame-based inertia movement using acceleration and turn smoothing.
        /// Updates current direction and velocity, then applies motion via Rigidbody2D.MovePosition.
        /// </summary>
        /// <param name="direction">The input movement direction vector.</param>
        protected override void MoveWithInertia(Vector2 direction)
        {
            currentDirection = Vector2.Lerp(currentDirection, direction, turnAccelerationRate * Time.deltaTime);
            Vector2 desiredVelocity = currentDirection * currentSpeed;
            currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, moveAccelerationRate * Time.deltaTime);
            Vector2 newPos = rb2D.position + currentVelocity * Time.deltaTime;
            TryMoveRawPosition(newPos);
        }

        /// <summary>
        /// Immediately moves the Rigidbody2D to the given position using MovePosition.
        /// Typically used for raw positional updates without tweening.
        /// </summary>
        /// <param name="position">The world-space position to move to.</param>
        protected override void MoveToPosition(Vector2 position)
        {
            rb2D.MovePosition(position);
        }

        /// <summary>
        /// Tween-moves the Rigidbody2D toward a fixed target position over time,
        /// optionally using directional curves for arc motion and easing for speed blending.
        /// Also updates currentVelocity and currentDirection for accurate bounce reflection.
        /// </summary>
        /// <param name="position">Destination position.</param>
        /// <param name="duration">Time to reach destination.</param>
        /// <param name="easeCurve">Optional curve to control tween easing.</param>
        /// <param name="moveCurve">Optional curve to offset motion perpendicular to the direction (for arc effect).</param>
        /// <returns>DOTween Tween object for control or cancellation.</returns>
        protected override Tween MoveToPositionOverTime(Vector2 position, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;
            Vector2 endPos = position;
            Vector2 perpendicular = Vector2.Perpendicular((endPos - startPos).normalized);
            Vector2 previousPosition = startPos;
            
            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 basePos = Vector2.Lerp(startPos, endPos, linearT);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;
                Vector2 frameDelta = curvedPos - previousPosition;
                float frameSpeed = frameDelta.magnitude / Time.deltaTime;
                
                currentDirection = frameDelta.normalized;
                currentVelocity = currentDirection * frameSpeed;
                
                previousPosition = curvedPos;
                TryMoveRawPosition(curvedPos);
            }, 1f, duration);

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// Tween-moves the Rigidbody2D toward a dynamic Transform target over time.
        /// Calculates position updates each frame and supports directional arc movement via moveCurve.
        /// </summary>
        /// <param name="target">Transform to follow.</param>
        /// <param name="duration">Time to reach the target.</param>
        /// <param name="easeCurve">Optional easing curve for movement speed control.</param>
        /// <param name="moveCurve">Optional perpendicular offset curve for arcing motion.</param>
        /// <returns>DOTween Tween instance managing the movement.</returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null,
            AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;
            Vector2 previousPosition = startPos;

            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 currentTargetPos = target.position;
                Vector2 basePos = Vector2.Lerp(startPos, currentTargetPos, linearT);
                Vector2 direction = (currentTargetPos - startPos).normalized;
                Vector2 perpendicular = Vector2.Perpendicular(direction);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;
                Vector2 frameDelta = curvedPos - previousPosition;
                float frameSpeed = frameDelta.magnitude / Time.deltaTime;
                
                currentDirection = frameDelta.normalized;
                currentVelocity = currentDirection * frameSpeed;
                
                previousPosition = curvedPos;
                TryMoveRawPosition(curvedPos);
            }, 1f, duration);

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }
        
        #endregion
    }
}