using System;
using System.Collections;
using Characters;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Skills
{
    public class SkillPerfectBubble : SkillBase
    {
        #region Inspectors & Fields

        [Title("SkillPerfectBubble")] 
        [SerializeField] private float iframeDuration = 1f;
        [SerializeField] private float counterDashDistance = 8f;
        private bool _gotHit;
        #endregion -------------------------------------------------------------------------------------------------------------------

        #region UnityMethods

        public override void InitializeSkill(CharacterBase ownerCharacter)
        {
            base.InitializeSkill(ownerCharacter);
            OwnerCharacter.onHit.AddListener(() => _gotHit = true);
        }
        
        private IEnumerator StartIframe()
        {
            float timer = 0;
            while (timer <= iframeDuration && !_gotHit)
            {
                if (!OwnerCharacter) yield break;
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!_gotHit) yield break;
            Vector2 dashPosition;
            Vector2 direction;
            OwnerCharacter.IsModifyingMovement = true;
            OwnerCharacter.IsDash = true;
            
            if (IsPlayer)
            {
                dashPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                direction = (dashPosition - (Vector2)OwnerCharacter.transform.position).normalized;
            }
            else
            {
                OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
                direction = agent.velocity.normalized;
                agent.enabled = false;
            }
            
            dashPosition = (Vector2)OwnerCharacter.transform.position + (direction * counterDashDistance);
            OwnerCharacter.TryGetComponent(out Rigidbody2D rigid2D);
            rigid2D.velocity = Vector2.zero;
            OwnerCharacter.transform.DOMove(dashPosition, skillDuration).SetEase(Ease.InOutSine);
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
        
        #region Methods
        protected override void OnSkillStart()
        {
            StartCoroutine(StartIframe());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsModifyingMovement = false;
            OwnerCharacter.IsDash = false;
            if (IsPlayer) return;
            OwnerCharacter.TryGetComponent(out NavMeshAgent agent);
            agent.enabled = true;
        }
        #endregion -------------------------------------------------------------------------------------------------------------------
    }
}
