using System;
using System.Collections.Generic;
using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillBlackHoleRuntime : BaseSkillRuntime<SkillBlackHoleDataSo>
    {
        private List<GameObject> _cloneObjectPool = new List<GameObject>();
        
        #region Unity Methods

        private async void Start()
        {
            await UniTask.Delay(1000);
            for (int i = 0; i < skillData.CloneAmount; i++)
            {
                GameObject cloneObj = CreateCharacterClone(gameObject);
                cloneObj.SetActive(false);
                _cloneObjectPool.Add(cloneObj);
            }
        }

        #endregion
        
        #region Methods
        protected override void OnSkillStart()
        {
            foreach (GameObject cloneObj in _cloneObjectPool)
            {
                cloneObj.transform.position = owner.transform.position;
                cloneObj.SetActive(true);
            }
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.Delay(1000, cancellationToken: cancelToken);
        }

        protected override void OnSkillExit()
        {
            foreach (GameObject cloneObj in _cloneObjectPool)
            {
                cloneObj.transform.position = owner.transform.position;
                cloneObj.SetActive(false);
            }
        }

        public static GameObject CreateCharacterClone(GameObject original)
        {
            GameObject clone = new GameObject("CharacterClone");

            // Copy Transform
            clone.transform.position = original.transform.position;
            clone.transform.rotation = original.transform.rotation;
            clone.transform.localScale = original.transform.localScale;

            // Copy SpriteRenderer
            SpriteRenderer originalSprite = original.GetComponent<SpriteRenderer>();
            if (originalSprite != null)
            {
                SpriteRenderer cloneSprite = clone.AddComponent<SpriteRenderer>();
                cloneSprite.sprite = originalSprite.sprite;
                cloneSprite.color = originalSprite.color;
                cloneSprite.flipX = originalSprite.flipX;
                cloneSprite.flipY = originalSprite.flipY;
                cloneSprite.sortingLayerID = originalSprite.sortingLayerID;
                cloneSprite.sortingOrder = originalSprite.sortingOrder;
            }

            // Copy Animator
            Animator originalAnimator = original.GetComponent<Animator>();
            if (originalAnimator != null)
            {
                Animator cloneAnimator = clone.AddComponent<Animator>();
                cloneAnimator.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;
                cloneAnimator.avatar = originalAnimator.avatar;
                cloneAnimator.applyRootMotion = originalAnimator.applyRootMotion;
                cloneAnimator.updateMode = originalAnimator.updateMode;
                cloneAnimator.cullingMode = originalAnimator.cullingMode;
            }

            // Copy Collider2D 
            Collider2D originalCollider = original.GetComponent<Collider2D>();
            if (originalCollider != null)
            {
                Type colliderType = originalCollider.GetType();
                Collider2D cloneCollider = (Collider2D)clone.AddComponent(colliderType);

                // Copy common properties
                if (originalCollider is BoxCollider2D oBox && cloneCollider is BoxCollider2D cBox)
                {
                    cBox.offset = oBox.offset;
                    cBox.size = oBox.size;
                    cBox.isTrigger = oBox.isTrigger;
                }
                else if (originalCollider is CircleCollider2D oCircle && cloneCollider is CircleCollider2D cCircle)
                {
                    cCircle.offset = oCircle.offset;
                    cCircle.radius = oCircle.radius;
                    cCircle.isTrigger = oCircle.isTrigger;
                }
            }

            return clone;
        }
        
        #endregion
    }
}
