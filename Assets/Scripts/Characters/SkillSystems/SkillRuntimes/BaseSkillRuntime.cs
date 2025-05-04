using System.Collections;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using ProjectExtensions;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Abstract base class for all skill runtime behaviours.
    /// Provides a unified interface for executing skills during gameplay.
    /// </summary>
    public abstract class BaseSkillRuntime : MonoBehaviour
    {
        /// <summary>
        /// Executes the skill using the provided owner and input direction.
        /// Should be overridden in a generic subclass to implement full behavior.
        /// </summary>
        /// <param name="owner">The character who is performing the skill.</param>
        /// <param name="direction">The direction in which the skill is being cast.</param>
        public abstract void PerformSkill(BaseController owner, Vector2 direction);
    }

    /// <summary>
    /// Generic base class for skill runtime execution, parameterized by its associated skill data type.
    /// Encapsulates lifecycle logic such as skill start, update (coroutine), and exit phases.
    /// </summary>
    /// <typeparam name="T">The specific skill data type (inherited from <see cref="BaseSkillDataSo"/>).</typeparam>
    public abstract class BaseSkillRuntime<T> : BaseSkillRuntime where T : BaseSkillDataSo
    {
        #region Inspector & Variables

        /// <summary>
        /// The skill data containing configuration values for this runtime behavior.
        /// </summary>
        protected T skillData;

        /// <summary>
        /// The controller that owns and performs this skill (e.g., player or enemy).
        /// </summary>
        protected BaseController owner;

        /// <summary>
        /// The direction in which the skill should be executed (e.g., toward target).
        /// </summary>
        protected Vector2 aimDirection;

        /// <summary>
        /// Indicates whether this skill is currently active or executing.
        /// Prevents the skill from being retriggered while already in use.
        /// </summary>
        private bool _isPerforming;

        #endregion

        #region Methods

        /// <summary>
        /// Injects the skill data into this runtime behavior.
        /// Called during skill initialization.
        /// </summary>
        /// <param name="skillData">The data asset associated with this runtime skill.</param>
        public void AssignSkillData(T skillData)
        {
            this.skillData = skillData;
        }

        /// <summary>
        /// Begins the full execution flow of the skill:
        /// 1. Assigns owner and direction
        /// 2. Starts the skill (OnSkillStart)
        /// 3. Executes coroutine (OnSkillUpdate)
        /// 4. Cleans up (OnSkillExit)
        /// </summary>
        /// <param name="owner">The character who is casting this skill.</param>
        /// <param name="direction">The direction to aim or move during the skill.</param>
        public override void PerformSkill(BaseController owner, Vector2 direction)
        {
            if (_isPerforming) return;
            this.owner = owner;
            aimDirection = direction;

            HandleSkillStart();
            StartCoroutine(OnSkillUpdate().WithCallback(HandleSkillExit));
        }

        /// <summary>
        /// Handles pre-execution logic and sets the skill as currently performing.
        /// Triggers the OnSkillStart phase.
        /// </summary>
        protected virtual void HandleSkillStart()
        {
            _isPerforming = true;
            OnSkillStart();
        }

        /// <summary>
        /// Handles post-execution logic and resets the performing flag.
        /// Triggers the OnSkillExit phase.
        /// </summary>
        protected virtual void HandleSkillExit()
        {
            _isPerforming = false;
            OnSkillExit();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Called immediately before the skill logic begins.
        /// Use this to play animations, disable input, etc.
        /// </summary>
        protected abstract void OnSkillStart();

        /// <summary>
        /// Coroutine containing the main execution logic for the skill (e.g., dashing, charging).
        /// Can yield over time or wait for conditions.
        /// </summary>
        /// <returns>Coroutine IEnumerator controlling skill duration or logic.</returns>
        protected abstract IEnumerator OnSkillUpdate();

        /// <summary>
        /// Called once the skill finishes execution.
        /// Use this for re-enabling control or stopping effects.
        /// </summary>
        protected abstract void OnSkillExit();

        #endregion
    }
}
