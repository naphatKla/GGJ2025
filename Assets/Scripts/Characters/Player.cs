using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters
{
    public class Player : CharacterBase
    {
        [SerializeField] [BoxGroup("Skills")] private float maxChargeMouseButton = 5f;
        [SerializeField] [BoxGroup("Upgrade")] private float cameraSizePerState = 3f;
        protected float leftClickTime;
        protected float rightClickTime;

        private void Start()
        {
            onSizeUpState.AddListener(() =>
            {
                int state = (int)(BubbleSize / 100) - 1;
                float size = CameraManager.Instance.StartOrthographicSize + (state * cameraSizePerState);
                CameraManager.Instance.SetLensOrthographicSize(size,0.3f);
            });
        }

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
