using System.Collections;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        #region Inspectors & Fields

        [SerializeField] protected float score;
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] [BoxGroup("Life")] protected int life = 1;
        [SerializeField] [BoxGroup("Life")] protected float iframeAfterHitDuration;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase skillLeft;
        [SerializeField] [BoxGroup("Skills")] protected  SkillBase skillRight;
        [SerializeField] [BoxGroup("Feedbacks")] public MMF_Player killFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player takeDamageFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        [BoxGroup("Events")] [PropertyOrder(100f)] public UnityEvent onDead;
        private Rigidbody2D _rigidBody2D;
        private float _currentSpeed;
        private Animator _animator;
        protected bool IsDead;
        private float _lastHitTime;
        private static readonly int DeadTriggerAnimation = Animator.StringToHash("DeadTrigger");
        private static readonly int DashTriggerAnimation = Animator.StringToHash("DashTrigger");
        private static readonly int BlackHoleTriggerAnimation = Animator.StringToHash("BlackHoleTrigger");

        #endregion -------------------------------------------------------------------------------------------------------------
        
        #region Properties 
        protected float CurrentSpeed => _currentSpeed;
        public float Score => score;
        public bool IsModifyingMovement { get; set; }
        public bool IsIframe { get; set; }
        public bool IsDash { get; set; }
        protected Rigidbody2D Rigid2D => _rigidBody2D;
        #endregion -------------------------------------------------------------------------------------------------------------
        
        #region UnityMethods 
        protected virtual void Awake()
        {
            skillLeft?.InitializeSkill(this);
            skillRight?.InitializeSkill(this);
            _rigidBody2D = GetComponent<Rigidbody2D>();
            _currentSpeed = maxSpeed;
        }
        
        protected virtual void Start()
        {
            _animator = GetComponent<Animator>();
            skillLeft?.onSkillStart.AddListener(() => _animator.SetTrigger(DashTriggerAnimation));
            skillRight?.onSkillStart.AddListener(() => _animator.SetTrigger(BlackHoleTriggerAnimation));
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            skillLeft?.UpdateCooldown();
            skillRight?.UpdateCooldown();
        }
        
        private IEnumerator DeadAndDestroy()
        {
            yield return null;
            yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
            Destroy(gameObject);
        }
        #endregion ---------------------------------------------------------------------------------------------
        
        #region Methods
        protected abstract void SkillInputHandler();
        protected virtual void SetScore(float scoreToSet)
        {
            score = scoreToSet;
        }

        protected virtual void AddScore(float scoreToAdd)
        {
            score += scoreToAdd;
            if (scoreToAdd >= 0) return; 
            DropOxygen(Mathf.Abs(scoreToAdd));
        }
        
        public virtual void Dead(CharacterBase killer, bool dropOxygen = true) 
        {
            if (IsIframe) return;
            if (IsDead) return;
            if (Time.time - _lastHitTime < iframeAfterHitDuration) return;
            life--;
            _lastHitTime = Time.time;
            takeDamageFeedback?.PlayFeedbacks();
            if (life > 0) return;
            
            IsDead = true;
            if (dropOxygen)
                DropOxygen(score);
            
            if (CompareTag("Player"))
                _rigidBody2D.velocity = Vector2.zero;
            else if (CompareTag("Enemy"))
            {
                Player.HitCombo++;
                NavMeshAgent navmesh = GetComponent<NavMeshAgent>();
                navmesh.velocity = Vector3.zero;
                navmesh.enabled = false;
            }
            
            if (killer is CloningCharacter)
                killer.GetComponent<CloningCharacter>().OwnerCharacter.killFeedback?.PlayFeedbacks();
            else
                killer?.killFeedback?.PlayFeedbacks();
            
            _animator.SetTrigger(DeadTriggerAnimation);
            _animator.Play("Dead");
            deadFeedback?.PlayFeedbacks();
            onDead?.Invoke();
            StartCoroutine(DeadAndDestroy());
        }
        
        protected virtual void DropOxygen(float amount)
        {
            float sumDrop = 0;
            foreach (Oxygen drop in OxygenSpawnManager.Instance.AllOfOxygenType)
            {
                while (sumDrop + drop.scoreAmount <= amount)
                {
                    sumDrop += drop.scoreAmount;
                    Transform characterTransform = transform;
                    float radius = characterTransform.localScale.x * 2f;
                    Vector2 randomPosition = characterTransform.position + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0);
                    Instantiate(drop.gameObject, transform.position, Quaternion.identity).TryGetComponent(out Oxygen oxygenDrop);
                    oxygenDrop.transform.DOMove(randomPosition, 0.4f).SetEase(Ease.InOutSine).onComplete += () => oxygenDrop.canPickUp = true;
                    oxygenDrop.canPickUp = false;
                }
            }
        }
        #endregion -------------------------------------------------------------------------------------------------------------
    }
}
