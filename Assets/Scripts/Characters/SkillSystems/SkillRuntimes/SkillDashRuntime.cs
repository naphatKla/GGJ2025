using System.Threading;
using Characters.SO.SkillDataSo;
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
        #region Methods

        /// <summary>
        /// Called when the dash skill begins.
        /// Disables character input and resets speed to base before performing the dash.
        /// </summary>
        protected override void OnSkillStart()
        {
            owner.MovementSystem.StopMovement();
            owner.ToggleMovementInputController(false);
            owner.MovementSystem.ResetSpeedToDefault();
            owner.DamageOnTouch.EnableDamage(true);
        }

        /// <summary>
        /// Performs the dash by moving the character toward a target position over time.
        /// Supports cancellation via token, allowing skill interruption if required.
        /// </summary>
        /// <param name="cancelToken">Token used to cancel the dash early (e.g., on interrupt or skill timeout).</param>
        /// <returns>A UniTask that completes when the dash tween ends or is cancelled.</returns>
        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            Vector2 dashPosition = (Vector2)transform.position + aimDirection * skillData.DashDistance;
            await owner.MovementSystem
                .TryMoveToPositionOverTime(dashPosition, skillData.DashDuration, skillData.DashEaseCurve,
                    skillData.DashMoveCurve).WithCancellation(cancelToken);
        }

        /// <summary>
        /// Called when the dash completes or is cancelled.
        /// Re-enables movement input to return control to the character.
        /// </summary>
        protected override void OnSkillExit()
        {
            owner.ToggleMovementInputController(true);
            owner.DamageOnTouch.EnableDamage(false);
        }

        #endregion
    }
}