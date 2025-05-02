using Characters.HeathSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CombatSystems
{
    /// <summary>
    /// Handles applying damage to other objects upon collision, based on the Physics2D Layer Matrix settings.
    /// </summary>
    public class DamageOnTouch : MonoBehaviour
    {
        #region Inspectors & Variables

        /// <summary>
        /// Indicates whether this object is currently allowed to deal damage on contact.
        /// </summary>
        [ShowInInspector, ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isEnableDamage;
        
        /// <summary>
        /// The base damage value assigned to this object.
        /// </summary>
        private float _baseDamage;
        
        /// <summary>
        /// The current amount of damage to apply upon collision.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentDamage;

        #endregion
        
        #region Unity Methods

        /// <summary>
        /// Attempts to apply damage to a valid target when this object enters a trigger collider.
        /// The target must have a <see cref="HealthSystem"/> component to receive damage.
        /// Damage application is controlled by the Layer Matrix settings in Project Settings → Physics2D.
        /// </summary>
        /// <param name="other">The collider that triggered the event.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isEnableDamage) return;
            if (!other.TryGetComponent(out HealthSystem targetHealth)) return;
            targetHealth.TakeDamage(_currentDamage);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns the damage data for the character.
        /// This method is typically called by the character controller during initialization.
        /// </summary>
        /// <param name="baseDamage">The base damage value to assign.</param>
        public void AssignDamageOnTouchData(float baseDamage)
        {
            _baseDamage = baseDamage;
            ModifyDamage(_baseDamage);
        }
        
        /// <summary>
        /// Enables or disables the ability to apply damage on contact.
        /// </summary>
        /// <param name="isEnable">True to enable damage, false to disable.</param>
        public void EnableDamage(bool isEnable)
        {
            _isEnableDamage = isEnable;
        }

        /// <summary>
        /// Modify the current damage value used when applying damage on collision.
        /// </summary>
        /// <param name="damage">The new damage value to use.</param>
        public void ModifyDamage(float damage)
        {
            _currentDamage = damage;
        }

        #endregion
    }
}
