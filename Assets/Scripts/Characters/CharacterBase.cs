using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
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
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        [HideInInspector] public Rigidbody2D rigidbody2D;
        [ShowInInspector] protected float currentSpeed;
        protected Animator Animator;
        public bool canDead;
        public bool isDash;
        
        protected float CurrentSpeed => currentSpeed;
        public bool IsModifyingMovement { get; set; }
        protected abstract void SkillInputHandler();
        public float Score => score;


        protected virtual void Awake()
        {
            SkillMouseLeft?.InitializeSkill(this);
            SkillMouseRight?.InitializeSkill(this);
            rigidbody2D = GetComponent<Rigidbody2D>();
            currentSpeed = maxSpeed;
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            SkillMouseLeft?.UpdateCooldown();
            SkillMouseRight?.UpdateCooldown();
        }
        
        public virtual void Dead()
        {
            if (!canDead) return;
            DropOxygen(score);
            deadFeedback?.PlayFeedbacks();
            Destroy(gameObject);
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
                    Vector2 randomPosition = transform.position + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0);
                    ExpScript dropInstant = Instantiate(drop.gameObject, transform.position, Quaternion.identity).GetComponent<ExpScript>();
                    dropInstant.canPickUp = false;
                    dropInstant.transform.DOMove(randomPosition, 0.4f).SetEase(Ease.InOutSine).onComplete += () => dropInstant.canPickUp = true;
                }
            }
        }
        
        public virtual void SetScore(float score)
        {
            score = score;
        }
        
        public virtual void AddScore(float score)
        {
            score += score;
            
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
