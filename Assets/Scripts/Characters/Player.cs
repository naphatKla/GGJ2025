using UnityEngine;

namespace Characters
{
    public class Player : CharacterBase
    {
        [SerializeField] private float maxChargeMouseButton = 5f;
        protected float leftClickTime;
        protected float rightClickTime;
        
        protected override void Update()
        {
            base.Update();
            MovementController();
        }

        private void MovementController()
        {
            if (IsModifyingMovement) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            rigidbody2D.AddForce(mouseDirection.normalized * Speed);
            rigidbody2D.velocity = Vector2.ClampMagnitude(rigidbody2D.velocity, Speed);
        }
        
        protected override void SkillInputHandler()
        {
            if (Input.GetMouseButton(0))
            {
                leftClickTime += Time.deltaTime;
                leftClickTime = Mathf.Clamp(leftClickTime, 0, maxChargeMouseButton);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                SkillMouseLeft.UseSkill(leftClickTime/maxChargeMouseButton);
                leftClickTime = 0;
            }
            
            if (Input.GetMouseButton(0))
            {
                rightClickTime += Time.deltaTime;
                rightClickTime = Mathf.Clamp(rightClickTime, 0, maxChargeMouseButton);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                SkillMouseRight.UseSkill(rightClickTime/maxChargeMouseButton);
                rightClickTime = 0;
            }
        }
    }
}
