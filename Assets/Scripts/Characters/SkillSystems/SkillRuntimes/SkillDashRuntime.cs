using System.Collections;
using Characters.SO.SkillDataSo;
using DG.Tweening;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime logic for the dash skill.
    /// Moves the character quickly in the aimed direction for a short duration and distance.
    /// Uses tweening for smooth motion and temporarily disables player input during execution.
    /// </summary>
    public class SkillDashRuntime : BaseSkillRuntime<SkillDashDataSo>
    {
        #region Methods

        /// <summary>
        /// Called at the beginning of the dash skill.
        /// Stops movement and disables player input to prevent interference during dash.
        /// </summary>
        protected override void OnSkillStart()
        {
            owner.MovementSystem.StopMovement();
            owner.ToggleMovementInputController(false);
            owner.MovementSystem.ResetSpeedToDefault();
        }

        /// <summary>
        /// Executes the dash movement by moving toward the calculated dash position.
        /// Waits for the tween to complete before finishing the skill.
        /// </summary>
        /// <returns>A coroutine that yields until the dash movement completes.</returns>
        protected override IEnumerator OnSkillUpdate()
        {
            Vector2 dashPosition = (Vector2)transform.position + aimDirection * skillData.DashDistance;

            yield return owner.MovementSystem
                .TryMoveToPositionOverTime(dashPosition, skillData.DashDuration)
                .WaitForCompletion();
        }

        /// <summary>
        /// Called after the dash skill finishes.
        /// Re-enables movement input so the player can move again.
        /// </summary>
        protected override void OnSkillExit()
        {
            owner.ToggleMovementInputController(true);
        }

        #endregion
    }
}