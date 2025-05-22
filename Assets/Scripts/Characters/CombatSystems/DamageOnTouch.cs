using System;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CombatSystems
{
    /// <summary>
    /// Handles applying damage to other objects upon collision, based on the Physics2D Layer Matrix settings.
    /// Typically used for melee enemies or hazardous objects that deal damage when touched.
    /// </summary>
    public class DamageOnTouch : MonoBehaviour
    {
        #region Inspectors & Variables

        /// <summary>
        /// Indicates whether this object is currently allowed to deal damage on contact.
        /// Can be toggled at runtime for dynamic combat behaviors.
        /// </summary>
        [ShowInInspector, ReadOnly] 
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isEnableDamage;
        
        #endregion
        
        #region Unity Methods

        /// <summary>
        /// Called by Unity when another collider marked as trigger enters this object's collider.
        /// If damage is enabled, applies damage to the target via <see cref="CombatManager"/>.
        /// </summary>
        /// <param name="other">The other collider that has entered this object's trigger.</param>
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!_isEnableDamage) return;
            CombatManager.ApplyDamageTo(other.gameObject, gameObject);
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Enables or disables the ability to apply damage on contact.
        /// Should be called based on character state (e.g. active, stunned, dead).
        /// </summary>
        /// <param name="isEnable">True to enable damage, false to disable.</param>
        public void EnableDamage(bool isEnable)
        {
            _isEnableDamage = isEnable;
        }
        
        /// <summary>
        /// Resets the damage system by disabling contact-based damage.
        /// Often called during character death or respawn.
        /// </summary>
        public void ResetDamageOnTouch()
        {
            EnableDamage(false);
        }

        #endregion
    }
}
