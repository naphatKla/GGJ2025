using Characters.CombatSystems;
using Characters.HeathSystems;
using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
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
        [SerializeField] private BaseMovementSystem movementSystem;

        /// <summary>
        /// Reference to the character's skill system, responsible for executing primary and secondary skills.
        /// Should be assigned via Inspector or at runtime, Automatically initialized
        /// </summary>
        [SerializeField] private SkillSystem skillSystem;

        /// <summary>
        /// Reference to the health system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] private HealthSystem healthSystem;

        /// <summary>
        /// Reference to the Damage on touch system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] private DamageOnTouch damageOnTouch;

        /// <summary>
        /// ScriptableObject containing base character stats used to initialize systems.
        /// </summary>
        [SerializeField] private CharacterDataSo characterData;

        /// <summary>
        /// Character input handler implementing <see cref="ICharacterInput"/>.
        /// Serialized using Odin to allow assignment of interface in the Inspector.
        /// Fallbacks to auto-fetching via <c>TryGetComponent</c> if not manually assigned.
        /// </summary>
        [OdinSerialize] private ICharacterInput _inputSystem;

        /// <summary>
        /// Reference to the movement system used to control character motion.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        public BaseMovementSystem MovementSystem => movementSystem;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Initializes core systems for the character on the first frame,
        /// using data provided from the assigned <see cref="characterData"/>.
        /// </summary>
        private void Start()
        {
            skillSystem.Initialize(this);
            movementSystem?.AssignMovementData(characterData.BaseSpeed);
            healthSystem?.AssignHealthData(characterData.MaxHealth);
            damageOnTouch?.AssignDamageOnTouchData(characterData.BaseDamage);
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
