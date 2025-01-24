using System;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skills;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [SerializeField] private float bubbleSize = 1f;
        [SerializeField] private float speed = 1f;
        [SerializeField] [PropertyTooltip("1 bubble size will affect to the object scale += 0.01")] [BoxGroup("Upgrade")] private float increaseScalePerSize = 0.01f; 
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 9f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 2f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenCurveForce = 5f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private LayerMask oxygenLayer;
        [SerializeField] [BoxGroup("Skills")] protected SkillBase SkillMouseLeft;
        [SerializeField] [BoxGroup("Skills")] protected  SkillBase SkillMouseRight;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeUpFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeDownFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        [SerializeField] private ExpScript[] oxygenDrops;
        [HideInInspector] public Rigidbody2D rigidbody2D;
        protected float lastStateSize = 100f;
        
        public float BubbleSize => bubbleSize;
        protected float Speed => speed;
        public bool IsModifyingMovement { get; set; }
        protected abstract void SkillInputHandler();
        [Title("Events")] public UnityEvent onSizeUpState;
     
        
        protected virtual void Awake()
        {
            SkillMouseLeft?.InitializeSkill(this);
            SkillMouseRight?.InitializeSkill(this);
            rigidbody2D = GetComponent<Rigidbody2D>();
            oxygenDrops.Sort((x, y) => x.expAmount.CompareTo(y.expAmount));
            Array.Reverse(oxygenDrops);
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            SkillMouseLeft?.UpdateCooldown();
            SkillMouseRight?.UpdateCooldown();
            
            // เก็บ oxygen รอบๆรัศมี
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, (transform.localScale.x / 2) + oxygenDetectionRadius, oxygenLayer);
            
            foreach (var collider in colliders)
            {
                Vector2 direction = (transform.position - collider.transform.position).normalized;
                Vector2 perpendicularRight = new Vector2(direction.y, -direction.x).normalized;
                Vector2 combinedVector = (direction + perpendicularRight).normalized;
                float force = oxygenMagneticStartForce - (Time.deltaTime*3);
                force = Mathf.Clamp(force, oxygenMagneticEndForce, oxygenMagneticStartForce);
                collider.transform.position += (Vector3) (combinedVector * force * Time.deltaTime);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Exp")) return;
            ExpScript exp = other.GetComponent<ExpScript>();
            if (!exp.canPickUp) return;
            
            AdjustSize(exp.expAmount);
            UpdateScale();
            Destroy(other.gameObject);
            return;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            float distance = Vector2.Distance(transform.position, other.transform.position);
            float thisRadius = (transform.localScale.x / 2);
            CharacterBase otherCharacter = other.GetComponent<CharacterBase>();
            if (!otherCharacter) return;
            bool canEat = (distance <= thisRadius) && (bubbleSize > otherCharacter.bubbleSize);
            if (!canEat) return;
            AdjustSize(otherCharacter.bubbleSize);
            UpdateScale();
            otherCharacter.Dead();
        }
        
        [Button]
        protected virtual void Dead()
        {
            float sumDrop = 0;
            
            foreach (ExpScript drop in oxygenDrops)
            {
                while (sumDrop + drop.expAmount <= bubbleSize)
                {
                    sumDrop += drop.expAmount;
                    float radius = transform.localScale.x;
                    Vector2 randomPosition = transform.position + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0);
                    ExpScript dropInstant = Instantiate(drop.gameObject, transform.position, Quaternion.identity).GetComponent<ExpScript>();
                    dropInstant.canPickUp = false;
                    dropInstant.transform.DOMove(randomPosition, 0.15f).SetEase(Ease.OutBounce).onComplete += () => dropInstant.canPickUp = true;
                }
            }
            
            deadFeedback?.PlayFeedbacks();
            Destroy(gameObject);
        }
        
        public virtual void AdjustSize(float size)
        {
            bubbleSize += size;
            if (Mathf.Abs(bubbleSize - lastStateSize) >= 100) 
            {
                onSizeUpState?.Invoke();
                lastStateSize = bubbleSize;
            }
            
            switch (size)
            {
                case > 0:
                    sizeUpFeedback?.PlayFeedbacks();
                    break;
                case < 0:
                    sizeDownFeedback?.PlayFeedbacks();
                    break;
            }
        
            if (bubbleSize > 0) return;
            Dead();
        }

        public void UpdateScale()
        {
            Vector2 newScale = Vector2.one * (bubbleSize * increaseScalePerSize);
            transform.DOScale(newScale, 0.05f).SetEase(Ease.OutBounce);
        }
        

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, (transform.localScale.x/2) + oxygenDetectionRadius);
        }
    }
}
