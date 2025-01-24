using System;
using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Skills
{
    public abstract class SkillBase : MonoBehaviour
    {
        [SerializeField] [Title("SkillBase")] private float cooldown = 1f;
        [SerializeField] protected float skillDuration = 1f;
        protected CharacterBase OwnerCharacter;
        [Title("Events")] [PropertyOrder(100)] public UnityEvent onSKillStart;
        [PropertyOrder(100)] public UnityEvent onSkillEnd;
    
        /// <summary>
        /// Override this method to implement the skill logic
        /// </summary>
        protected abstract void SkillAction();
        
        public void InitializeSkill(CharacterBase ownerCharacter)
        {
            OwnerCharacter = ownerCharacter;
        }
    
        public void UpdateCooldown()
        {
            if (cooldown <= 0)
            {
                cooldown = 0;
                return;
            }
            cooldown -= Time.deltaTime;
        }
    
        public virtual void UseSkill()
        {
            if (cooldown > 0)
            {
                Debug.LogWarning("Skill is on cooldown");
                return;
            }
            
            SkillAction();
            onSKillStart?.Invoke();
            cooldown = 1;
            
            if (skillDuration <= 0) 
            {
                onSkillEnd?.Invoke();
                return;
            }
            DOVirtual.DelayedCall(skillDuration, () => onSkillEnd?.Invoke());
        }
    }
}
