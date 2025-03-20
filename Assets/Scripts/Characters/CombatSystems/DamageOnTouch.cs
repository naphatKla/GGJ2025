using Characters.HeathSystems;
using UnityEngine;

namespace Characters.CombatSystems
{
    /// <summary>
    /// Applies damage to any object it collides with, based on the Physics2D Layer Matrix settings.
    /// This script is useful for hazards, enemy attacks, or environmental damage.
    /// </summary>
    public class DamageOnTouch : MonoBehaviour
    {
        #region Inspectors & Variables

        /// <summary>
        /// Determines whether this object can deal damage.
        /// </summary>
        public bool isEnableDamage;

        /// <summary>
        /// The amount of damage to apply when colliding with a valid target.
        /// </summary>
        public float damage;

        #endregion

        #region Unity Methods

        /// <summary>
        /// Applies damage to a valid target when entering a trigger collider.
        /// The target must have a <see cref="HealthSystem"/> component to receive damage.
        /// The interaction is controlled by the Layer Matrix settings in Project Settings → Physics2D.
        /// </summary>
        /// <param name="other">The collider that entered the trigger.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isEnableDamage) return;
            if (!other.TryGetComponent(out HealthSystem targetHealth)) return;
            targetHealth.TakeDamage(damage);
        }

        #endregion
    }
}