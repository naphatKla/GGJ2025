using System;
using Characters.HeathSystems;
using GlobalSettings;
using UnityEngine;

namespace Characters.CombatSystems
{
    public class DamageOnTouch : MonoBehaviour
    {
        #region Inspectors & Variables
        public bool isEnableDamage;
        public float damage;
        #endregion
        
        #region Unity Methods
        /// <summary>
        /// Damage is applied to targets based on the Layer Matrix settings (Project Settings → Physics2D)
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isEnableDamage) return;
            if (!TryGetComponent(out HealthSystem targetHealth)) return;
            targetHealth.TakeDamage(damage);
        }
        #endregion
    }
}
