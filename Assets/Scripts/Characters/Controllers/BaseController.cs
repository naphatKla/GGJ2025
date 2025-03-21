using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.Controllers
{
    /// <summary>
    /// Abstract base class for all character controllers (e.g. player, enemy).
    /// Coordinates input handling, movement, and skill execution systems.
    /// Designed to be extended by specific controller types.
    /// </summary>
    public abstract class BaseController : MonoBehaviour
    {
        #region Inspectors & Fields

        /// <summary>
        /// Reference to the movement system used to control character motion.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        public BaseMovementSystem movementSystem;

        /// <summary>
        /// Reference to the character's skill system, responsible for executing primary and secondary skills.
        /// Automatically initialized at runtime.
        /// </summary>
        [SerializeField] private SkillSystem skillSystem;

        /// <summary>
        /// Character input handler implementing <see cref="ICharacterInput"/>.
        /// Serialized using Odin to allow assignment of interface in the Inspector.
        /// Fallbacks to auto-fetching via <c>TryGetComponent</c> if not manually assigned.
        /// </summary>
        [OdinSerialize] private ICharacterInput _inputSystem;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Called on the first frame after the script is enabled.
        /// Initializes the skill system by passing this controller as its owner.
        /// </summary>
        private void Start()
        {
            skillSystem.Initialize(this);
        }

        /// <summary>
        /// Called when the GameObject becomes active.
        /// Automatically attempts to locate an input component if none is assigned,
        /// and subscribes to movement and skill input events.
        /// </summary>
        private void OnEnable()
        {
            if (_inputSystem == null)
            {
                if (!TryGetComponent(out _inputSystem))
                    Debug.Log("Error: Input Reader is required. Please add an Input Reader component.");
            }

            ToggleMovementInputController(true);
            ToggleSkillInputController(true);
        }

        /// <summary>
        /// Called when the GameObject becomes inactive.
        /// Unsubscribes from all input events to prevent unintended behavior.
        /// </summary>
        private void OnDisable()
        {
            ToggleMovementInputController(false);
            ToggleSkillInputController(false);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Subscribes or unsubscribes the character's movement input handler
        /// to the input system's movement event.
        /// </summary>
        /// <param name="isToggle">If true, subscribes to movement input; otherwise, unsubscribes.</param>
        public void ToggleMovementInputController(bool isToggle)
        {
            if (isToggle)
                _inputSystem.OnMove += movementSystem.TryMove;
            else
                _inputSystem.OnMove -= movementSystem.TryMove;
        }

        /// <summary>
        /// Subscribes or unsubscribes the character's skill input handlers
        /// to the input system's primary and secondary skill events.
        /// </summary>
        /// <param name="isToggle">If true, subscribes to skill input; otherwise, unsubscribes.</param>
        public void ToggleSkillInputController(bool isToggle)
        {
            if (isToggle)
            {
                _inputSystem.OnPrimarySkillPerform += skillSystem.PerformPrimarySkill;
                _inputSystem.OnSecondarySkillPerform += skillSystem.PerformSecondarySkill;
            }
            else
            {
                _inputSystem.OnPrimarySkillPerform -= skillSystem.PerformPrimarySkill;
                _inputSystem.OnSecondarySkillPerform -= skillSystem.PerformSecondarySkill;
            }
        }

        #endregion
    }
}
