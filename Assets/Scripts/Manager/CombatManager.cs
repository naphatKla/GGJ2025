using System.Collections.Generic;
using Characters.CombatSystems;
using Characters.Controllers;
using Characters.FeedbackSystems;
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

        /// <summary>Separate heavy damage value — reduces target's Break Point instead of HP.</summary>
        public float HeavyDamage { get; }

        public DamageData(GameObject attacker, GameObject targetHit, Vector2 hitPos, float damage, bool isCritical,
            float lifeSteal, float heavyDamage = 0)
        {
            Attacker = attacker;
            TargetHit = targetHit;
            HitPosition = hitPos;
            Damage = damage;
            IsCritical = isCritical;
            LifeSteal = lifeSteal;
            HeavyDamage = heavyDamage;
        }
    }

    /// <summary>
    /// Static manager responsible for handling combat interactions between entities.
    /// Provides utility methods for applying damage and triggering combat-related callbacks.
    /// </summary>
    public static class CombatManager
    {
        // ----- Component Caches -----
        private static readonly Dictionary<GameObject, BaseController> _characterCaches = new();
        private static readonly Dictionary<GameObject, DamageOnTouch> _damageOnTouches = new();

        /// <summary>
        /// Applies damage from an attacker GameObject to a target GameObject.
        /// Calculates the final damage using the attacker's <see cref="CombatSystem"/> with the specified multiplier,
        /// and applies it to the target's <see cref="HealthSystem"/>.
        /// If the damage is successfully applied, it triggers the attacker's <c>OnDealDamage</c> event.
        /// </summary>
        /// <param name="target">The GameObject receiving the damage.</param>
        /// <param name="attacker">The GameObject dealing the damage.</param>
        /// <param name="multiplier">A multiplier applied to the attacker's damage (default is 100%).</param>
        public static void ApplyCalculatedDamageTo(GameObject target, GameObject attacker, string attackerId, GameObject realObjectAttack, Vector2 hitPosition,
            float baseSkillDamage, float multiplier, float additionalCriRate, float additionCriDamge,
            float lifeStealPercent, float lifeStealEffective, float baseSkillHeavyDamage = 0)
        {
            TryGetCharacterFromCache(target, out var targetController);
            TryGetCharacterFromCache(attacker, out var attackerController);
            
            var damageData = attackerController.CombatSystem.CalculateSkillDamageDeal(target, hitPosition,
                baseSkillDamage, multiplier, additionalCriRate, additionCriDamge, lifeStealPercent, lifeStealEffective,
                baseSkillHeavyDamage);
            
            // Apply Damage To Target ==========================================
            var hitInfo = new HealthSystem.HitInfo
            {
                attackerId       = attackerId,
                damage           = damageData.Damage,
                heavyDamage      = damageData.HeavyDamage,
                attacker         = attackerController,
                realObjectAttack = realObjectAttack
            };
            
            // Actual damage ALWAYS applies to HP. Heavy damage is an additional effect that
            // drains the target's Break Point - it never replaces the HP damage.
            if (!targetController.HealthSystem.TakeDamage(hitInfo)) return;
            
            TryTakeHeavyDamage(targetController, hitInfo);
            
            attackerController.CombatSystem.OnDealDamageHandler(damageData);

            if (damageData.LifeSteal > 0)
                attackerController.HealthSystem.Heal(damageData.LifeSteal);
        }

        public static void ApplyRawDamageTo(GameObject target, GameObject objectAttacker, string attackerId, float damage,
            float heavyDamage = 0)
        {
            TryGetCharacterFromCache(target, out var targetController);
            
            var hitInfo = new HealthSystem.HitInfo
            {
                attackerId       = attackerId,
                damage           = damage,
                heavyDamage      = heavyDamage,
                attacker         = null,
                realObjectAttack = objectAttacker
            };
            
            if (!targetController.HealthSystem.TakeDamage(hitInfo)) return;
            
            TryTakeHeavyDamage(targetController, hitInfo);
        }

        /// <summary>
        /// Additionally routes the heavy portion of a committed hit to the target's
        /// <see cref="BreakPointSystem"/> when present. The HP damage of the same hit has already
        /// been applied by the caller - heavy damage only drains Break Point on top of it.
        /// Returns true if the target consumed heavy damage.
        /// </summary>
        private static bool TryTakeHeavyDamage(BaseController targetController, HealthSystem.HitInfo hitInfo)
        {
            if (hitInfo.heavyDamage <= 0) return false;
            if (!targetController.TryGetComponent(out BreakPointSystem breakPointSystem)) return false;
            
            breakPointSystem.TakeHeavyDamage(hitInfo);
            return true;
        }

        public static bool TryGetCharacterFromCache(GameObject target, out BaseController controller)
        {
            controller = null;

            if (target == null)
                return false;
            
            if (_characterCaches.TryGetValue(target, out controller))
            {
                if (controller)
                    return true;
                
                _characterCaches.Remove(target);
                controller = null;
            }

            if (!target.TryGetComponent(out controller)) return false;
            _characterCaches[target] = controller;
            return true;
        }
        
        public static bool TryGetDamageOnTouchFromCache(GameObject target, out DamageOnTouch damageOnTouch)
        {
            damageOnTouch = null;

            if (target == null)
                return false;
            
            if (_damageOnTouches.TryGetValue(target, out damageOnTouch))
            {
                if (damageOnTouch)
                    return true;
                
                _damageOnTouches.Remove(target);
                damageOnTouch = null;
            }

            if (!target.TryGetComponent(out damageOnTouch)) return false;
            _damageOnTouches[target] = damageOnTouch;
            return true;
        }
        
        public static void ClearCache()
        {
            _characterCaches.Clear();
        }
    }
}