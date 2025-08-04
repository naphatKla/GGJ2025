using System.Collections.Generic;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CombatSystems
{
    public class DamageOnTouch : MonoBehaviour
    {
        #region Inspectors & Variables

        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isEnableDamage;

        private GameObject _attacker;
        private float _hitPerSec = 1f;

        // Cooldown map: tracks when each target can be hit next
        private readonly Dictionary<GameObject, float> _nextHitTimeMap = new();
        public bool IsEnableDamage => _isEnableDamage;

        #endregion

        #region Unity Methods

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_isEnableDamage || _attacker == null) return;

            GameObject target = other.gameObject;
            float currentTime = Time.time;

            if (_nextHitTimeMap.TryGetValue(target, out float nextHitTime))
            {
                if (currentTime < nextHitTime)
                    return; 
            }

            // Apply damage
            CombatManager.ApplyDamageTo(target, _attacker);

            // Set next allowed hit time
            float cooldown = 1f / Mathf.Max(_hitPerSec, 0.01f);
            _nextHitTimeMap[target] = currentTime + cooldown;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Optional: Clean up target entry to avoid memory growth
            _nextHitTimeMap.Remove(other.gameObject);
        }

        #endregion

        #region Methods

        public void EnableDamage(bool isEnable, GameObject ownerAttacker, float hitPerSec)
        {
            _isEnableDamage = isEnable;
            _attacker = ownerAttacker;
            _hitPerSec = Mathf.Max(hitPerSec, 0.01f);

            if (ownerAttacker && ownerAttacker != gameObject)
                gameObject.layer = ownerAttacker.layer;

            if (!isEnable)
                _nextHitTimeMap.Clear();
        }

        public void ResetDamageOnTouch()
        {
            EnableDamage(false, null, 1);
        }

        #endregion
    }
}
