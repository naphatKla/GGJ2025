using System.Collections.Generic;
using System.Threading;
using Characters.MovementSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Executes a black hole skill by creating multiple clones that explode outward and then return to the caster.
    /// Useful for visual-heavy effects like illusions or area disruption.
    /// </summary>
    public class SkillBlackHoleRuntime : BaseSkillRuntime<SkillBlackHoleDataSo>
    {
        /// <summary>
        /// Object pool for clone instances to reduce instantiation overhead.
        /// </summary>
        private readonly List<RigidbodyMovementSystem> _cloneObjectPool = new();

        #region Base Methods

        /// <summary>
        /// Initializes and activates clone objects at the start of the skill.
        /// </summary>
        protected override void OnSkillStart()
        {
            InitializedSkill();
            ResetCharacterClone(true);
        }

        /// <summary>
        /// Handles the two-phase skill sequence: outward explosion and reabsorption.
        /// </summary>
        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            // Phase 1: Explosion
            for (int i = 0; i < skillData.CloneAmount; i++)
            {
                float angle = i * 2 * Mathf.PI / skillData.CloneAmount;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 explosionPos = (Vector2)_cloneObjectPool[i].transform.position + direction * skillData.ExplosionDistance;

                var clone = _cloneObjectPool[i];
                Tween tween = clone.TryMoveToPositionOverTime(explosionPos, skillData.ExplosionDuration,
                    skillData.ExplosionEaseCurve, skillData.ExplosionMoveCurve);

                if (i == skillData.CloneAmount - 1)
                    await tween.AsyncWaitForCompletion();
            }

            // Phase 2: Merge
            for (int i = 0; i < _cloneObjectPool.Count; i++)
            {
                var clone = _cloneObjectPool[i];
                Tween tween = clone.TryMoveToTargetOverTime(transform, skillData.MergeDuration,
                    skillData.MergeEaseCurve, skillData.MergeMoveCurve);

                if (i == _cloneObjectPool.Count - 1)
                {
                    await tween.AsyncWaitForCompletion();
                }
                else
                {
                    tween.OnComplete(() => clone.gameObject.SetActive(false));
                    await UniTask.Delay(50, cancellationToken: cancelToken);
                }
            }
        }

        /// <summary>
        /// Resets clone states when the skill ends.
        /// </summary>
        protected override void OnSkillExit()
        {
            ResetCharacterClone(false);
        }

        #endregion

        #region Clone Management

        /// <summary>
        /// Ensures the pool matches the desired clone count by adding or removing clones as needed.
        /// </summary>
        private void InitializedSkill()
        {
            int difference = skillData.CloneAmount - _cloneObjectPool.Count;

            if (difference > 0)
            {
                for (int i = 0; i < difference; i++)
                {
                    var clone = CreateCharacterClone(gameObject);
                    clone.transform.position = owner.transform.position;
                    clone.gameObject.SetActive(false);
                    _cloneObjectPool.Add(clone);
                }
            }
            else if (difference < 0)
            {
                for (int i = 0; i < -difference; i++)
                {
                    Destroy(_cloneObjectPool[^1]);
                    _cloneObjectPool.RemoveAt(_cloneObjectPool.Count - 1);
                }
            }
        }

        /// <summary>
        /// Sets all clones to the caster's position and toggles their active state.
        /// </summary>
        private void ResetCharacterClone(bool isActive)
        {
            foreach (var clone in _cloneObjectPool)
            {
                clone.transform.position = owner.transform.position;
                clone.gameObject.SetActive(isActive);
            }
        }

        #endregion

        #region Clone Copy Methods

        /// <summary>
        /// Instantiates a clone object and copies relevant components from the original GameObject.
        /// </summary>
        private RigidbodyMovementSystem CreateCharacterClone(GameObject original)
        {
            GameObject clone = new("CharacterClone")
            {
                transform =
                {
                    position = original.transform.position,
                    rotation = original.transform.rotation,
                    localScale = original.transform.localScale
                }
            };

            CopySpriteRenderer(original, clone);
            CopyAnimator(original, clone);
            CopyCollider2D(original, clone);
            CopyRigidbody2D(original, clone);

            return clone.AddComponent<RigidbodyMovementSystem>();
        }

        /// <summary>
        /// Duplicates the SpriteRenderer component.
        /// </summary>
        private void CopySpriteRenderer(GameObject original, GameObject clone)
        {
            var src = original.GetComponent<SpriteRenderer>();
            if (!src) return;

            var dst = clone.AddComponent<SpriteRenderer>();
            dst.sprite = src.sprite;
            dst.color = src.color;
            dst.flipX = src.flipX;
            dst.flipY = src.flipY;
            dst.sortingLayerID = src.sortingLayerID;
            dst.sortingOrder = src.sortingOrder;
        }

        /// <summary>
        /// Duplicates the Animator component.
        /// </summary>
        private void CopyAnimator(GameObject original, GameObject clone)
        {
            var src = original.GetComponent<Animator>();
            if (!src) return;

            var dst = clone.AddComponent<Animator>();
            dst.runtimeAnimatorController = src.runtimeAnimatorController;
            dst.avatar = src.avatar;
            dst.applyRootMotion = src.applyRootMotion;
            dst.updateMode = src.updateMode;
            dst.cullingMode = src.cullingMode;
        }

        /// <summary>
        /// Copies supported Collider2D settings to the clone.
        /// </summary>
        private void CopyCollider2D(GameObject original, GameObject clone)
        {
            var src = original.GetComponent<Collider2D>();
            if (!src) return;

            var dst = (Collider2D)clone.AddComponent(src.GetType());

            switch (src)
            {
                case BoxCollider2D oBox when dst is BoxCollider2D cBox:
                    cBox.offset = oBox.offset;
                    cBox.size = oBox.size;
                    cBox.isTrigger = oBox.isTrigger;
                    break;

                case CircleCollider2D oCircle when dst is CircleCollider2D cCircle:
                    cCircle.offset = oCircle.offset;
                    cCircle.radius = oCircle.radius;
                    cCircle.isTrigger = oCircle.isTrigger;
                    break;
            }
        }

        /// <summary>
        /// Duplicates Rigidbody2D settings, applying defaults if missing.
        /// </summary>
        private void CopyRigidbody2D(GameObject original, GameObject clone)
        {
            var src = original.GetComponent<Rigidbody2D>();
            var dst = clone.AddComponent<Rigidbody2D>();

            if (!src)
            {
                dst.gravityScale = 0f;
                dst.constraints = RigidbodyConstraints2D.FreezeRotation;
                return;
            }

            dst.bodyType = src.bodyType;
            dst.mass = src.mass;
            dst.drag = src.drag;
            dst.angularDrag = src.angularDrag;
            dst.gravityScale = src.gravityScale;
            dst.interpolation = src.interpolation;
            dst.collisionDetectionMode = src.collisionDetectionMode;
            dst.constraints = src.constraints;
        }

        #endregion
    }
}
