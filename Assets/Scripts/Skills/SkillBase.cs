using System.Collections;
using Characters;
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
        protected abstract void OnSkillStart();
        protected abstract void OnSkillEnd();
        
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
            OnSkillStart();
            onSkillStart?.Invoke();
            cooldownCounter = cooldown;
            StartCoroutine(SkillPerforming());
        }

        private IEnumerator SkillPerforming()
        {
            yield return new WaitForSeconds(skillDuration);
            if (!gameObject) yield break;
            OnSkillEnd();
            onSkillEnd?.Invoke();
        }
    }
}
