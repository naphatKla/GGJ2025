using System;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.Events;

namespace Characters
{
    public class PlayerCharacter : CharacterBase
    {
        #region Inspectors & Fields
        [SerializeField] private float accelerator = 6;
        [SerializeField] private float _hitCombo;
        [SerializeField] private int _kill;
        #endregion -------------------------------------------------------------------------------------------------------------------
        #region Properties
        public static UnityEvent OnHitComboChanged = new UnityEvent();
        public SkillBase SkillLeft => skillLeft;
        public SkillBase SkillRight => skillRight;

        public float HitCombo
        {
            get => _hitCombo;
            set
            {
                if (_hitCombo != value)
                {
                    _hitCombo = value;
                    OnHitComboChanged?.Invoke();
                }
            }
        }
        public int Kill
        {
            get => _kill;
            set => _kill = value;
        }
        public float Score
        {
            get => score;
            
        }
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
            if (IsStun) return;
            if (Input.GetMouseButtonDown(0))
                skillLeft.UseSkill();
            if (Input.GetMouseButtonDown(1))
                skillRight.UseSkill();
        }

        public override void TakeDamage(CharacterBase attacker)
        {
            if (IsDash) return;
            base.TakeDamage(attacker);
            //Tank Stun
            if (attacker.GetComponent<EnemyCharacter>().currentType == EnemyCharacter.EnemyType.Tank && IsDash)
                StartCoroutine(Stun(0.5f));
            OnTakeDamage?.Invoke();
        }

        private void ResetHitCombo()
        {
            HitCombo = 0f;
        }

        private void MovementController()
        {
            if (IsStoppingMovementController) return;
            if (IsStun) return;
            Vector2 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            Rigid2D.AddForce(mouseDirection.normalized * (accelerator * Time.deltaTime), ForceMode2D.Impulse);
            Rigid2D.velocity = Vector2.ClampMagnitude(Rigid2D.velocity, CurrentSpeed);
        }

        public override void AddScore(float scoreToAdd)
        {
            base.AddScore(scoreToAdd);
        }
        
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}