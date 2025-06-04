using System;
using System.IO.Compression;
using Characters.FeedbackSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.HeathSystems
{
    /// <summary>
    /// Handles the health system of a character, including taking damage, healing,
    /// invincibility status, hit cooldown, and death state.
    /// Prevents taking damage during temporary invincibility or hit cooldown period.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        #region Inspectors & Variables

        /// <summary>
        /// The maximum health the character can have.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _maxHealth;

        /// <summary>
        /// The current health of the character.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentHealth;

        /// <summary>
        /// Determines if the character is temporarily invincible.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isInvincible;

        /// <summary>
        /// The duration (in seconds) of temporary hit cooldown after taking damage.
        /// During this time, additional hits will be ignored.
        /// </summary>
        private float _invincibleTimePerHit;

        /// <summary>
        /// Whether the character is currently in hit cooldown state.
        /// Prevents taking consecutive damage.
        /// </summary>
        private bool _isHitCooldown;

        /// <summary>
        /// Indicates whether the character is dead.
        /// </summary>
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isDead;

        /// <summary>
        /// Indicates whether the character is dead.
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// Event triggered when the character takes damage.
        /// </summary>
        public Action OnTakeDamage { get; set; }

        /// <summary>
        /// Event triggered when the character heals.
        /// </summary>
        public Action OnHeal { get; set; }

        /// <summary>
        /// Event triggered when this character dies.
        /// </summary>
        public Action OnDead { get; set; }

        /// <summary>
        /// Event triggered when the invincibility state changes.
        /// The boolean parameter represents whether the character is now invincible.
        /// </summary>
        public Action<bool> OnInvincible { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns the health data for the character.
        /// This method is typically called by the character controller during initialization.
        /// </summary>
        /// <param name="maxHealth">Maximum health to assign.</param>
        /// <param name="invincibleTimePerHit">Cooldown duration after taking damage.</param>
        public void AssignHealthData(float maxHealth, float invincibleTimePerHit)
        {
            _maxHealth = maxHealth;
            _invincibleTimePerHit = invincibleTimePerHit;
            ResetHealthSystem();
        }

        /// <summary>
        /// Reduces the character's health by the given damage amount.
        /// Prevents damage if the character is invincible, in cooldown, or already dead.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        /// <returns>True if the damage was applied; otherwise, false.</returns>
        public bool TakeDamage(float damage)
        {
            if (_isInvincible) return false;
            if (_isHitCooldown) return false;
            if (_isDead) return false;
            ModifyHealth(-damage);
            OnTakeDamage?.Invoke();
            
            if (TryGetComponent(out FeedbackSystem feedbackSystem))
                feedbackSystem.PlayFeedback(FeedbackName.TakeDamage);
           
            HitCooldownHandler();

            if (_currentHealth <= 0)
            {
                Dead();
            }

            return true;
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
        /// Also clears invincibility and hit cooldown states.
        /// </summary>
        public void ResetHealthSystem()
        {
            _currentHealth = _maxHealth;
            SetInvincible(false);
            _isHitCooldown = false;
            _isDead = false;
        }

        /// <summary>
        /// Starts the hit cooldown period after the character takes damage.
        /// Prevents additional damage for the configured duration.
        /// </summary>
        public async void HitCooldownHandler()
        {
            _isHitCooldown = true;
            await UniTask.WaitForSeconds(_invincibleTimePerHit);
            _isHitCooldown = false; 
        }

        /// <summary>
        /// Modifies the character's health by a given value.
        /// Ensures health does not exceed the maximum or drop below zero.
        /// </summary>
        /// <param name="value">The amount to modify health by (positive for healing, negative for damage).</param>
        private void ModifyHealth(float value)
        {
            _currentHealth += value;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
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
            gameObject.SetActive(false);
        }

        #endregion
    }
}
