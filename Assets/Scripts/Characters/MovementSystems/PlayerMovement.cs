using Characters.MovementSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.MovementSystem
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
    }
}