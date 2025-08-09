using System.Collections.Generic;
using Characters.CombatSystems;
using Characters.HeathSystems;
using UnityEngine;

namespace Manager
{
    public class DamageData
    {
        public GameObject Attacker { get; }
        public GameObject TargetHit { get; }
        public Vector2 HitPosition { get; }
        public float Damage { get; }
        public float LifeSteal { get; }
        public bool IsCritical { get; }

        public DamageData(GameObject attacker, GameObject targetHit, Vector2 hitPos, float damage, bool isCritical,float lifeSteal)
        {
            Attacker = attacker;
            TargetHit = targetHit;
            HitPosition = hitPos;
            Damage = damage;
            IsCritical = isCritical;
            LifeSteal = lifeSteal;
        }
    }

    /// <summary>
    /// Static manager responsible for handling combat interactions between entities.
    /// Provides utility methods for applying damage and triggering combat-related callbacks.
    /// </summary>
    public static class CombatManager
    {
        // ----- Component Caches -----
        private static readonly Dictionary<GameObject, HealthSystem> _healthCache = new();
        private static readonly Dictionary<GameObject, CombatSystem> _combatCache = new();
        private static readonly Dictionary<GameObject, DamageOnTouch> _touchCache = new();

        /// <summary>
        /// Applies damage from an attacker GameObject to a target GameObject.
        /// Calculates the final damage using the attacker's <see cref="CombatSystem"/> with the specified multiplier,
        /// and applies it to the target's <see cref="HealthSystem"/>.
        /// If the damage is successfully applied, it triggers the attacker's <c>OnDealDamage</c> event.
        /// </summary>
        /// <param name="target">The GameObject receiving the damage.</param>
        /// <param name="attacker">The GameObject dealing the damage.</param>
        /// <param name="multiplier">A multiplier applied to the attacker's damage (default is 100%).</param>
        public static void ApplyDamageTo(GameObject target, GameObject attacker, Vector2 hitPosition,
            float baseSkillDamage, float multiplier, float additionalCriRate, float additionCriDamge,
            float lifeStealPercent, float lifeStealEffective)
        {
            if (!TryGetCachedComponent(target, _healthCache, out var targetHealth)) return;

            if (!TryGetCachedComponent(attacker, _combatCache, out var attackerCombatSystem))
            {
                Debug.LogWarning("Can not apply damage. No attacker CombatSystem.");
                return;
            }

            // Counter attack: both attacker and target have DamageOnTouch enabled
            if (TryGetCachedComponent(attacker, _touchCache, out var attackerDamage) &&
                TryGetCachedComponent(target, _touchCache, out var targetDamage) &&
                attackerDamage.IsEnableDamage && targetDamage.IsEnableDamage)
            {
                attackerCombatSystem.OnCounterAttackHandler();
            }

            var damageData = attackerCombatSystem.CalculateSkillDamageDeal(targetHealth.gameObject, hitPosition,
                baseSkillDamage, multiplier, additionalCriRate, additionCriDamge, lifeStealPercent, lifeStealEffective);

            if (!targetHealth.TakeDamage(damageData.Damage)) return;
            attackerCombatSystem.OnDealDamageHandler(damageData);
        }

        /// <summary>
        /// Gets a component with caching to avoid repeated GetComponent calls.
        /// </summary>
        private static bool TryGetCachedComponent<T>(GameObject obj, Dictionary<GameObject, T> cache, out T result)
            where T : Component
        {
            if (obj == null)
            {
                result = null;
                return false;
            }

            if (cache.TryGetValue(obj, out result))
                return result != null;

            result = obj.GetComponent<T>();
            cache[obj] = result;
            return result != null;
        }
    }
}