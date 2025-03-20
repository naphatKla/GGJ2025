using System;
using UnityEngine;

namespace Characters.HeathSystems
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private float maxHealth;
        private float _currentHealth;
        private bool _isDead;
        
        public Action OnTakeDamage { get; set; }
        public Action OnHeal { get; set; }
        public Action OnDead { get; set; }

        private void Start()
        {
            ResetHealth();
        }

        public void TakeDamage(float damage)
        {
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
    }
}
