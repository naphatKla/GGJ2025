using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float score = 0;
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 9f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenCurveForce = 5f;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase SkillMouseLeft;
        [SerializeField] [BoxGroup("Skills")] protected  SkillBase SkillMouseRight;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeUpFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeDownFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider2D;
        [HideInInspector] public Rigidbody2D rigidbody2D;
        protected float lastLocalScale;
        [ShowInInspector] protected float currentSpeed;
        protected Animator Animator;
        public bool canDead;
        public bool isDash;

        public float Score => score;
        protected float CurrentSpeed => currentSpeed;
        public bool IsModifyingMovement { get; set; }
        protected abstract void SkillInputHandler();


        protected virtual void Awake()
        {
            SkillMouseLeft?.InitializeSkill(this);
            SkillMouseRight?.InitializeSkill(this);
            rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider2D = GetComponent<Collider2D>();
            currentSpeed = maxSpeed;
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            SkillMouseLeft?.UpdateCooldown();
            SkillMouseRight?.UpdateCooldown();
            
            // เก็บ oxygen รอบๆรัศมี
            if (!gameObject.CompareTag("Player")) return;
            if (!_spriteRenderer.enabled) return;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, (transform.localScale.x / 2) + oxygenDetectionRadius, LayerMask.GetMask("EXP"));
            
            foreach (var collider in colliders)
            {
                Vector2 direction = (transform.position - collider.transform.position).normalized;
                Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                Vector2 combinedVector = (direction + perpendicularRight).normalized;
                float force = oxygenMagneticStartForce - (Time.deltaTime*3);
                force = Mathf.Clamp(force, oxygenMagneticEndForce, oxygenMagneticStartForce);
                collider.transform.position += (Vector3)(combinedVector * force * Time.deltaTime);
            }
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
            this.score = score;
        }
        
        public virtual void AddScore(float score)
        {
            this.score += score;
            if (Mathf.Abs(transform.localScale.x - lastLocalScale) >= 1 && !_isExploding) 
            {
                onSizeUpState?.Invoke();
                lastLocalScale = transform.localScale.x;
            }
            
            switch (score)
            {
                case > 0:
                    sizeUpFeedback?.PlayFeedbacks();
                    break;
                case < 0:
                    sizeDownFeedback?.PlayFeedbacks();
                    DropOxygen(Mathf.Abs(score));
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, (transform.localScale.x/2) + oxygenDetectionRadius);
        }
    }
}
