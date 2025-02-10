using Characters;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Skills
{
    public abstract class SkillBase : MonoBehaviour
    {
        #region Inspectors & Fields

        [Title("SkillBase")] [SerializeField] [BoxGroup("Duration")]
        private float cooldown = 1f; 

        [Title("Feedbacks")] [SerializeField] [PropertyOrder(99)]
        private MMF_Player skillStartFeedback;

        [SerializeField] [PropertyOrder(99)] private MMF_Player skillEndFeedback;
        [Title("Events")] [PropertyOrder(100)] public UnityEvent onSkillStart;
        [PropertyOrder(100)] public UnityEvent onSkillEnd;
        protected CharacterBase OwnerCharacter;
        private float _cooldownCounter;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Properties

        protected bool IsPlayer => OwnerCharacter.CompareTag("Player");
        public float Cooldown => cooldown;
        public float CooldownCounter => _cooldownCounter;

        #endregion -------------------------------------------------------------------------------------------------------------------

        #region Methods

        protected abstract void OnSkillStart();
        protected abstract void OnSkillEnd();

        public virtual void InitializeSkill(CharacterBase ownerCharacter)
        {
            OwnerCharacter = ownerCharacter;
        }

        public void UpdateCooldown()
        {
            if (_cooldownCounter <= 0)
            {
                _cooldownCounter = 0;
                return;
            }

            _cooldownCounter -= Time.deltaTime;
        }

        public virtual void UseSkill()
        {
            if (_cooldownCounter > 0) return;
            onSkillStart?.Invoke();
            skillStartFeedback?.PlayFeedbacks();
            OnSkillStart();
            _cooldownCounter = cooldown;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Please call this method on skill end condition
        /// </summary>
        protected virtual void ExitSkill()
        {
            OnSkillEnd();
            onSkillEnd?.Invoke();
            skillEndFeedback?.PlayFeedbacks();
        }
        
        protected virtual Vector2 GetTargetDirection()
        {
            if (!IsPlayer)
                return (PlayerCharacter.Instance.transform.position - OwnerCharacter.transform.position).normalized;
            
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - (Vector2)OwnerCharacter.transform.position).normalized;
            return direction;
        }
        
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}