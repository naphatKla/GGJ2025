using Characters.CombatSystems;
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
        [SerializeField] protected SkillSystem skillSystem;

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
        [SerializeField] protected CombatSystem combatSystem;

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
        /// ScriptableObject containing base character stats used to initialize systems.
        /// </summary>
        [PropertyOrder(9999)]
        [Title("Data")] [SerializeField] private BaseCharacterDataSo characterData;

        /// <summary>
        /// Character input handler implementing <see cref="ICharacterInput"/>.
        /// Serialized using Odin to allow assignment of interface in the Inspector.
        /// Fallbacks to auto-fetching via <c>TryGetComponent</c> if not manually assigned.
        /// </summary>
        [OdinSerialize] private ICharacterInput _inputSystem;
        
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
        public BaseCharacterDataSo CharacterData => characterData;
        
        #endregion

        #region Unity Methods

        protected virtual void OnEnable()
        {
            SubscribeDependency();
            AssignCharacterData(characterData);
        }

        protected virtual void OnDisable()
        {
            UnSubscribeDependency();
        }

        #endregion

        #region Methods
        
        public virtual void AssignCharacterData(BaseCharacterDataSo data)
        {
            characterData = data;
            skillSystem.AssignData(this, data);
            movementSystem.AssignMovementData(
                characterData.BaseSpeed,
                characterData.MoveAccelerationRate,
                characterData.TurnAccelerationRate
            );
            healthSystem.AssignHealthData(
                characterData.MaxHealth,
                characterData.InvincibleTimePerHit, this);
            combatSystem.AssignCombatData(characterData.BaseDamage);
        }
        
        protected virtual void SubscribeDependency()
        {
            _inputSystem ??= GetComponent<ICharacterInput>();

            if (_inputSystem == null) return;
            _inputSystem.OnSkillPerform += skillSystem.PerformSkill;
            _inputSystem.OnMove += movementSystem.AssignInputDirection;
        }

        protected virtual void UnSubscribeDependency()
        {
            if (_inputSystem == null) return;
            _inputSystem.OnSkillPerform -= skillSystem.PerformSkill;
            _inputSystem.OnMove -= movementSystem.AssignInputDirection;
        }
        
        public void TryPlayFeedback(FeedbackName feedbackName)
        {
            // handle feedback condition here.
            if (!feedbackSystem) return;
            if (feedbackName == FeedbackName.AttackHit)
            {
                if (FeedbackSystem.IsFeedbackPlaying(FeedbackName.CounterAttack))
                    return;
            }
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
        public virtual void ResetAllDependentBehavior()
        {
            movementSystem.ResetMovementSystem();
            skillSystem.ResetSkillSystem();
            healthSystem.ResetHealthSystem();
            statusEffectSystem.ResetStatusEffectSystem();
            damageOnTouch.ResetDamageOnTouch();
        }

        #endregion
    }
}