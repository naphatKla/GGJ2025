using System;
using UnityEngine;

namespace Characters.HeathSystems
{
    /// <summary>
    /// Handles the health system of a character, including taking damage, healing,
    /// invincibility status, and death state.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        #region Inspectors & Variables

        /// <summary>
        /// The maximum health the character can have.
        /// </summary>
        [SerializeField] private float maxHealth;

        /// <summary>
        /// The current health of the character.
        /// </summary>
        private float _currentHealth;

        /// <summary>
        /// Determines if the character is temporarily invincible.
        /// </summary>
        private bool _isInvincible;

        /// <summary>
        /// Indicates whether the character is dead.
        /// </summary>
        private bool _isDead;

        /// <summary>
        /// Event triggered when the character takes damage.
        /// </summary>
        public Action OnTakeDamage { get; set; }

        /// <summary>
        /// Event triggered when the character heals.
        /// </summary>
        public Action OnHeal { get; set; }

        /// <summary>
        /// Event triggered when the character dies.
        /// </summary>
        public Action OnDead { get; set; }

        /// <summary>
        /// Event triggered when the invincibility state changes.
        /// The boolean parameter represents whether the character is now invincible.
        /// </summary>
        public Action<bool> OnInvincible { get; set; }

        #endregion

        #region Unity Methods

        /// <summary>
        /// Initializes the health system by resetting health to maximum.
        /// </summary>
        private void Start()
        {
            ResetHealth();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reduces the character's health by the given damage amount.
        /// Prevents damage if the character is invincible or already dead.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        public void TakeDamage(float damage)
        {
            if (_isInvincible) return;
            if (_isDead) return;
            ModifyHealth(-damage);
            OnTakeDamage?.Invoke();

            if (_currentHealth <= 0)
            {
                Dead();
            }
        }

        /// <summary>
        /// Increases the character's health by the given amount, up to the maximum health.
        /// </summary>
        /// <param name="healAmount">The amount of health to restore.</param>
        public void Heal(float healAmount)
        {
            if (_isDead) return;
            ModifyHealth(healAmount);
            OnHeal?.Invoke();
        }

        /// <summary>
        /// Sets the character's invincibility state.
        /// </summary>
        /// <param name="value">True to make the character invincible, false to disable invincibility.</param>
        public void SetInvincible(bool value)
        {
            _isInvincible = value;
            OnInvincible?.Invoke(_isInvincible);
        }

        /// <summary>
        /// Resets the character's health to maximum and revives them if they were dead.
        /// </summary>
        public void ResetHealth()
        {
            _currentHealth = maxHealth;
            _isDead = false;
        }

        /// <summary>
        /// Modifies the character's health by a given value.
        /// Ensures health does not exceed the maximum or drop below zero.
        /// </summary>
        /// <param name="value">The amount to modify health by (positive for healing, negative for damage).</param>
        private void ModifyHealth(float value)
        {
            _currentHealth += value;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
        }

        /// <summary>
        /// Triggers the character's death state if their health reaches zero.
        /// Ensures the death logic is executed only once.
        /// </summary>
        private void Dead()
        {
            if (_isDead) return;
            _isDead = true;
            OnDead?.Invoke();
        }

        #endregion
    }
}
