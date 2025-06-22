using Characters.CombatSystems;
using Characters.ComboSystems;
using Characters.FeedbackSystems;
using Characters.HeathSystems;
using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
using Characters.StatusEffectSystems;
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
        /// Sprite body of this character.
        /// </summary>
        [SerializeField] private SpriteRenderer body;
        
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

        public SpriteRenderer Body => body;

        
        public ICharacterInput InputSystem => _inputSystem;
        
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
                AssignCharacterData(characterData);
            
            SubscribeDependency();
        }

        private void OnDestroy()
        {
            UnSubscribeDependency();
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
            InitializeData();
        }

        /// <summary>
        /// Initializes all assigned systems with stat data.
        /// Marks the controller as initialized.
        /// </summary>
        private void InitializeData()
        {
            if (skillSystem)
                skillSystem.Initialize(this);
            
            if (movementSystem)
            {
                movementSystem.AssignMovementData(
                    characterData.BaseSpeed,
                    characterData.MoveAccelerationRate,
                    characterData.TurnAccelerationRate
                );
            }
            
            if (healthSystem)
            {
                healthSystem.AssignHealthData(
                    characterData.MaxHealth,
                    characterData.InvincibleTimePerHit, this);
            }
            
            if (combatSystem)
                combatSystem.AssignCombatData(characterData.BaseDamage);
            
            _isInitialize = true;
        }

        private void SubscribeDependency()
        {
            _inputSystem ??= GetComponent<ICharacterInput>();
            
            if (_inputSystem != null)
            {
                _inputSystem.OnPrimarySkillPerform += skillSystem.PerformPrimarySkill;
                _inputSystem.OnSecondarySkillPerform += skillSystem.PerformSecondarySkill;
                _inputSystem.OnMove += movementSystem.AssignInputDirection;
            }
            
            if (comboSystem)
                combatSystem.OnDealDamage += comboSystem.RegisterHit;
        }

        private void UnSubscribeDependency()
        {
            if (_inputSystem != null)
            {
                _inputSystem.OnPrimarySkillPerform -= skillSystem.PerformPrimarySkill;
                _inputSystem.OnSecondarySkillPerform -= skillSystem.PerformSecondarySkill;
                _inputSystem.OnMove -= movementSystem.AssignInputDirection;
            }
            
            if (comboSystem)
                combatSystem.OnDealDamage -= comboSystem.RegisterHit;
        }
        
        public void TryPlayFeedback(FeedbackName feedbackName)
        {
            // handle feedback condition here.
            if (!feedbackSystem) return;
            feedbackSystem.PlayFeedback(feedbackName);
        }

        public void TryStopFeedback(FeedbackName feedbackName)
        {
            if (!feedbackSystem) return;
            feedbackSystem.StopFeedback(feedbackName);
        }

        /// <summary>
        /// Resets the damage-on-touch system by disabling its damage output.
        /// Commonly used during respawn, state reset, or when temporarily disabling attack interactions.
        /// </summary>
        public void ResetAllDependentBehavior()
        {
            if (!_isInitialize) return;
            movementSystem?.ResetMovementSystem();
            skillSystem?.ResetSkillSystem();
            healthSystem?.ResetHealthSystem();
            statusEffectSystem?.ResetStatusEffectSystem();
            damageOnTouch?.ResetDamageOnTouch();
        }

        #endregion
    }
}