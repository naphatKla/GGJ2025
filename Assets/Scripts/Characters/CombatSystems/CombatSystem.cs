using System;
using Characters.Controllers;
using Characters.FeedbackSystems;
using UnityEngine;

namespace Characters.CombatSystems
{
    /// <summary>
    /// Handles character combat logic, including base damage management and damage calculation.
    /// Provides hooks for reacting to damage-dealing events and supports dynamic damage scaling.
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        #region Inspector & Variables

        /// <summary>
        /// The original base damage assigned to this character.
        /// Typically defined by character stats or ScriptableObject data.
        /// </summary>
        private float _baseDamage;

        /// <summary>
        /// The current damage value used for actual damage calculations.
        /// Can be modified dynamically through buffs, debuffs, or status effects.
        /// </summary>
        private float _currentDamage;

        /// <summary>
        /// Owner controller.
        /// </summary>
        private BaseController _owner;

        /// <summary>
        /// Event triggered whenever this character successfully deals damage.
        /// Useful for triggering combo counters, visual effects, or gameplay responses.
        /// </summary>
        public Action OnDealDamage { get; set; }
        
        /// <summary>
        /// Event triggered whenever this character and target perform attack in the same time.
        /// </summary>
        public Action OnCounterAttack { get; set; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Assigns the base damage stat to this combat system.
        /// Also initializes the current damage value to match the base.
        /// </summary>
        /// <param name="baseDamage">The base damage to be used for combat calculations.</param>
        public void AssignCombatData(float baseDamage)
        {
            _baseDamage = baseDamage;
            _currentDamage = baseDamage;
        }

        /// <summary>
        /// Calculates final damage dealt based on the current damage value and a multiplier.
        /// Commonly used during skill or attack execution.
        /// </summary>
        /// <param name="multiplier">The multiplier applied to current damage (e.g. from skills, crits).</param>
        /// <returns>The final damage value to be applied to a target.</returns>
        public float CalculateDamageDeal(float multiplier)
        {
            return _currentDamage * multiplier;
        }

        public void OnCounterAttackHandler()
        {
            OnCounterAttack?.Invoke();
            
            if (!TryGetComponent(out BaseController owner)) return;
            owner?.TryPlayFeedback(FeedbackName.CounterAttack);
        }

        public void OnDealDamageHandler()
        {
            OnDealDamage?.Invoke();
            
            if (!TryGetComponent(out BaseController owner)) return;
            owner?.TryPlayFeedback(FeedbackName.AttackHit);
        }

        #endregion
 
    }
}
