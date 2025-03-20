using System;
using UnityEngine;

namespace Characters.HeathSystems
{
    public class HealthSystem : MonoBehaviour
    {
        #region Inspectors & Variables
        [SerializeField] private float maxHealth;
        private float _currentHealth;
        private bool _isInvincible;
        private bool _isDead;
        public Action OnTakeDamage { get; set; }
        public Action OnHeal { get; set; }
        public Action OnDead { get; set; }
        public Action<bool> OnInvincible { get; set; }
        #endregion
        
        #region Unity Methods
        private void Start()
        {
            ResetHealth();
        }
        #endregion

        #region Methods
        public void TakeDamage(float damage)
        {
            if (_isInvincible) return;
            if (_isDead) return;
            ModifyHealth(-damage);
            OnTakeDamage?.Invoke();
        }

        public void Heal(float healAmount)
        {
            if (_isDead) return;
            ModifyHealth(healAmount);
            OnHeal?.Invoke();
        }

        public void SetInvincible(bool value)
        {
            _isInvincible = value;
            OnInvincible?.Invoke(_isInvincible);
        }
        
        public void ResetHealth()
        {
            _currentHealth = maxHealth;
            _isDead = false;
        }

        private void ModifyHealth(float value)
        {
            _currentHealth += value;
            _currentHealth = Mathf.Clamp(_currentHealth, 0,maxHealth);
        }
        
        private void Dead()
        {
            if (_isDead) return;
            _isDead = true;
            OnDead?.Invoke();
        }
        #endregion
    }
}
