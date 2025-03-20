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
        private LayerMask TargetLayer => CharacterGlobalSettings.Instance.EnemyLayerDictionary[tag];
        #endregion
        
        #region Unity Methods
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isEnableDamage) return;
            if (other.gameObject.layer != TargetLayer) return;
            if (TryGetComponent(out HealthSystem targetHealth))
            {
                Debug.LogWarning($"{targetHealth.gameObject.name} can't take damage. Health system not found");
            }
            
            targetHealth.TakeDamage(1);
        }
        #endregion
    }
}
