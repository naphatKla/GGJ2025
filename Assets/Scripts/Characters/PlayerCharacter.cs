using System.Collections;
using System.Linq;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Characters
{
    public class PlayerCharacter : CharacterBase
    {
        #region Inspectors & Fields
        [SerializeField] private float accelerator = 6;
        [SerializeField] private int _kill;
        [SerializeField] private ParticleSystem enchantedParticleStateOne;
        [SerializeField] private ParticleSystem enchantedParticleStateTwo;
        #endregion -------------------------------------------------------------------------------------------------------------------
        
        #region Properties
        public static UnityEvent OnHitComboChanged = new UnityEvent();
        public static UnityEvent OnRandomNewSkill = new UnityEvent();
        public SkillBase SkillLeft => skillLeft;
        public SkillBase SkillRight => skillRight;

        [BoxGroup("Random Skill")] 
        [SerializeField] private float _scoreReach;
        [BoxGroup("Random Skill")]
        [SerializeField] private float _nextScoreThreshold;
        [BoxGroup("Random Skill")] public SerializableDictionary<string, SkillBase> SkillDictionary = new();

        private  int _hitCombo;
        private bool isHitInvoked = false;
        private float _currentScore;
        private AudioFeedback _audiofeedback;
        private SkillBase _currentRandomSkill;

        public int HitCombo
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

            if (!(Score >= _nextScoreThreshold)) return;
            UnlockRandomSkill();
            _nextScoreThreshold += _scoreReach;
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
        public override void DamageBoost(CharacterBase attacker)
        {
            base.DamageBoost(attacker);
            switch (damage)
            {
                case >= 3 :
                    enchantedParticleStateTwo.Play();
                    enchantedParticleStateOne.Stop();
                    break;
                case >= 2:
                    enchantedParticleStateOne.Play();
                    enchantedParticleStateTwo.Stop();
                    break;
            }
        }

        public override void ResetDamage(CharacterBase attacker)
        {
            base.ResetDamage(attacker);
            enchantedParticleStateTwo.Stop();
            enchantedParticleStateOne.Stop();
        }
        
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
            randomFeedback.PlayFeedbacks();
            if (_audiofeedback.soundFeedbacks.ContainsKey(selectedSkill.Key))
            {
                _audiofeedback.PlayMultipleAudio(selectedSkill.Key, "Sfx");
            }

            skillRight = selectedSkill.Value;
            OnRandomNewSkill?.Invoke();
            selectedSkill.Value.InitializeSkill(this);
        }

        public override void TakeDamage(CharacterBase attacker)
        {
            if (IsDash) return;
            base.TakeDamage(attacker);
            //Tank Stun
            if (attacker.GetComponent<EnemyCharacter>().currentType == EnemyCharacter.EnemyType.Tank)
                StartCoroutine(Stun(0.5f));
        }

        private void ResetHitCombo()
        {
            HitCombo = 0;
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