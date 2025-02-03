using System.Collections;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        #region Inspectors & Fields 
        [SerializeField] protected float score = 0;
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase skillLeft;
        [SerializeField] [BoxGroup("Skills")] protected  SkillBase skillRight;
        [SerializeField] [BoxGroup("Feedbacks")] public MMF_Player killFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        private Rigidbody2D _rigidbody2D;
        private float _currentSpeed;
        private Animator _animator;
        protected bool IsDead;
        protected Animator Animator;
        private static readonly int DeadTrigger = Animator.StringToHash("DeadTrigger");

        #endregion -------------------------------------------------------------------------------------------------------------
        
        #region Properties 
        protected float CurrentSpeed => _currentSpeed;
        public float Score => score;
        public bool IsModifyingMovement { get; set; }
        public bool IsIframe { get; set; } = true;
        public bool IsDash { get; set; }
        protected Rigidbody2D Rigid2D => _rigidbody2D;
        #endregion -------------------------------------------------------------------------------------------------------------
        
        #region UnityMethods 
        protected virtual void Awake()
        {
            skillLeft?.InitializeSkill(this);
            skillRight?.InitializeSkill(this);
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _currentSpeed = maxSpeed;
        }
        
        protected virtual void Start()
        {
            _animator = GetComponent<Animator>();
            skillLeft?.onSkillStart.AddListener(() => _animator.SetTrigger("DashTrigger"));
            skillRight?.onSkillStart.AddListener(() => _animator.SetTrigger("BlackHoleTrigger"));
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
            
            IsDead = true;
            if (dropOxygen)
                DropOxygen(score);
            
            if (CompareTag("Player"))
                _rigidbody2D.velocity = Vector2.zero;
            else if (CompareTag("Enemy"))
            {
                Player.Hitcombo++;
                NavMeshAgent navmesh = GetComponent<NavMeshAgent>();
                navmesh.velocity = Vector3.zero;
                navmesh.enabled = false;
            }
            
            if (killer is CloningCharacter)
                killer.GetComponent<CloningCharacter>().OwnerCharacter.killFeedback?.PlayFeedbacks();
            else
                killer?.killFeedback?.PlayFeedbacks();
            
            _animator.SetTrigger(DeadTrigger);
            _animator.Play("Dead");
            deadFeedback?.PlayFeedbacks();
            StartCoroutine(DeadAndDestroy());
        }
        
        protected virtual void DropOxygen(float amount)
        {
            float sumDrop = 0;
            foreach (Oxygen drop in OxygenSpawnManager.Instance.AllOfOxygenType)
            {
                while (sumDrop + drop.expAmount <= amount)
                {
                    sumDrop += drop.expAmount;
                    Transform characterTransform = transform;
                    float radius = characterTransform.localScale.x;
                    Vector2 randomPosition = characterTransform.position + new Vector3(Random.Range(-radius*2, radius*2), Random.Range(-radius*2, radius*2), 0);
                    Instantiate(drop.gameObject, transform.position, Quaternion.identity).TryGetComponent<Oxygen>(out Oxygen oxygenDrop);
                    oxygenDrop.transform.DOMove(randomPosition, 0.4f).SetEase(Ease.InOutSine).onComplete += () => oxygenDrop.canPickUp = true;
                    oxygenDrop.canPickUp = false;
                }
            }
        }
        #endregion -------------------------------------------------------------------------------------------------------------
    }
}
