using UnityEngine;

namespace Characters
{
    public class Player : CharacterBase
    {
        protected override void Update()
        {
            base.Update();
            MovementController();
        }

        private void MovementController()
        {
            if (IsModifyingMovement) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            rigidbody2D.AddForce(mouseDirection * Speed);
            rigidbody2D.velocity = Vector2.ClampMagnitude(rigidbody2D.velocity, Speed);
        }
        
        protected override void SkillInputHandler()
        {
            if (Input.GetMouseButtonDown(0))
            {
                SkillMouseLeft.UseSkill();
            }
            
            if (Input.GetMouseButtonDown(1))
            {
                SkillMouseRight.UseSkill();
            }
        }
    }
}
