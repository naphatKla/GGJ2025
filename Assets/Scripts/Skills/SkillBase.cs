using System;
using Characters;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
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
        protected MMF_Player skillStartFeedback;

        [SerializeField] [PropertyOrder(99)] private MMF_Player skillEndFeedback;
        [Title("Events")] [PropertyOrder(100)] public UnityEvent onSkillStart;
        [PropertyOrder(100)] public UnityEvent onSkillEnd;
        [PropertyOrder(100)] public UnityEvent onCooldownEnd;
        protected CharacterBase OwnerCharacter;
        private float _cooldownCounter;
        private bool _isThisSkillPerforming;

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
            _cooldownCounter = 0f;
        }

        public void UpdateCooldown()
        {
            if (_cooldownCounter <= 0)
            {
                _cooldownCounter = 0;
                return;
            }

            _cooldownCounter -= Time.deltaTime;
            if (_cooldownCounter <= 0)
            {
                onCooldownEnd?.Invoke();
            }
        }
        
        public void SetCooldown(float newCooldown)
        {
            cooldown = newCooldown;
        }
        
        public virtual void UseSkill()
        {
            if (_cooldownCounter > 0) return;
            if (_isThisSkillPerforming) return;
            if (!OwnerCharacter.CanUseSkill) return;
            
            OnSkillStart();
            onSkillStart?.Invoke();
            skillStartFeedback?.PlayFeedbacks();
            _cooldownCounter = cooldown;
            _isThisSkillPerforming = true;
            
            if (OwnerCharacter.CompareTag("Player"))
            {
                PlayerCharacter player = OwnerCharacter as PlayerCharacter;
                player.StartCoroutine(player.ResetDashCooldown(0.1f));  
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Please call this method on skill end condition
        /// </summary>
        protected virtual void ExitSkill()
        {
            if (!_isThisSkillPerforming) return;
            _isThisSkillPerforming = false;
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

        private void OnDestroy()
        {
            DOTween.Kill(gameObject);
        }

        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}