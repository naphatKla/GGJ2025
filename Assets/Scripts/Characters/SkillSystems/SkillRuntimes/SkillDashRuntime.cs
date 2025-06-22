using System;
using System.Collections.Generic;
using System.Threading;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime implementation of a dash skill.
    /// Executes a quick burst movement in the aimed direction using DOTween,
    /// while temporarily disabling movement input and respecting skill lifetime and cancellation.
    /// </summary>
    public class SkillDashRuntime : BaseSkillRuntime<SkillDashDataSo>
    {
        private float _dashEffective;
        
        #region Methods
        
        protected override void OnSkillStart()
        {
            owner.TryPlayFeedback(FeedbackName.Dash);
            _dashEffective = 1f;

            if (!skillData.IsFlexibleDash) return;
            _dashEffective = Mathf.Clamp(aimDirection.length / skillData.MaxInputLength, skillData.MinMaxEffective.x,
                skillData.MinMaxEffective.y);

            if (Math.Abs(_dashEffective - 1) < 0.0001f) return;
            if (!skillData.IsFlexibleStatusEffectDuration) return;

            foreach (var statusEffectDataPayload in effectsApplyOnStart)
            {
                float calculatedEffectDuration = statusEffectDataPayload.OverrideDuration * _dashEffective;
                statusEffectDataPayload.SetOverrideDuration(calculatedEffectDuration);
            }
        }

        /// <summary>
        /// Performs the dash by moving the character toward a target position over time.
        /// Supports cancellation via token, allowing skill interruption if required.
        /// </summary>
        /// <param name="cancelToken">Token used to cancel the dash early (e.g., on interrupt or skill timeout).</param>
        /// <returns>A UniTask that completes when the dash tween ends or is cancelled.</returns>
        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            float calculatedDistance = skillData.DashDistance * _dashEffective;
            float calculatedDuration = skillData.DashDuration * _dashEffective;

            Vector2 dashPosition = (Vector2)transform.position + aimDirection.direction * calculatedDistance;
            await owner.MovementSystem
                .TryMoveToPositionOverTime(dashPosition, calculatedDuration, skillData.DashEaseCurve,
                    skillData.DashMoveCurve).WithCancellation(cancelToken);
        }

        protected override void OnSkillExit()
        {
            effectsApplyOnStart = new List<StatusEffectDataPayload>(skillData.StatusEffectOnSkillStart);
        }

        #endregion
    }
}