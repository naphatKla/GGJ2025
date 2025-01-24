using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [SerializeField] private float bubbleSize = 1f;
        [SerializeField] private float speed = 1f;
        [SerializeField] [PropertyTooltip("1 bubble size will affect to the object scale += 0.01")] private float increaseScalePerSize = 0.01f; 
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenDetectionRadius = 1f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticStartForce = 3f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenMagneticEndForce = 3f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private float oxygenCurveForce = 2.6f;
        [SerializeField] [BoxGroup("PickUpOxygen")] private LayerMask oxygenLayer;
        [SerializeField] [SceneObjectsOnly] [BoxGroup("Skills")] protected SkillBase SkillMouseLeft;
        [SerializeField] [SceneObjectsOnly] [BoxGroup("Skills")] protected  SkillBase SkillMouseRight;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeUpFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player sizeDownFeedback;
        [SerializeField] [BoxGroup("Feedbacks")] private MMF_Player deadFeedback;
        [HideInInspector] public Rigidbody2D rigidbody2D;
        public float BubbleSize => bubbleSize;
        protected float Speed => speed;
        public bool IsModifyingMovement { get; set; }
        protected abstract void SkillInputHandler();
        
        protected virtual void Awake()
        {
            SkillMouseLeft?.InitializeSkill(this);
            SkillMouseRight?.InitializeSkill(this);
            rigidbody2D = GetComponent<Rigidbody2D>();
        }
        
        protected virtual void Update()
        {
            SkillInputHandler();
            SkillMouseLeft.UpdateCooldown();
            SkillMouseRight.UpdateCooldown();
            
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
            if (other.CompareTag("Exp"))
            {
                ExpScript exp = other.GetComponent<ExpScript>();
                AdjustSize(exp.expAmount);
                UpdateScale();
                Destroy(other.gameObject);
            }
        }
        
        protected virtual void Dead()
        {
            Destroy(gameObject);
            deadFeedback?.PlayFeedbacks();
        }
        
        public virtual void AdjustSize(float size)
        {
            bubbleSize += size;
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
