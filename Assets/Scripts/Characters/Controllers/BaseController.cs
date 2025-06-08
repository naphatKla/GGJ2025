using System;
using System.Threading;
using Characters.CombatSystems;
using Characters.ComboSystems;
using Characters.FeedbackSystems;
using Characters.HeathSystems;
using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using Manager;
using Sirenix.OdinInspector;
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
        [Title("Dependents")] [SerializeField] private BaseMovementSystem movementSystem;

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
        /// Reference to the status effect system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] private StatusEffectSystem statusEffectSystem;

        /// <summary>
        /// Reference to the combat system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] private CombatSystem combatSystem;

        /// <summary>
        /// Reference to the Damage on touch system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] private DamageOnTouch damageOnTouch;

        /// <summary>
        /// Reference to the feedback system
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] private FeedbackSystem feedbackSystem;

        /// <summary>
        /// Reference to the combo system
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        [SerializeField] public ComboSystem comboSystem;

        /// <summary>
        /// ScriptableObject containing base character stats used to initialize systems.
        /// </summary>
        [Title("Data")] [SerializeField] private CharacterDataSo characterData;

        /// <summary>
        /// Character input handler implementing <see cref="ICharacterInput"/>.
        /// Serialized using Odin to allow assignment of interface in the Inspector.
        /// Fallbacks to auto-fetching via <c>TryGetComponent</c> if not manually assigned.
        /// </summary>
        [OdinSerialize] private ICharacterInput _inputSystem;

        /// <summary>
        /// Is this controller is initialize or not.
        /// </summary>
        private bool _isInitialize;

        /// <summary>
        /// Reference to the movement system used to control character motion.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        public BaseMovementSystem MovementSystem => movementSystem;

        /// <summary>
        /// Reference to the health system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        public HealthSystem HealthSystem => healthSystem;

        /// <summary>
        /// Reference to the Damage on touch system.
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        public DamageOnTouch DamageOnTouch => damageOnTouch;

        /// <summary>
        /// Reference to the feedback system
        /// Should be assigned via Inspector or at runtime.
        /// </summary>
        public FeedbackSystem FeedbackSystem => feedbackSystem;

        /// <summary>
        /// ScriptableObject containing base character stats used to initialize systems.
        /// </summary>
        public CharacterDataSo CharacterData => characterData;

        #endregion

        #region Unity Methods
        
        private void Start()
        {
            if (characterData && !_isInitialize)
                Initialize();
        }

        /// <summary>
        /// Called automatically by Unity when the object is first enabled.
        /// Waits for initialization before subscribing to input and combo events.
        /// </summary>
        private async void OnEnable()
        {
            await UniTask.WaitUntil(() => _isInitialize);
            if (this == null || gameObject == null || !gameObject.activeInHierarchy) return;
            if (!Application.isPlaying) return;

            _inputSystem ??= GetComponent<ICharacterInput>();

            ToggleMovementInputController(true);
            ToggleSkillInputController(true);

            if (comboSystem != null)
                combatSystem.OnDealDamage += comboSystem.RegisterHit;
        }

        /// <summary>
        /// Called automatically by Unity when the object is disabled.
        /// Waits for initialization before unsubscribing from input and combo events.
        /// </summary>
        private async void OnDisable()
        {
            await UniTask.WaitUntil(() => _isInitialize);

            ToggleMovementInputController(false);
            ToggleSkillInputController(false);

            if (comboSystem != null)
                combatSystem.OnDealDamage -= comboSystem.RegisterHit;
        }

        /// <summary>
        /// Called when a collision with another collider begins.
        /// Currently unused, but reserved for future logic such as hit reaction triggers or knockback.
        /// </summary>
        /// <param name="other">Collision data from Unity's physics engine.</param>
        private void OnCollisionEnter2D(Collision2D other)
        {
        }

        /// <summary>
        /// Called continuously while colliding with another collider.
        /// Applies contact damage and triggers bounce if applicable.
        /// </summary>
        private void OnCollisionStay2D(Collision2D other)
        {
            if (DamageOnTouch?.IsEnableDamage == true)
                CombatManager.ApplyDamageTo(other.gameObject, gameObject);

            if (other.gameObject && other.gameObject.activeSelf)
                movementSystem?.BounceHandler(other);
        }

        /// <summary>
        /// Called when the collision with another collider ends.
        /// Currently unused, but can be extended to handle exit-specific behavior (e.g., status cleanup).
        /// </summary>
        /// <param name="other">Collision data from Unity's physics engine.</param>
        private void OnCollisionExit2D(Collision2D other)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns dependencies and stat data to this character.
        /// Automatically initializes all major subsystems.
        /// </summary>
        public void AssignCharacterData(CharacterDataSo data,
            BaseMovementSystem assignMovementSystem = null,
            SkillSystem assignSkillSystem = null,
            HealthSystem assignHealthSystem = null,
            StatusEffectSystem assignStatusEffectSystem = null,
            CombatSystem assignCombatSystem = null,
            DamageOnTouch assignDamageOnTouch = null,
            FeedbackSystem assignFeedbackSystem = null,
            ComboSystem assignComboSystem = null,
            ICharacterInput inputSystem = null)
        {
            movementSystem      ??= assignMovementSystem;
            skillSystem         ??= assignSkillSystem;
            healthSystem        ??= assignHealthSystem;
            statusEffectSystem  ??= assignStatusEffectSystem;
            combatSystem        ??= assignCombatSystem;
            damageOnTouch       ??= assignDamageOnTouch;
            feedbackSystem      ??= assignFeedbackSystem;
            comboSystem         ??= assignComboSystem;
            _inputSystem        ??= inputSystem;

            characterData = data;
            Initialize();
        }

        /// <summary>
        /// Initializes all assigned systems with stat data.
        /// Marks the controller as initialized.
        /// </summary>
        private void Initialize()
        {
            skillSystem?.Initialize(this);

            movementSystem?.AssignMovementData(
                characterData.BaseSpeed,
                characterData.MoveAccelerationRate,
                characterData.TurnAccelerationRate,
                characterData.BounceMultiplier,
                characterData.EffectOnBounce);

            healthSystem?.AssignHealthData(
                characterData.MaxHealth,
                characterData.InvincibleTimePerHit);

            combatSystem?.AssignCombatData(characterData.BaseDamage);

            _isInitialize = true;
        }

        /// <summary>
        /// Subscribes or unsubscribes the character's movement input handler
        /// to the input system's movement event.
        /// </summary>
        /// <param name="isToggle">If true, subscribes to movement input; otherwise, unsubscribes.</param>
        public void ToggleMovementInputController(bool isToggle)
        {
            if (_inputSystem == null) return;
            if (isToggle)
                _inputSystem.OnMove += movementSystem.AssignInputDirection;
            else
                _inputSystem.OnMove -= movementSystem.AssignInputDirection;
        }

        /// <summary>
        /// Subscribes or unsubscribes the character's skill input handlers
        /// to the input system's primary and secondary skill events.
        /// </summary>
        /// <param name="isToggle">If true, subscribes to skill input; otherwise, unsubscribes.</param>
        public void ToggleSkillInputController(bool isToggle)
        {
            if (_inputSystem == null) return;
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

        /// <summary>
        /// Resets the damage-on-touch system by disabling its damage output.
        /// Commonly used during respawn, state reset, or when temporarily disabling attack interactions.
        /// </summary>
        public void ResetAllDependentBehavior()
        {
            movementSystem?.ResetMovementSystem();
            skillSystem?.ResetSkillSystem();
            healthSystem?.ResetHealthSystem();
            statusEffectSystem?.ResetStatusEffectSystem();
            damageOnTouch?.ResetDamageOnTouch();
        }

        #endregion
    }
}