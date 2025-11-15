using System;
using System.Collections.Generic;
using Characters.CombatSystems;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.HeathSystems;
using UnityEngine;
using UnityEngine.XR;

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

        public DamageData(GameObject attacker, GameObject targetHit, Vector2 hitPos, float damage, bool isCritical,
            float lifeSteal)
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
        private static readonly Dictionary<GameObject, BaseController> _characterCaches = new();
        private static float _lastTimePlayerCounterDash;
        private static float _counterDashCooldown = 0.15f;

        /// <summary>
        /// Applies damage from an attacker GameObject to a target GameObject.
        /// Calculates the final damage using the attacker's <see cref="CombatSystem"/> with the specified multiplier,
        /// and applies it to the target's <see cref="HealthSystem"/>.
        /// If the damage is successfully applied, it triggers the attacker's <c>OnDealDamage</c> event.
        /// </summary>
        /// <param name="target">The GameObject receiving the damage.</param>
        /// <param name="attacker">The GameObject dealing the damage.</param>
        /// <param name="multiplier">A multiplier applied to the attacker's damage (default is 100%).</param>
        public static void ApplyCalculatedDamageTo(GameObject target, GameObject attacker, Vector2 hitPosition,
            float baseSkillDamage, float multiplier, float additionalCriRate, float additionCriDamge,
            float lifeStealPercent, float lifeStealEffective)
        {
            bool counterAttackThisFrame = false;
            
            if (!_characterCaches.ContainsKey(target))
                _characterCaches.Add(target, target.GetComponent<BaseController>());

            if (!_characterCaches.ContainsKey(attacker))
                _characterCaches.Add(attacker, attacker.GetComponent<BaseController>());

            var targetController = _characterCaches[target];
            var attackerController = _characterCaches[attacker];

            // Counter attack: both attacker and target have DamageOnTouch enabled
            if (targetController.DamageOnTouch.IsEnableDamage && attackerController.DamageOnTouch.IsEnableDamage)
            {
                counterAttackThisFrame = true;
                attackerController.CombatSystem.OnCounterAttackHandler();
            }
            
            var damageData = attackerController.CombatSystem.CalculateSkillDamageDeal(target, hitPosition,
                baseSkillDamage, multiplier, additionalCriRate, additionCriDamge, lifeStealPercent, lifeStealEffective);
            
            // Apply Damage To Target ==========================================
            if (targetController is PlayerController && counterAttackThisFrame)
            {
                if (_lastTimePlayerCounterDash + _counterDashCooldown >= Time.time)
                {
                    _lastTimePlayerCounterDash = Time.time;
                    return; // counter attack player won't take damage.
                }
            }
               
            if (!targetController.HealthSystem.TakeDamage(damageData.Damage, out bool dieThisFrame)) return;
            attackerController.CombatSystem.OnDealDamageHandler(damageData);

            if (damageData.LifeSteal > 0)
                attackerController.HealthSystem.Heal(damageData.LifeSteal);
            
            if (targetController.FeedbackSystem is PlayerFeedbackSystem playerFeedback)
                playerFeedback.OpenFocusBlackDropOnHit(0.4f, attacker);

            if (!dieThisFrame) return;
            attackerController.CombatSystem.OnKillHandler(targetController);
            if (targetController is PlayerController)
                Debug.LogWarning(attackerController.name + "Kill Player!!!");
        }

        public static void ApplyRawDamageTo(GameObject target, GameObject attacker, float damage)
        {
            if (!_characterCaches.ContainsKey(target))
                _characterCaches.Add(target, target.GetComponent<BaseController>());

            var targetController = _characterCaches[target];
            
            if (!targetController.HealthSystem.TakeDamage(damage, out _)) return;
            
            if (targetController.FeedbackSystem is PlayerFeedbackSystem playerFeedback)
                playerFeedback.OpenFocusBlackDropOnHit(0.4f, attacker);
        }

        public static void ClearCache()
        {
            _characterCaches.Clear();
        }
    }
}