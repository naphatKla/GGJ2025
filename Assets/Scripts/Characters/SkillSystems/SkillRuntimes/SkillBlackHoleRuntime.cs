using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.CombatSystems;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.HeathSystems;
using Characters.MovementSystems;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
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
        private readonly List<BaseMovementSystem> _cloneObjectPool = new();
        private BaseMovementSystem _ownerStartPrefab;

        #region Base Methods

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            _ownerStartPrefab = CreateCharacterClone(gameObject);
            _cloneObjectPool.Add(_ownerStartPrefab);
        }

        /// <summary>
        /// Initializes and activates clone objects at the start of the skill.
        /// </summary>
        protected override void OnSkillStart()
        {
            InitializedSkill();
            ResetCharacterClone(true);
            owner.TryPlayFeedback(FeedbackName.BlackHole);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            int cloneCount = _cloneObjectPool.Count;

            // --- Phase 1: Explosion ---
            float explosionDuration = skillData.ExplosionEntireDuration;
            float explosionStagger = skillData.ExplosionStartDuration;

            // Step 1: Normalize curve weights
            float[] explosionWeights = new float[cloneCount];
            float explosionWeightSum = 0f;

            for (int i = 0; i < cloneCount; i++)
            {
                float t = i / (float)(cloneCount - 1);
                explosionWeights[i] = Mathf.Clamp01(skillData.ExplosionStartCurve.Evaluate(t));
                explosionWeightSum += explosionWeights[i];
            }

            // Step 2: Launch clones with staggered delay
            for (int i = 0; i < cloneCount; i++)
            {
                float angle = i * 2 * Mathf.PI / cloneCount;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 explosionPos = (Vector2)_cloneObjectPool[i].transform.position +
                                       direction * skillData.ExplosionDistance;

                float delay = (explosionWeights[i] / explosionWeightSum) * explosionStagger;
                float actualMoveDuration = Mathf.Clamp(explosionDuration - delay, 0.05f, explosionDuration);
                explosionDuration -= delay;

                var clone = _cloneObjectPool[i];
                clone.TryMoveToPositionOverTime(explosionPos, actualMoveDuration,
                    skillData.ExplosionEaseCurve, skillData.ExplosionMoveCurve);
         
                StatusEffectManager.ApplyEffectTo(clone.gameObject, skillData.CloneEffects);
                await UniTask.Delay((int)(delay * 1000), cancellationToken: cancelToken);
            }

            // Wait for the last clone to finish
            await UniTask.Delay((int)(explosionDuration * 1000), cancellationToken: cancelToken);

            // --- Phase 2: Merge ---
            float mergeDuration = skillData.MergeEntireDuration;
            float mergeStagger = skillData.MergeStartDuration;

            float[] mergeWeights = new float[cloneCount];
            float mergeWeightSum = 0f;

            for (int i = 0; i < cloneCount; i++)
            {
                float t = i / (float)(cloneCount - 1);
                mergeWeights[i] = Mathf.Clamp01(skillData.MergeStartCurve.Evaluate(t));
                mergeWeightSum += mergeWeights[i];
            }

            for (int i = 0; i < cloneCount; i++)
            {
                var clone = _cloneObjectPool[i];
          
                float delay = (mergeWeights[i] / mergeWeightSum) * mergeStagger;
                float actualMoveDuration = Mathf.Clamp(mergeDuration - delay, 0.05f, mergeDuration);
                mergeDuration -= delay;

                Tween tween = clone.TryMoveToTargetOverTime(transform, actualMoveDuration,
                    skillData.MergeEaseCurve, skillData.MergeMoveCurve);

                tween.OnComplete(() => clone.gameObject.SetActive(false));

                await UniTask.Delay((int)(delay * 1000), cancellationToken: cancelToken);
            }

            await UniTask.Delay((int)(mergeDuration * 1000), cancellationToken: cancelToken);
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
                    var clone = CreateCharacterClone(_ownerStartPrefab.gameObject);
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
        private BaseMovementSystem CreateCharacterClone(GameObject original)
        {
            GameObject clone = Instantiate(original);
            
            clone.TryGetComponent(out BaseController controller);
            if (!controller)
            {
                Debug.LogError("Cloning target not have character controller!");
                return null;
            }
            
            Type[] typeToKeep = new[]
            {
                typeof(BaseController),
                typeof(CombatSystem),
                typeof(StatusEffectSystem),
                typeof(DamageOnTouch),
                typeof(BaseMovementSystem),
                typeof(SpriteFollowMouse),
                typeof(HealthSystem)
            };

            foreach (var mono in clone.GetComponents<MonoBehaviour>())
            {
                Type compType = mono.GetType();
                bool shouldKeep = typeToKeep.Any(t => t.IsAssignableFrom(compType));
                if (shouldKeep) continue;
                Destroy(mono);
            }
            
            clone.gameObject.SetActive(false);
            return clone.GetComponent<BaseMovementSystem>();
        }
        
        #endregion
    }
}