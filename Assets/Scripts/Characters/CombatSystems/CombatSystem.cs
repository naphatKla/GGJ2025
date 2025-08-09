using System;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;
using Manager;
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
        public Action<DamageData> OnDealDamage { get; set; }
        
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
        public void AssignCombatData(float baseDamage, BaseController owner)
        {
            _baseDamage = baseDamage;
            _currentDamage = baseDamage;
            _owner = owner;
        }

        /// <summary>
        /// Calculates final damage dealt based on the current damage value and a multiplier.
        /// Commonly used during skill or attack execution.
        /// </summary>
        /// <param name="multiplier">The multiplier percent% applied to current damage (e.g. from skills, crits).</param>
        /// <returns>The final damage value to be applied to a target.</returns>
        public DamageData CalculateSkillDamageDeal(GameObject target, Vector2 hitPos, float baseSkillDamage, float multiplier)
        {
            bool isCritical = Random.Range(0, 100) < 20;
            float damageDeal = baseSkillDamage + ((multiplier/100) * _currentDamage);
            damageDeal = isCritical ? damageDeal * 2 : damageDeal;
            
            var damageData = new DamageData(gameObject, target, hitPos, damageDeal, isCritical);
            return damageData;
        }

        public void OnCounterAttackHandler()
        {
            OnCounterAttack?.Invoke();
            _owner.TryPlayFeedback(FeedbackName.CounterAttack);
        }

        public void OnDealDamageHandler(DamageData damageData)
        {
            OnDealDamage?.Invoke(damageData);
            _owner.TryPlayFeedback(FeedbackName.AttackHit);
        }

        private async UniTask ResetOrthoAfterLerp(float waitTime)
        {
            await UniTask.WaitForSeconds(waitTime, this, cancellationToken: this.destroyCancellationToken);
        }
        #endregion
 
    }
}
