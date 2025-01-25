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
        [Title("Events")] [PropertyOrder(100)] public UnityEvent onSkillStart;
        [PropertyOrder(100)] public UnityEvent onSkillEnd;
        
        protected CharacterBase OwnerCharacter;
        protected bool IsPlayer => OwnerCharacter.CompareTag("Player");
        protected float cooldownCounter;
    
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
            if (cooldownCounter <= 0)
            {
                cooldownCounter = 0;
                return;
            }
            cooldownCounter -= Time.deltaTime;
        }
    
        public virtual void UseSkill()
        {
            if (cooldownCounter > 0) return;
            
            SkillAction();
            onSkillStart?.Invoke();
            cooldownCounter = cooldown;
            
            if (skillDuration <= 0) 
            {
                onSkillEnd?.Invoke();
                return;
            }
            DOVirtual.DelayedCall(skillDuration, () => onSkillEnd?.Invoke());
        }
    }
}
