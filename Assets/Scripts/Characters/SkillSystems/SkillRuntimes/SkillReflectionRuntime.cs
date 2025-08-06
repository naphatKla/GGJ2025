using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SkillSystems.SkillObjects;
using Characters.SO.SkillDataSo;
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
    public class SkillReflectionRuntime : BaseSkillRuntime<SkillReflectionDataSo>
    {
        #region Base Methods

        private List<ReflectionSkillObject> _skillObjects = new();
        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            PoolingManager.Instance.Create<ReflectionSkillObject>(this.skillData.ReflectionSkillObject.name, PoolingGroupName.SkillObject, 
                CreatePoolInstance);
        }

        /// <summary>
        /// Initializes and activates skill objects at the start of the skill.
        /// </summary>
        protected override void OnSkillStart()
        {
            _skillObjects.Clear();

            for (int i = 0; i < skillData.SkillObjectAmount; i++)
            {
                var skillObject = PoolingManager.Instance.Get<ReflectionSkillObject>(skillData.ReflectionSkillObject.name);
                skillObject.transform.position = owner.transform.position;
                skillObject.DamageOnTouch.EnableDamage(owner.gameObject, this, skillData.DamageHitPerSec);
                skillObject.gameObject.SetActive(true);
                _skillObjects.Add(skillObject);
            }
            
            owner.TryPlayFeedback(FeedbackName.Reflection);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            int skillObjectCount = skillData.SkillObjectAmount;
            
            // --- Phase 1: Explosion ---
            float explosionDuration = skillData.ExplosionEntireDuration;
            float explosionStagger = skillData.ExplosionStartDuration;

            // Step 1: Normalize curve weights
            float[] explosionWeights = new float[skillObjectCount];
            float explosionWeightSum = 0f;

            for (int i = 0; i < skillObjectCount; i++)
            {
                float t = i / (float)(skillObjectCount - 1);
                explosionWeights[i] = Mathf.Clamp01(skillData.ExplosionStartCurve.Evaluate(t));
                explosionWeightSum += explosionWeights[i];
            }

            // Step 2: Launch skill objects with staggered delay
            for (int i = 0; i < skillObjectCount; i++)
            {
                float angle = i * 2 * Mathf.PI / skillObjectCount;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 explosionPos = (Vector2)_skillObjects[i].transform.position +
                                       direction * skillData.ExplosionDistance;

                float delay = (explosionWeights[i] / explosionWeightSum) * explosionStagger;
                float actualMoveDuration = Mathf.Clamp(explosionDuration - delay, 0.05f, explosionDuration);
                explosionDuration -= delay;
                
                _skillObjects[i].MovementSystem.TryMoveToPositionOverTime(explosionPos, actualMoveDuration,
                    skillData.ExplosionEaseCurve, skillData.ExplosionMoveCurve);
                
                await UniTask.Delay((int)(delay * 1000), cancellationToken: cancelToken);
            }

            // Wait for the last skill object to finish
            await UniTask.Delay((int)(explosionDuration * 1000), cancellationToken: cancelToken);

            // --- Phase 2: Merge ---
            float mergeDuration = skillData.MergeEntireDuration;
            float mergeStagger = skillData.MergeStartDuration;

            float[] mergeWeights = new float[skillObjectCount];
            float mergeWeightSum = 0f;

            for (int i = 0; i < skillObjectCount; i++)
            {
                float t = i / (float)(skillObjectCount - 1);
                mergeWeights[i] = Mathf.Clamp01(skillData.MergeStartCurve.Evaluate(t));
                mergeWeightSum += mergeWeights[i];
            }

            for (int i = 0; i < skillObjectCount; i++)
            {
                var skillObject = _skillObjects[i];
          
                float delay = (mergeWeights[i] / mergeWeightSum) * mergeStagger;
                float actualMoveDuration = Mathf.Clamp(mergeDuration - delay, 0.05f, mergeDuration);
                mergeDuration -= delay;

                Tween tween = skillObject.MovementSystem.TryMoveToTargetOverTime(transform, actualMoveDuration,
                    skillData.MergeEaseCurve, skillData.MergeMoveCurve);

                tween.OnComplete(() => skillObject.gameObject.SetActive(false));

                await UniTask.Delay((int)(delay * 1000), cancellationToken: cancelToken);
            }

            await UniTask.Delay((int)(mergeDuration * 1000), cancellationToken: cancelToken);
        }

        protected override void OnSkillExit()
        {
            foreach (var skillObject in _skillObjects)
            {
                skillObject.DamageOnTouch.DisableDamage(this);
                skillObject.gameObject.SetActive(false);
                skillObject.transform.position = owner.transform.position;
                PoolingManager.Instance.Release(skillData.ReflectionSkillObject.name, skillObject);
            }
            
            _skillObjects.Clear();
        }

        #endregion
        
        // Pool life cycle
        private ReflectionSkillObject CreatePoolInstance()
        {
            ReflectionSkillObject skillObj = Instantiate(skillData.ReflectionSkillObject);
            skillObj.gameObject.SetActive(false);
            skillObj.transform.position = owner.transform.position;
            return skillObj;
        }
    }
}