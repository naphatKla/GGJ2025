using UnityEngine;
using UnityEngine.Events;

namespace Characters
{
    public class PlayerCharacter : CharacterBase
    {
        #region Properties
        public static float HitCombo;
        public static PlayerCharacter Instance { get; private set; }
        public UnityEvent OnTakeDamage = new UnityEvent();
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods
        private void OnEnable()
        {
            onDead?.AddListener(ResetHitCombo);
        }

        protected void OnDisable()
        {
            onDead?.RemoveListener(ResetHitCombo);
        }

        protected override void Awake()
        {
            base.Awake();
            if (!Instance)
                Instance = this;
        }

        protected override void Update()
        {
            base.Update();
            MovementController();
        }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            if (IsDead) return;
            if (other.CompareTag("Enemy") && IsDash)
                other.GetComponent<CharacterBase>().TakeDamage(this);
        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        protected override void SkillInputHandler()
        {
            if (Time.timeScale == 0) return;
            if (Input.GetMouseButtonDown(0))
                skillLeft.UseSkill();
            if (Input.GetMouseButtonDown(1))
                skillRight.UseSkill();
        }

        public override void TakeDamage(CharacterBase attacker)
        {
            if (IsDash) return;
            base.TakeDamage(attacker);
            OnTakeDamage?.Invoke();
        }
        
        private static void ResetHitCombo()
        {
            HitCombo = 0f;
        }

        private void MovementController()
        {
            if (IsModifyingMovement) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            Rigid2D.AddForce(mouseDirection.normalized * CurrentSpeed);
            Rigid2D.velocity = Vector2.ClampMagnitude(Rigid2D.velocity, CurrentSpeed);
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}