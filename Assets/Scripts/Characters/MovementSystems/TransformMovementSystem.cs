using DG.Tweening;
using UnityEngine;

namespace Characters.MovementSystems
{
    /// <summary>
    /// Movement system that uses Transform.position directly (no Rigidbody2D).
    /// Suitable for characters that don't rely on Unity physics.
    /// </summary>
    public class TransformMovementSystem : BaseMovementSystem
    {
        #region Methods

        protected override void MoveWithInertia(Vector2 direction, Vector2? overrideVelocity = null)
        {
            float dt = Time.fixedDeltaTime;
            currentDirection = SmoothVector(currentDirection, direction, turnAccelerationRate);
            Vector2 desiredVelocity = overrideVelocity ?? currentDirection * currentSpeed;
            currentVelocity = SmoothVector(currentVelocity, desiredVelocity, moveAccelerationRate);
            Vector2 newPos = (Vector2)transform.position + currentVelocity * dt;
            TryMoveRawPosition(newPos);
        }

        protected override void MoveToPosition(Vector2 position)
        {
            transform.position = position;
        }

        protected override Tween MoveToPositionOverTime(Vector2 position, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = transform.position;
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
                float frameSpeed = frameDelta.magnitude / Time.fixedDeltaTime;

                if (frameDelta.normalized.magnitude >= 0.01f)
                {
                    currentDirection = frameDelta.normalized;
                    currentVelocity = currentDirection * frameSpeed;
                }

                previousPosition = curvedPos;
                TryMoveRawPosition(curvedPos);
            }, 1f, duration).SetUpdate(UpdateType.Fixed);

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        protected override Tween MoveToTargetOverTime(Transform target, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = transform.position;
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
                float frameSpeed = frameDelta.magnitude / Time.fixedDeltaTime;

                if (frameDelta.normalized.magnitude >= 0.01f)
                {
                    currentDirection = frameDelta.normalized;
                    currentVelocity = currentDirection * frameSpeed;
                }

                previousPosition = curvedPos;
                TryMoveRawPosition(curvedPos);
            }, 1f, duration).SetUpdate(UpdateType.Fixed);

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        protected Vector2 SmoothVector(Vector2 targetVector, Vector2 desiredVector, float lerpSpeed)
        {
            float dt = Time.fixedDeltaTime;
            float lerpFactor = 1f - Mathf.Exp(-lerpSpeed * dt);
            return Vector2.Lerp(targetVector, desiredVector, lerpFactor);
        }

        #endregion
    }
}
