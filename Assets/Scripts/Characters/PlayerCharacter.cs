using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Skills;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Characters
{
    public class PlayerCharacter : CharacterBase
    {
        [SerializeField] private float accelerator = 6;
        #region Properties
        public static UnityEvent OnHitComboChanged = new UnityEvent();

        [BoxGroup("Random Skill")] 
        [SerializeField] private float _scoreReach;
        [BoxGroup("Random Skill")]
        [SerializeField] private float _nextScoreThreshold;
        [BoxGroup("Random Skill")] public SerializableDictionary<string, SkillBase> SkillDictionary = new();

        private static float _hitCombo;
        private bool isHitInvoked = false;
        private float _currentScore;
        private AudioFeedback _audiofeedback;
        private SkillBase _currentRandomSkill;

        public static float HitCombo
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

        private void Start()
        {
            _audiofeedback = GetComponent<AudioFeedback>();
            _currentRandomSkill = skillRight;
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance) return;
            Instance = this;
        }

        protected override void Update()
        {
            base.Update();
            MovementController();
            
            if (Score >= _nextScoreThreshold)
            {
                UnlockRandomSkill();
                _nextScoreThreshold += _scoreReach;
            }
        }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            if (IsDead) return;
            if (other.CompareTag("Enemy") && IsDash)
            {
                other.GetComponent<CharacterBase>().TakeDamage(this);
                skillLeft.SetCooldown(0.3f);
            }

        }
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods
        public IEnumerator ResetDashCooldown(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!isHitInvoked)
            {
                skillLeft.SetCooldown(0.5f);
            }
            isHitInvoked = false;
        }
        
        protected override void SkillInputHandler()
        {
            if (Time.timeScale == 0) return;
            if (IsStun) return;
            if (Input.GetMouseButtonDown(0))
                skillLeft.UseSkill();
            if (Input.GetMouseButtonDown(1))
                skillRight.UseSkill();
        }

        [Button]
        private void UnlockRandomSkill()
        {
            var availableSkills = SkillDictionary.Where(s => s.Value != _currentRandomSkill).ToList();
            if (availableSkills.Count == 0) return;

            var randomIndex = Random.Range(0, availableSkills.Count);
            var selectedSkill = availableSkills[randomIndex];

            _currentRandomSkill = selectedSkill.Value;
            if (_audiofeedback.soundFeedbacks.ContainsKey(selectedSkill.Key))
            {
                _audiofeedback.PlayMultipleAudio(selectedSkill.Key, "Sfx");
            }

            skillRight = selectedSkill.Value;
            selectedSkill.Value.InitializeSkill(this);
        }

        public override void TakeDamage(CharacterBase attacker)
        {
            if (IsDash) return;
            base.TakeDamage(attacker);
            //Tank Stun
            if (attacker.GetComponent<EnemyCharacter>().currentType == EnemyCharacter.EnemyType.Tank)
                StartCoroutine(Stun(0.5f));
            OnTakeDamage?.Invoke();
        }

        private static void ResetHitCombo()
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
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}