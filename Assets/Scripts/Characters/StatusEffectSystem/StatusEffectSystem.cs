using System.Collections.Generic;
using Characters.StatusEffectSystem.StatusEffectRuntimes;
using ProjectExtensions;
using UnityEngine;

namespace Characters.StatusEffectSystem
{
    public enum StatusEffectName
    {
        Iframe,
        Slow,
        // Add more statuses here
    }
    
    public class StatusEffectSystem : MonoBehaviour
    {
        private readonly Dictionary<StatusEffectName, BoolRequestFlag> _statusFlags = new();
        private readonly Dictionary<StatusEffectName, BaseStatusEffectRuntime> _activeEffects = new();

        private void Awake()
        {
            foreach (StatusEffectName status in System.Enum.GetValues(typeof(StatusEffectName)))
                _statusFlags[status] = new BoolRequestFlag();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            List<StatusEffectName> toRemove = new();

            foreach (var pair in _activeEffects)
            {
                var effect = pair.Value;
                effect.OnTick(gameObject, deltaTime);
                effect.duration -= deltaTime;
                if (!effect.IsDone) continue;
                toRemove.Add(pair.Key);
            }

            foreach (var status in toRemove)
                RemoveEffect(status);
        }

        public bool IsStatusActive(StatusEffectName statusEffect) => _statusFlags[statusEffect].IsActive;

        public void ApplyEffect(BaseStatusEffectRuntime effect)
        {
            StatusEffectName effectName = effect.EffectName;

            if (!_activeEffects.TryGetValue(effectName, out var existing)) return;
            if (existing.duration >= effect.duration) return;
            RemoveEffect(effectName); // Remove weaker effect
            
            _statusFlags[effectName].Request(effect);
            _activeEffects[effectName] = effect;
            effect.OnStart(gameObject);
        }

        public void RemoveEffect(StatusEffectName effectName)
        {
            if (!_activeEffects.TryGetValue(effectName, out var effect)) return;
            effect.OnEnd(gameObject);
            _statusFlags[effectName].Release(effect);
            _activeEffects.Remove(effectName);
        }
    }
}