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
    /// A skill that creates multiple clone objects and performs an "explosion and reabsorb" motion.
    /// Clones are scattered outward from the caster, stay for a period, and return back to the caster.
    /// Useful for visual-heavy skills like black holes or illusions.
    /// </summary>
    public class SkillBlackHoleRuntime : BaseSkillRuntime<SkillBlackHoleDataSo>
    {
        /// <summary>
        /// Pool of reusable clone objects to reduce runtime instantiation costs.
        /// Each clone uses RigidbodyMovementSystem for movement control.
        /// </summary>
        private readonly List<RigidbodyMovementSystem> _cloneObjectPool = new();

        #region Base Methods

        /// <summary>
        /// Called at the beginning of the skill.
        /// Prepares and activates clones at the caster's position.
        /// </summary>
        protected override void OnSkillStart()
        {
            InitializedSkill();
            ResetCharacterClone(true);
        }

        /// <summary>
        /// Main update logic for the skill.
        /// Clones move outward, wait, then return to the caster with animation curves.
        /// </summary>
        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            // Phase 1: Explosion (outward movement)
            for (int i = 0; i < skillData.CloneAmount; i++)
            {
                float angle = i * 2 * Mathf.PI / skillData.CloneAmount;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 explosionPos = (Vector2)_cloneObjectPool[i].transform.position + direction * skillData.ExplosionDistance;

                _cloneObjectPool[i].TryMoveToPositionOverTime(explosionPos, skillData.ExplosionSpeed, moveCurve: skillData.ExplosionCurve);
            }

            // Wait for skill duration
            await UniTask.Delay((int)(skillData.SkillDuration * 1000), cancellationToken: cancelToken);

            // Phase 2: Return to caster
            for (int i = 0; i < _cloneObjectPool.Count; i++)
            {
                var clone = _cloneObjectPool[i];
                int lastIndex = _cloneObjectPool.Count - 1;

                if (i == lastIndex)
                {
                    await clone.TryMoveToTargetOverTime(transform, skillData.MergeInSpeed, moveCurve: skillData.MergeInCurve);
                    break;
                }

                clone.TryMoveToTargetOverTime(transform, skillData.MergeInSpeed, moveCurve: skillData.MergeInCurve)
                    .OnComplete(() => clone.gameObject.SetActive(false));

                await UniTask.Delay(50, cancellationToken: cancelToken);
            }
        }

        /// <summary>
        /// Called when the skill ends.
        /// Hides all clone objects and resets their position.
        /// </summary>
        protected override void OnSkillExit()
        {
            ResetCharacterClone(false);
        }

        #endregion

        #region Clone Management

        /// <summary>
        /// Ensures the object pool has enough clones based on skillData.CloneAmount.
        /// Destroys or creates as needed.
        /// </summary>
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

                var clone = CreateCharacterClone(gameObject);
                clone.transform.position = owner.transform.position;
                clone.gameObject.SetActive(false);
                _cloneObjectPool.Add(clone);
            }
        }

        /// <summary>
        /// Resets all clones to the caster's position and sets their active state.
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
        /// Creates a new clone GameObject that copies key components from the original GameObject.
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
        /// Copies the SpriteRenderer component from the original to the clone.
        /// </summary>
        /// <param name="original">The source GameObject.</param>
        /// <param name="clone">The target clone GameObject.</param>
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
        /// Copies the Animator component from the original to the clone.
        /// </summary>
        /// <param name="original">The source GameObject.</param>
        /// <param name="clone">The target clone GameObject.</param>
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
        /// Copies Collider2D component settings from the original to the clone.
        /// Supports BoxCollider2D and CircleCollider2D.
        /// </summary>
        /// <param name="original">The source GameObject.</param>
        /// <param name="clone">The target clone GameObject.</param>
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
        /// Copies Rigidbody2D settings from the original to the clone.
        /// Sets defaults if the original has no Rigidbody2D.
        /// </summary>
        /// <param name="original">The source GameObject.</param>
        /// <param name="clone">The target clone GameObject.</param>
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
