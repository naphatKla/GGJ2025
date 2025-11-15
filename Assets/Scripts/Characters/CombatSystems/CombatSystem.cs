using System;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

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
        
        private float _baseCriRate;
        
        private float _baseCriDamage;
        
        private float _baseLifeStealPercent;
        
        private float _baseLifeStealEffective;
        
        public int TotalKill { get; private set; }
        public int TotalDamageDeal { get; private set; }
        public int TotalCriticalCount { get; private set; }
        public int TotalCounterDashCount { get; private set; }

        /// <summary>
        /// The current damage value used for actual damage calculations.
        /// Can be modified dynamically through buffs, debuffs, or status effects.
        /// </summary>
        [ShowInInspector, ReadOnly]
        private float _currentDamage;

        [ShowInInspector, ReadOnly]
        private float _currentDamageMultiplier;
        
        /// <summary>
        /// Owner controller.
        /// </summary>
        private BaseController _owner;

        public float CurrentDamage => _currentDamage;

        /// <summary>
        /// Event triggered whenever this character successfully deals damage.
        /// Useful for triggering combo counters, visual effects, or gameplay responses.
        /// </summary>
        public Action<DamageData> OnDealDamage { get; set; }

        /// <summary>
        /// Event triggered whenever this character and target perform attack in the same time.
        /// </summary>
        public Action OnCounterAttack { get; set; }
        
        public Action<BaseController> OnKill { get; set; } //object killed

        #endregion

        #region Methods

        /// <summary>
        /// Assigns the base damage stat to this combat system.
        /// Also initializes the current damage value to match the base.
        /// </summary>
        /// <param name="baseDamage">The base damage to be used for combat calculations.</param>
        public void AssignCombatData(BaseController owner, float baseDamage, float baseCriRate, float baseCriDamage, float baseLifeStealPercent, float baseLifeStealEffective)
        {
            _owner = owner;
            _baseDamage = baseDamage;
            _baseCriRate = baseCriRate;
            _baseCriDamage = baseCriDamage;
            _baseLifeStealPercent = baseLifeStealPercent;
            _baseLifeStealEffective = baseLifeStealEffective;
            _currentDamage = baseDamage;
        }

        /// <summary>
        /// Calculates final damage dealt based on the current damage value and a multiplier.
        /// Commonly used during skill or attack execution.
        /// </summary>
        /// <param name="multiplier">The multiplier percent% applied to current damage (e.g. from skills, crits).</param>
        /// <returns>The final damage value to be applied to a target.</returns>
        public DamageData CalculateSkillDamageDeal(GameObject target, Vector2 hitPos, float baseSkillDamage,
            float multiplier, float additionalCriRate, float additionCriDamage, float additionalLifeStealPercent,
            float additionalLifeStealEffective)
        {
            var calculatedCriRate = _baseCriRate + additionalCriRate;
            var calculatedCriDamage = _baseCriDamage + additionCriDamage;
            var calculatedLifeStealPercent = _baseLifeStealPercent + additionalLifeStealPercent;
            var calculatedLifeStealEffective = _baseLifeStealEffective + additionalLifeStealEffective;
            var calculatedCurrentDamage = _currentDamage + (_currentDamage * (_currentDamageMultiplier/100));
            
            bool isCritical = Random.Range(0, 100) < calculatedCriRate;
            float damageDeal = baseSkillDamage + ((multiplier / 100) * calculatedCurrentDamage);
            damageDeal = isCritical ? damageDeal + (damageDeal * calculatedCriDamage/100) : damageDeal;

            bool isLifeSteal = Random.Range(0, 100) < calculatedLifeStealPercent;
            float lifeSteal = isLifeSteal ? damageDeal * (calculatedLifeStealEffective/100) : 0;

            damageDeal = Mathf.Ceil(damageDeal);
            lifeSteal = Mathf.Ceil(lifeSteal);
            
            var damageData = new DamageData(gameObject, target, hitPos, damageDeal, isCritical, lifeSteal);
            return damageData;
        }

        public void OnCounterAttackHandler()
        {
            OnCounterAttack?.Invoke();
            _owner.TryPlayFeedback(FeedbackName.Character.CounterAttack);
            TotalCounterDashCount++;
        }

        public void OnDealDamageHandler(DamageData damageData)
        {
            OnDealDamage?.Invoke(damageData);
            _owner.TryPlayFeedback(FeedbackName.Character.AttackHit);
            TotalDamageDeal += (int)damageData.Damage;
            
            if (damageData.IsCritical)
                TotalCriticalCount++;
        }

        public void OnKillHandler(BaseController targetKilled)
        {
            OnKill?.Invoke(targetKilled);
            TotalKill++;
        }
        
        public void AddCurrentDamage(float value)
        {
            _currentDamage = Mathf.Max(0, _currentDamage + value);
        }

        public void AddCurrentDamageMultiplierPercent(float multiplierPercentage)
        {
            _currentDamageMultiplier += multiplierPercentage;
        }

        public void ResetCombatSystem()
        {
            _currentDamage = _baseDamage;
            _currentDamageMultiplier = 0;
        }

        #endregion
    }
}