using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    public class RigidbodyMovementSystem : BaseMovementSystem
    {
        [Required] [SerializeField] private Rigidbody2D rb2D;

        private void Awake()
        {
            if (rb2D) return;
            rb2D = GetComponent<Rigidbody2D>();
        }

        #region Methods

        protected override void MoveWithInertia(Vector2 direction)
        {
            currentDirection = Vector2.Lerp(currentDirection, direction, turnAccelerationRate * Time.deltaTime);
            Vector2 desiredVelocity = currentDirection * currentSpeed;
            currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, moveAccelerationRate * Time.deltaTime);
            Vector2 newPos = rb2D.position + currentVelocity * Time.deltaTime;
            TryMoveRawPosition(newPos);
        }

        protected override void MoveToPosition(Vector2 position)
        {
            rb2D.MovePosition(position);
        }

        protected override Tween MoveToPositionOverTime(Vector2 position, float duration,
            AnimationCurve easeCurve = null, AnimationCurve moveCurve = null)
        {
            Vector2 startPos = rb2D.position;
            Vector2 endPos = position;
            Vector2 perpendicular = Vector2.Perpendicular((endPos - startPos).normalized);
            Vector2 previousPosition = startPos;
            float averageSpeed = Vector2.Distance(position, startPos) / duration;

            float totalDistance = Vector2.Distance(startPos, endPos);
            float elapsed = 0f;
            int bounceCount = 0;

            var tween = DOTween.To(() => 0f, t =>
            {
                elapsed += Time.deltaTime;
                float remainingT = Mathf.Clamp01(1f - elapsed / duration);
                float linearT = Mathf.Clamp01(t);

                Vector2 basePos = Vector2.Lerp(startPos, endPos, linearT);
                float offset = moveCurve?.Evaluate(linearT) ?? 0f;
                Vector2 curvedPos = basePos + perpendicular * offset;

                currentDirection = (curvedPos - previousPosition).normalized;
                currentVelocity = currentDirection * averageSpeed;

                if (bounceDirection != Vector2.zero)
                {
                    float scale = 1f / (1f + bounceCount);
                    float newRemainingTime = duration * remainingT * scale;
                    float newRemainingDistance = totalDistance * remainingT * scale;

                    // 🎯 สร้างทิศทางใหม่ตาม bounce
                    startPos = rb2D.position;
                    endPos = startPos + bounceDirection * newRemainingDistance;
                    perpendicular = Vector2.Perpendicular(bounceDirection);
                    previousPosition = startPos;

                    // ⏱ รีเซ็ตตัวนับเวลา
                    elapsed = 0f;
                    duration = newRemainingTime + 0.125f;

                    bounceCount++;
                    bounceDirection = Vector2.zero;
                    Debug.Log("Bounce");
                    return;
                }

                previousPosition = curvedPos;
                TryMoveRawPosition(curvedPos);
            }, 1f, duration);

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

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

                currentDirection = (curvedPos - previousPosition).normalized;
                currentVelocity = currentDirection * averageSpeed;

                previousPosition = curvedPos;
                TryMoveRawPosition(curvedPos);
            }, 1f, duration);

            return easeCurve != null ? tween.SetEase(easeCurve) : tween.SetEase(Ease.InOutSine);
        }

        #endregion
    }
}