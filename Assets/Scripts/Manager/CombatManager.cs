using Characters.CombatSystems;
using Characters.HeathSystems;
using Characters.StatusEffectSystems;
using UnityEngine;

namespace Manager
{   
    /// <summary>
    /// Static manager responsible for handling combat interactions between entities.
    /// Provides utility methods for applying damage and triggering combat-related callbacks.
    /// </summary>
    public static class CombatManager
    {
        /// <summary>
        /// Applies damage from an attacker GameObject to a target GameObject.
        /// Calculates the final damage using the attacker's <see cref="CombatSystem"/> with the specified multiplier,
        /// and applies it to the target's <see cref="HealthSystem"/>.
        /// If the damage is successfully applied, it triggers the attacker's <c>OnDealDamage</c> event.
        /// </summary>
        /// <param name="target">The GameObject receiving the damage.</param>
        /// <param name="attacker">The GameObject dealing the damage.</param>
        /// <param name="multiplier">A multiplier applied to the attacker's damage (default is 1).</param>
        public static void ApplyDamageTo(GameObject target, GameObject attacker, float multiplier = 1)
        {
            if (!target.TryGetComponent(out HealthSystem targetHealth)) return;
            if (!attacker.TryGetComponent(out CombatSystem attackerCombatSystem))
            {
                Debug.LogWarning("Can not apply damage. no attacker no damage deal.");
                return;
            }
            if (StatusEffectManager.TryGetEffect(attacker, StatusEffectName.DamageOnTouch, out _) &&
                StatusEffectManager.TryGetEffect(target, StatusEffectName.DamageOnTouch, out _))
            {
                attackerCombatSystem.OnCounterAttackHandler();
            }
            
            float damageDeal = attackerCombatSystem.CalculateDamageDeal(multiplier);
            if (!targetHealth.TakeDamage(damageDeal)) return;
            attackerCombatSystem.OnDealDamageHandler();
        }

        public static void ApplyDamageTo(GameObject target, float damage)
        {
            if (!target.TryGetComponent(out HealthSystem targetHealth)) return;
            targetHealth.TakeDamage(damage);
        }
    }
}