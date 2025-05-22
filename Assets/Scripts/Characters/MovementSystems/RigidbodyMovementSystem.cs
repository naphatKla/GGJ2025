using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Handles 2D movement logic for physics-based entities using <see cref="Rigidbody2D"/>.
    /// Supports instant, inertia-based, and tweened movement, including curved paths and dynamic target following.
    /// Inherits from <see cref="BaseMovementSystem"/> and can be used for both player-controlled and AI-driven objects.
    /// </summary>
    public class RigidbodyMovementSystem : BaseMovementSystem
    {
        /// <summary>
        /// The Rigidbody2D component used for applying physics-based movement.
        /// Automatically assigned if not set via inspector.
        /// </summary>
        [Required] [SerializeField] private Rigidbody2D rb2D;

        /// <summary>
        /// Ensures Rigidbody2D is assigned. Automatically retrieves the component from the GameObject if needed.
        /// </summary>
        private void Awake()
        {
            if (rb2D) return;
            rb2D = GetComponent<Rigidbody2D>();
        }

        #region Methods

        /// <summary>
        /// Moves the entity smoothly toward the specified position using Rigidbody2D physics.
        /// Applies gradual acceleration and turning inertia based on configured movement rates.
        /// This method should be called in <c>FixedUpdate</c>.
        /// </summary>
        /// <param name="position">The world position the entity should move toward.</param>
        protected override void MoveWithInertia(Vector2 position)
        {
            Vector2 desiredVelocity;

            if (isRecoveringFromTween)
            {
                blendBackTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(blendBackTimer / blendBackDuration);

                desiredVelocity = currentDirection * currentSpeed;
                currentVelocity = Vector2.Lerp(postTweenVelocity, desiredVelocity, t);
                rb2D.velocity = currentVelocity;
                isRecoveringFromTween = t < 1;
                return;
            }

            Vector2 rawDirection = (position - (Vector2)transform.position).normalized;
            currentDirection = Vector2.Lerp(currentDirection, rawDirection, turnAccelerationRate * Time.fixedDeltaTime);

            desiredVelocity = currentDirection * currentSpeed;
            currentVelocity =
                Vector2.Lerp(currentVelocity, desiredVelocity, moveAccelerationRate * Time.fixedDeltaTime);
            rb2D.velocity = currentVelocity;
        }

        /// <summary>
        /// Instantly moves the entity's Rigidbody2D to the specified position using physics-based movement.
        /// </summary>
        /// <param name="position">The target position to move to.</param>
        protected override void MoveToPosition(Vector2 position)
        {
            rb2D.MovePosition(position);
        }

        /// <summary>
        /// Smoothly moves the entity to a destination using DOTween over a specified duration.
        /// While tweening, this method updates <see cref="currentDirection"/> and <see cref="currentVelocity"/>
        /// to reflect the actual motion per frame. After the tween completes, the system blends velocity
        /// back to its default inertia-based movement to avoid abrupt transitions.
        /// Supports curved motion and custom easing.
        /// </summary>
        /// <param name="position">The target world position the entity should move toward.</param>
        /// <param name="duration">Total time (in seconds) to reach the target position.</param>
        /// <param name="easeCurve">
        /// Optional <see cref="AnimationCurve"/> used to control the interpolation progression over time.
        /// If null, defaults to <see cref="Ease.InOutSine"/>.
        /// </param>
        /// <param name="moveCurve">
        /// Optional <see cref="AnimationCurve"/> used to apply perpendicular displacement during movement,
        /// allowing curved or stylized motion such as arcs or wave slashes. If null, moves in a straight line.
        /// </param>
        /// <returns>
        /// A <see cref="Tween"/> instance managing the interpolated movement, which can be chained or cancelled.
        /// </returns>
        protected override Tween MoveToPositionOverTime(Vector2 position, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;
            Vector2 endPos = position;
            Vector2 perpendicular = Vector2.Perpendicular((endPos - startPos).normalized);
            Vector2 previousPosition = startPos;
            float averageSpeed = Vector2.Distance(position, startPos) / duration;
            float elapsed = 0f;

            var tween = DOTween.To(() => 0f, t =>
            {
                elapsed += Time.deltaTime;
                float remainingTime = Mathf.Max(0f, duration - elapsed);
                float linearT = Mathf.Clamp01(t);

                Vector2 basePos = Vector2.Lerp(startPos, endPos, linearT);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                Vector2 movementDir = (curvedPos - previousPosition).normalized;
                if (movementDir != Vector2.zero)
                    currentDirection = movementDir;

                currentVelocity = currentDirection * averageSpeed;

                // ✅ ตรงนี้แก้: ray ยิงจากตำแหน่งปัจจุบัน → ไปยัง curvedPos
                Vector2 from = rb2D.position;
                Vector2 to = curvedPos;
                Vector2 rayDir = to - from;
                float rayDist = rayDir.magnitude;

                RaycastHit2D hit = Physics2D.Raycast(from, rayDir.normalized, rayDist, LayerMask.GetMask("Enemy"));
                if (hit.collider)
                {
                    // ✅ Reflect ทิศ, คูณความเร็วและเวลา
                    Vector2 reflected = Vector2.Reflect(rayDir.normalized, hit.normal);
                    endPos = from + reflected * averageSpeed * remainingTime;
                    startPos = from;
                    perpendicular = Vector2.Perpendicular(reflected);
                    previousPosition = from;
                    return; // ข้าม MovePosition เฟรมนี้
                }

                previousPosition = curvedPos;
                rb2D.MovePosition(curvedPos);
            }, 1f, duration);

            tween.OnComplete(() =>
            {
                postTweenVelocity = currentVelocity;
                blendBackTimer = 0f;
                isRecoveringFromTween = true;
            });

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }


        /// <summary>
        /// Smoothly moves the entity toward a dynamic target using DOTween over a specified duration.
        /// This method recalculates the path and movement every frame based on the target's current position,
        /// and updates <see cref="currentDirection"/> and <see cref="currentVelocity"/> accordingly.
        /// Suitable for homing, tracking, or follow-style movement. Supports custom easing and curved motion.
        /// </summary>
        /// <param name="target">The <see cref="Transform"/> to follow during the tween duration.</param>
        /// <param name="duration">Time (in seconds) the movement should take to reach the target's current position.</param>
        /// <param name="easeCurve">
        /// Optional <see cref="AnimationCurve"/> controlling how movement progresses over time.
        /// If null, falls back to <see cref="Ease.InOutSine"/> easing.
        /// </param>
        /// <param name="moveCurve">
        /// Optional <see cref="AnimationCurve"/> applied perpendicularly to produce arcs or lateral motion effects.
        /// If null, moves in a straight line toward the target.
        /// </param>
        /// <returns>
        /// The <see cref="Tween"/> managing the DOTween-based movement. Can be killed or chained as needed.
        /// </returns>
        protected override Tween MoveToTargetOverTime(Transform target, float duration, AnimationCurve easeCurve = null,
            AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;
            Vector2 previousPosition = startPos;
            float averageSpeed = Vector2.Distance(target.position, transform.position) / duration;

            var tween = DOTween.To(() => 0f, t =>
            {
                float linearT = Mathf.Clamp01(t);
                Vector2 currentTargetPos = target.position;
                Vector2 basePos = Vector2.Lerp(startPos, currentTargetPos, linearT);

                Vector2 direction = (currentTargetPos - startPos).normalized;
                Vector2 perpendicular = Vector2.Perpendicular(direction);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                Vector2 frameDirection = (curvedPos - previousPosition).normalized;
                if (frameDirection != Vector2.zero)
                    currentDirection = frameDirection;

                currentVelocity = currentDirection * averageSpeed;

                previousPosition = curvedPos;
                rb2D.MovePosition(curvedPos);
            }, 1f, duration);

            tween.OnComplete(() =>
            {
                postTweenVelocity = currentVelocity;
                blendBackTimer = 0f;
                isRecoveringFromTween = true;
            });

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        #endregion
    }
}