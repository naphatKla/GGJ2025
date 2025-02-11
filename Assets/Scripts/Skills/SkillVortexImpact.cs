using System.Collections;
using System.Collections.Generic;
using Characters;
using DG.Tweening;
using UnityEngine;

namespace Skills
{
    public class SkillVortexImpact : SkillBase
    {
        #region Inspectors & Fields
        
        [SerializeField] protected int cloningAmount = 5;
        [SerializeField] protected float skillDuration = 5f;
        [SerializeField] protected float minSpeed = 1f;
        [SerializeField] protected float maxSpeed = 10f;
        [SerializeField] protected float startRadius = 1f;
        [SerializeField] protected float finalRadius = 10f;
        [SerializeField] protected bool iframeOnPerformingSkill;
        [SerializeField] protected bool cloningIframeOnPerformingSkill;
        [SerializeField] protected bool cloningDealDamageOnTouch;
        [SerializeField] protected bool cloningDestroyAfterTouch;

        private List<CloningCharacter> activeClones = new List<CloningCharacter>();
        private bool isSkillActive = false;

        #endregion
        
        private IEnumerator ActivateVortex()
        {
            OwnerCharacter.IsIframe = iframeOnPerformingSkill;
            isSkillActive = true;
            float elapsedTime = 0f;

            // สร้าง Cloning
            for (int i = 0; i < cloningAmount; i++)
            {
                float angle = i * 360f / cloningAmount;
                Vector2 spawnPos = (Vector2)transform.position + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * startRadius;
                
                CloningCharacter clone = OwnerCharacter.CreateCloning(0, CloningCharacter.LifeTimeType.MergeBack, 1, cloningDealDamageOnTouch, cloningDestroyAfterTouch);
                clone.IsIframe = cloningIframeOnPerformingSkill;
                clone.transform.position = spawnPos;

                activeClones.Add(clone);
            }

            while (elapsedTime < skillDuration)
            {
                elapsedTime += Time.deltaTime;
                float speed = Mathf.Lerp(minSpeed, maxSpeed, elapsedTime / skillDuration);
                float radius = Mathf.Lerp(startRadius, finalRadius, elapsedTime / skillDuration);

                for (int i = 0; i < activeClones.Count; i++)
                {
                    if (activeClones[i] == null) continue;

                    float angle = (elapsedTime * speed * 360f / skillDuration) + (i * 360f / cloningAmount);
                    Vector2 newPos = (Vector2)transform.position + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                    
                    activeClones[i].transform.DOMove(newPos, 0.1f).SetEase(Ease.Linear);
                }

                yield return null;
            }

            ExitSkill();
        }
        
        #region Methods

        protected override void OnSkillStart()
        {
            OwnerCharacter.IsIframe = iframeOnPerformingSkill;
            OwnerCharacter.CanUseSkill = false;
            StartCoroutine(ActivateVortex());
        }

        protected override void OnSkillEnd()
        {
            OwnerCharacter.IsIframe = false;
            isSkillActive = false;
            OwnerCharacter.CanUseSkill = true;
            
        }

        protected override void ExitSkill()
        {
            if (!isSkillActive) return;
            isSkillActive = false;

            base.ExitSkill();
        }

        #endregion
    }
}



