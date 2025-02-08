using System;
using System.Collections;
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

        [SerializeField] [BoxGroup("Duration")]
        protected float skillDuration = 1f;

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
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods
        private IEnumerator SkillPerforming()
        {
            yield return new WaitForSeconds(skillDuration);
            if (!gameObject) yield break;
            OnSkillEnd();
            onSkillEnd?.Invoke();
            skillEndFeedback?.PlayFeedbacks();
        }
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
            OnSkillStart();
            onSkillStart?.Invoke();
            skillStartFeedback?.PlayFeedbacks();
            _cooldownCounter = cooldown;
            StartCoroutine(SkillPerforming());
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
