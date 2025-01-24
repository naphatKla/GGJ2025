using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Skills;
using UnityEngine;
using UnityEngine.Serialization;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [SerializeField] private float bubbleSize = 1f;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float increaseScalePerSize = 0.01f;
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
        
            if (size > 0) return;
            Dead();
        }

        public void UpdateScale()
        {
            Vector2 newScale = Vector2.one * (bubbleSize * increaseScalePerSize);
            transform.DOScale(newScale, 0.05f).SetEase(Ease.OutBounce);
        }
    }
}
