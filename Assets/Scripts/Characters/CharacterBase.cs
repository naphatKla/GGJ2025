using System;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters
{
    public abstract class CharacterBase : MonoBehaviour
    {
        [SerializeField] private float bubbleSize = 1f;
        [SerializeField] private float speed = 1f;
        [SerializeField] private MMF_Player sizeUpFeedback;
        [SerializeField] private MMF_Player sizeDownFeedback;
        [SerializeField] private MMF_Player deadFeedback;
        [ShowInInspector] private SkillBase _skill;
        public float BubbleSize => bubbleSize;
        public float Speed => speed;
        
        protected void Awake()
        {
            _skill = new SkillTest();
            _skill.InitializeSkill(this);
        }

        protected virtual void Dead()
        {
            Destroy(gameObject);
            deadFeedback.PlayFeedbacks();
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
    }
}
