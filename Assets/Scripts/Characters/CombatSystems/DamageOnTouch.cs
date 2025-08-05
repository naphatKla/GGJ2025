using System;
using System.Collections.Generic;
using Characters.HeathSystems;
using UnityEngine;
using Manager;
using Sirenix.OdinInspector;

namespace Characters.CombatSystems
{
    public class DamageOnTouch : MonoBehaviour
    {
        private class DamageInstance
        {
            public object Caller;
            public float HitPerSec;
        }

        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isEnableDamage;

        private GameObject _owner;
        private readonly List<DamageInstance> _damageInstances = new();
        private readonly Dictionary<(GameObject target, object caller), float> _cooldownMap = new();
        private readonly List<(GameObject, object)> _cooldownRemoveBuffer = new(); // reuse list

        public GameObject Owner => _owner;
        public bool IsEnableDamage => _isEnableDamage;

        #region Unity Methods

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyDamageTo(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplyDamageTo(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            RemoveCooldownForTarget(other.gameObject);
        }

        #endregion

        #region Public API

        public void EnableDamage(GameObject owner, object caller, float hitPerSec)
        {
            if (caller == null || owner == null)
            {
                Debug.LogWarning("[DamageOnTouch] Invalid caller or owner.");
                return;
            }

            if (_owner == null)
            {
                _owner = owner;
                gameObject.layer = owner.layer;
            }
            else if (_owner != owner)
            {
                Debug.LogWarning("[DamageOnTouch] Already assigned to a different owner.");
                return;
            }

            _damageInstances.Add(new DamageInstance
            {
                Caller = caller,
                HitPerSec = Mathf.Max(hitPerSec, 0.01f),
            });

            _isEnableDamage = true;
        }

        public void DisableDamage(object caller)
        {
            _damageInstances.RemoveAll(instance => instance.Caller == caller);

            // Clean up cooldowns by caller
            _cooldownRemoveBuffer.Clear();
            foreach (var kvp in _cooldownMap)
            {
                if (kvp.Key.caller == caller)
                    _cooldownRemoveBuffer.Add(kvp.Key);
            }
            foreach (var key in _cooldownRemoveBuffer)
                _cooldownMap.Remove(key);

            if (_damageInstances.Count == 0)
            {
                _isEnableDamage = false;
                _owner = null;
            }
        }

        public void ResetDamageOnTouch()
        {
            _damageInstances.Clear();
            _cooldownMap.Clear();
            _cooldownRemoveBuffer.Clear();
            _isEnableDamage = false;
            _owner = null;
        }

        #endregion

        #region Internal Logic

        private void TryApplyDamageTo(Collider2D collider)
        {
            if (!_isEnableDamage || _owner == null) return;

            GameObject target = collider.gameObject;
            float now = Time.time;
            Vector2 hitPosition = collider.ClosestPoint(transform.position);

            for (int i = 0; i < _damageInstances.Count; i++)
            {
                var instance = _damageInstances[i];
                var key = (target, instance.Caller);

                if (_cooldownMap.TryGetValue(key, out float nextTime) && now < nextTime)
                    continue;

                CombatManager.ApplyDamageTo(target, _owner, hitPosition);

                float cooldown = 1f / instance.HitPerSec;
                _cooldownMap[key] = now + cooldown;
            }
        }


        private void RemoveCooldownForTarget(GameObject target)
        {
            _cooldownRemoveBuffer.Clear();
            foreach (var instance in _damageInstances)
            {
                var key = (target, instance.Caller);
                if (_cooldownMap.ContainsKey(key))
                    _cooldownRemoveBuffer.Add(key);
            }
            foreach (var key in _cooldownRemoveBuffer)
                _cooldownMap.Remove(key);
        }

        #endregion
    }
}
