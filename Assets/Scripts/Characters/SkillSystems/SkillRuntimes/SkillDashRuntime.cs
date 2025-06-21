using System.Threading;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
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
        #region Methods

        protected override void OnSkillStart()
        {
            owner.FeedbackSystem.PlayFeedback(FeedbackName.Dash);
        }

        /// <summary>
        /// Performs the dash by moving the character toward a target position over time.
        /// Supports cancellation via token, allowing skill interruption if required.
        /// </summary>
        /// <param name="cancelToken">Token used to cancel the dash early (e.g., on interrupt or skill timeout).</param>
        /// <returns>A UniTask that completes when the dash tween ends or is cancelled.</returns>
        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            Vector2 dashPosition = (Vector2)transform.position + aimDirection.direction * skillData.DashDistance;
            await owner.MovementSystem
                .TryMoveToPositionOverTime(dashPosition, skillData.DashDuration, skillData.DashEaseCurve,
                    skillData.DashMoveCurve).WithCancellation(cancelToken);
        }

        protected override void OnSkillExit()
        {
         
        }

        #endregion
    }
}