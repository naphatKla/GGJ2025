using System.Collections;
using Characters.Controllers;
using ProjectExtensions;
using UnityEngine;

namespace Characters.SkillSystems.SkillS
{
    /// <summary>
    /// Base class for all skills that can be performed by characters.
    /// Handles the skill execution flow including start, update (runtime logic), and exit.
    /// Prevents re-execution while a skill is already performing.
    /// </summary>
    public abstract class BaseSkill : MonoBehaviour
    {
        #region Inspectors & Variables

        /// <summary>
        /// The cooldown duration of the skill after use.
        /// </summary>
        public float cooldownDuration;

        /// <summary>
        /// The owner of the skill. Usually the character or entity casting the skill.
        /// </summary>
        protected BaseController owner;

        /// <summary>
        /// The aiming direction in which the skill is performed.
        /// </summary>
        protected Vector2 aimDirection;

        /// <summary>
        /// Flag to indicate whether the skill is currently being performed.
        /// Prevents skill spamming or reactivation during its execution.
        /// </summary>
        private bool _isPerforming;

        #endregion

        #region Methods

        /// <summary>
        /// Starts the skill execution process if it's not already performing.
        /// It triggers the start phase, runs the update coroutine, and finally calls the exit phase.
        /// </summary>
        /// <param name="owner">The entity performing the skill.</param>
        /// <param name="direction">The direction in which the skill is aimed or cast.</param>
        public void PerformSkill(BaseController owner, Vector2 direction)
        {
            if (_isPerforming) return;
            this.owner = owner;
            aimDirection = direction;

            HandleSkillStart();
            StartCoroutine(OnSkillUpdate().WithCallback(HandleSkillExit));
        }

        /// <summary>
        /// Internally handles the beginning of the skill execution and marks it as active.
        /// Calls the abstract method <see cref="OnSkillStart"/> implemented in derived classes.
        /// </summary>
        protected virtual void HandleSkillStart()
        {
            _isPerforming = true;
            OnSkillStart();
        }

        /// <summary>
        /// Internally handles the end of the skill execution and resets the performing flag.
        /// Calls the abstract method <see cref="OnSkillExit"/> implemented in derived classes.
        /// </summary>
        protected virtual void HandleSkillExit()
        {
            _isPerforming = false;
            OnSkillExit();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Called when the skill starts. To be implemented by derived skill classes.
        /// This is where you initialize animations, effects, or logic before execution.
        /// </summary>
        protected abstract void OnSkillStart();

        /// <summary>
        /// Coroutine that contains the main logic of the skill execution.
        /// Runs during the skill's active phase and allows for time-based behavior.
        /// </summary>
        /// <returns>An IEnumerator used as a coroutine.</returns>
        protected abstract IEnumerator OnSkillUpdate();

        /// <summary>
        /// Called when the skill ends. Used for cleanup or resetting states.
        /// </summary>
        protected abstract void OnSkillExit();

        #endregion
    }
}