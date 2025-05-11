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
        
        #region Methods
        protected override void OnSkillStart()
        {
            InitializedSkill();
            ResetCharacterClone(true);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            for (int i = 0; i < skillData.CloneAmount; i++)
            {
                float angle = i * 2 * Mathf.PI / skillData.CloneAmount;
                Vector2 directions = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 explosionPosition = (Vector2)_cloneObjectPool[i].transform.position + directions * skillData.ExplosionDistance;
                FollowTarget(_cloneObjectPool[i].transform, explosionPosition, skillData.ExplosionSpeed, cancelToken).Forget();
            }
            
            int skillDurationMillisecond = (int)(skillData.SkillDuration * 1000);
            await UniTask.Delay(skillDurationMillisecond, cancellationToken: cancelToken);

            for (int i = 0; i < _cloneObjectPool.Count; i++)
            {
                var clone = _cloneObjectPool[i];
                int lastIndex = _cloneObjectPool.Count - 1;
                
                if (i == lastIndex)
                {
                    await FollowTarget(clone.transform, transform, skillData.MergeInSpeed, cancelToken);
                    break;
                }
                
                FollowTarget(clone.transform, transform, skillData.MergeInSpeed, cancelToken).Forget();
                await UniTask.Delay(50, cancellationToken: cancelToken);
            }
        }

        protected override void OnSkillExit()
        {
            ResetCharacterClone(false);
        }
        
        private void InitializedSkill()
        {
            int amountToCreate = skillData.CloneAmount - _cloneObjectPool.Count;
            
            for (int i = 0; i < Mathf.Abs(amountToCreate); i++)
            {
                if (amountToCreate < 0)
                {
                    Destroy(_cloneObjectPool[i]);
                    _cloneObjectPool.RemoveAt(i);
                    continue;
                }
                
                GameObject cloneObj = CreateCharacterClone(gameObject);
                cloneObj.SetActive(false);
                cloneObj.transform.position = owner.transform.position;
                _cloneObjectPool.Add(cloneObj);
            }
        }

        private void ResetCharacterClone(bool isActive)
        {
            foreach (GameObject cloneObj in _cloneObjectPool)
            {
                cloneObj.transform.position = owner.transform.position;
                cloneObj.SetActive(isActive);
            }
        }
        
        private async UniTask FollowTarget(Transform follower, Transform target, float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            float t = 0f;
            Vector3 startPos = follower.position;

            // คำนวณ vector ทิศทางหลัก
            Vector3 mainDirection = (target.position - startPos).normalized;

            // หา vector แนวตั้งฉากเพื่อเบี่ยงเส้นทาง
            Vector3 perpendicular = Vector3.Cross(mainDirection, Vector3.forward).normalized;
            float noiseAmplitude = 10f; // ความสูงของคลื่น
            float frequency = 1f;        // ความถี่

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                t = Mathf.Clamp01(elapsed / duration);

                Vector3 basePosition = Vector3.Lerp(startPos, target.position, t);
                float offset = Mathf.Sin(t * Mathf.PI * frequency) * noiseAmplitude;

                follower.position = basePosition + perpendicular * offset;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            follower.position = target.position;
            follower.gameObject.SetActive(false);
        }
        
        private async UniTask FollowTarget(Transform follower, Vector2 targetPosition, float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            float t = 0f;
            Vector3 startPos = follower.position;
            Vector3 targetPos = targetPosition;

            // คำนวณ vector ทิศทางหลัก
            Vector3 mainDirection = (targetPos - startPos).normalized;

            // หา vector แนวตั้งฉากเพื่อเบี่ยงเส้นทาง
            Vector3 perpendicular = Vector3.Cross(mainDirection, Vector3.forward).normalized;
            float noiseAmplitude = 2f; // ความสูงของคลื่น
            float frequency = 1f;        // ความถี่

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                t = Mathf.Clamp01(elapsed / duration);

                Vector3 basePosition = Vector3.Lerp(startPos, targetPos, t);
                float offset = Mathf.Sin(t * Mathf.PI * frequency) * noiseAmplitude;

                follower.position = basePosition + perpendicular * offset;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            follower.position = targetPos;
            //follower.gameObject.SetActive(false);
        }

        private GameObject CreateCharacterClone(GameObject original)
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
