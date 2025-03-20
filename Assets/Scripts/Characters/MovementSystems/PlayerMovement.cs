using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystems
{
    public class PlayerMovement : BaseMovementSystem
    {
        [Required] [SerializeField] private Rigidbody2D rb2D;
        
        protected override void Move(Vector2 position)
        {
            Vector2 direction = position - (Vector2)transform.position;
            Vector2 newPos = (Vector2)transform.position + direction.normalized * (currentSpeed * Time.fixedDeltaTime);
            rb2D.MovePosition(newPos);
        }

        protected override void MoveToPositionOverTime(Vector2 position, float duration, Ease ease = Ease.InOutSine)
        {
            rb2D.DOMove(position, duration).SetEase(ease);
        }
    }
}