using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters
{
    public class Player : CharacterBase
    {
        [SerializeField] [BoxGroup("Upgrade")] private float cameraSizePerState = 3f;

        private void Start()
        {
            onSizeUpState.AddListener(() =>
            {
                ResizeCamera();
            });
        }

        public void ResizeCamera()
        {
            int state = (int)(BubbleSize / 100) - 1;
            float size = CameraManager.Instance.StartOrthographicSize + (state * cameraSizePerState);
            CameraManager.Instance.SetLensOrthographicSize(size,0.3f);
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
                SkillMouseLeft.UseSkill();
            }
            
            if (Input.GetMouseButton(1))
            {
                SkillMouseRight.UseSkill();
            }
        }
    }
}
