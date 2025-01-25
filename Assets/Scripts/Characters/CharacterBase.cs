using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [SerializeField] protected float score = 0;
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase SkillMouseLeft;
        [SerializeField] [BoxGroup("Skills")] protected  SkillBase SkillMouseRight;
        [SerializeField] [BoxGroup("Feedbacks")] public MMF_Player killFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        [HideInInspector] public Rigidbody2D rigidbody2D;
        public bool CanDead { get; set; } = true;
        public bool IsDash { get; set; }
        protected Animator Animator;
        private float currentSpeed;
        protected bool isDead;
        
        protected float CurrentSpeed => currentSpeed;
        public bool IsModifyingMovement { get; set; }
        protected abstract void SkillInputHandler();
        public float Score => score;
        private Animator _animator;
        
        protected virtual void Awake()
        {
            SkillMouseLeft?.InitializeSkill(this);
            SkillMouseRight?.InitializeSkill(this);
            rigidbody2D = GetComponent<Rigidbody2D>();
            currentSpeed = maxSpeed;
        }
        
        protected virtual void Start()
        {
            _animator = GetComponent<Animator>();
            
            SkillMouseLeft?.onSkillStart.AddListener((() =>
            {
                _animator.SetTrigger("DashTrigger");
            }));
            
            SkillMouseRight?.onSkillStart.AddListener((() =>
            {
                _animator.SetTrigger("BlackHoleTrigger");
            }));
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            SkillMouseLeft?.UpdateCooldown();
            SkillMouseRight?.UpdateCooldown();
        }
        
        public virtual void Dead(CharacterBase killer, bool dropOxygen = true) 
        {
            if (!CanDead) return;
            if (isDead) return;
            isDead = true;
            if (dropOxygen)
                DropOxygen(score);
            if (gameObject.CompareTag("Enemy"))
                Player.Hitcombo += 1;
            deadFeedback?.PlayFeedbacks();
            killer?.killFeedback?.PlayFeedbacks();
            _animator?.SetTrigger("DeadTrigger");
            Destroy(gameObject,0.4f);
        }

        protected virtual void DropOxygen(float amount)
        {
            float sumDrop = 0;
            foreach (ExpScript drop in RandomSpawnExp.Instance.OxygenAvailable)
            {
                while (sumDrop + drop.expAmount <= amount)
                {
                    sumDrop += drop.expAmount;
                    float radius = transform.localScale.x;
                    Vector2 randomPosition = transform.position + new Vector3(Random.Range(-radius*2, radius*2), Random.Range(-radius*2, radius*2), 0);
                    ExpScript dropInstant = Instantiate(drop.gameObject, transform.position, Quaternion.identity).GetComponent<ExpScript>();
                    dropInstant.canPickUp = false;
                    dropInstant.transform.DOMove(randomPosition, 0.4f).SetEase(Ease.InOutSine).onComplete += () => dropInstant.canPickUp = true;
                }
            }
        }
        
        public virtual void SetScore(float score)
        {
            this.score = score;
        }
        
        public virtual void AddScore(float score)
        {
            this.score += score;
            
            switch (score)
            {
                case > 0:
                    break;
                case < 0:
                    DropOxygen(Mathf.Abs(score));
                    break;
            }
        }
    }
}
