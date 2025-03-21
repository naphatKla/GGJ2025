using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    /// <summary>
    /// A dash skill that moves the character quickly in a given direction over a short distance.
    /// Disables movement input during the dash and re-enables it after completion.
    /// </summary>
    public class SkillDashNew : BaseSkill
    {
        #region Inspector & Variables

        /// <summary>
        /// The duration (in seconds) of the dash movement.
        /// </summary>
        [Title("SkillDash")] [SerializeField] private float dashDuration = 0.3f;

        /// <summary>
        /// The total distance the character will dash.
        /// </summary>
        [SerializeField] private float dashDistance = 8f;

        #endregion

        #region Methods

        /// <summary>
        /// Called at the start of the dash skill. Disables player movement input.
        /// </summary>
        protected override void OnSkillStart()
        {
            owner.ToggleMovementInputController(false);
        }

        /// <summary>
        /// Executes the dash movement using the movement system and waits for it to complete.
        /// </summary>
        /// <returns>Coroutine that yields until dash is completed.</returns>
        protected override IEnumerator OnSkillUpdate()
        {
            Vector2 dashPosition = (Vector2)transform.position + aimDirection * dashDistance;

            // Start dash movement and wait until finished (via Tween)
            yield return owner.movementSystem
                .TryMoveToPositionOverTime(dashPosition, dashDuration)
                .WaitForCompletion();
        }

        /// <summary>
        /// Called after the dash ends. Re-enables movement input.
        /// </summary>
        protected override void OnSkillExit()
        {
            owner.ToggleMovementInputController(true);
        }

        #endregion
    }
}