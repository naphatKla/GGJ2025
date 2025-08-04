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
            public float NextHitTime;
        }

        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isEnableDamage;

        private GameObject _owner;
        private readonly List<DamageInstance> _damageInstances = new();
        private readonly Dictionary<(GameObject, object), float> _cooldownMap = new();

        public GameObject Owner => _owner;
        public bool IsEnableDamage => _isEnableDamage;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_isEnableDamage || _owner == null) 
                return;
            
            GameObject target = other.gameObject;
            float now = Time.time;
            
            foreach (var instance in _damageInstances)
            {
                var key = (target, instance.Caller);

                if (_cooldownMap.TryGetValue(key, out float nextTime))
                {
                    if (now < nextTime)
                        continue;
                }

                // Apply damage
                CombatManager.ApplyDamageTo(target, _owner);

                float cooldown = 1f / Mathf.Max(instance.HitPerSec, 0.01f);
                _cooldownMap[key] = now + cooldown;
            }
        }

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

            var newInstance = new DamageInstance
            {
                Caller = caller,
                HitPerSec = Mathf.Max(hitPerSec, 0.01f),
                NextHitTime = Time.time
            };

            _damageInstances.Add(newInstance);
            _isEnableDamage = true;
        }

        public void DisableDamage(object caller)
        {
            _damageInstances.RemoveAll(x => x.Caller == caller);

            // Remove cooldowns related to this caller
            var keysToRemove = new List<(GameObject, object)>();
            foreach (var key in _cooldownMap.Keys)
            {
                if (key.Item2 == caller)
                    keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                _cooldownMap.Remove(key);
            }

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
            _isEnableDamage = false;
            _owner = null;
        }
    }
}
