using System;
using System.Collections.Generic;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystem.StatusEffects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.StatusEffectSystem
{
    [Serializable]
    public class StatusEffectDataPayload
    {
        [SerializeField] private BaseStatusEffectDataSo effectData;
        [SerializeField] private bool isOverrideDuration;
        [EnableIf(nameof(isOverrideDuration))]
        [SerializeField] private float overrideDuration;

        public BaseStatusEffectDataSo EffectData => effectData;
        public bool IsOverrideDuration => isOverrideDuration;
        public float OverrideDuration => overrideDuration;
    }

    public enum StatusEffectName
    {
        Iframe = 0,
        Slow = 1,
        Stun = 2,
        // Add more statuses here
    }

    public class StatusEffectSystem : MonoBehaviour
    {
        private Dictionary<StatusEffectName, BaseStatusEffect> _activeEffects = new Dictionary<StatusEffectName, BaseStatusEffect>();
        private readonly Queue<BaseStatusEffect> _toRemovesQueue = new Queue<BaseStatusEffect>();

        private void LateUpdate()
        {
            foreach (var kvp in _activeEffects)
            {
                var effect = kvp.Value;
                effect.OnUpdate(Time.deltaTime);
                effect.CurrentDuration -= Time.deltaTime;

                if (!effect.IsDone) continue;
                _toRemovesQueue.Enqueue(effect);
            }

            while (_toRemovesQueue.Count > 0)
            {
                var effect = _toRemovesQueue.Dequeue();
                effect.OnExit();
                _activeEffects.Remove(effect.EffectName);
            }
        }

        // Call from manager
        public void AddEffect(BaseStatusEffect newEffect)
        {
            if (_activeEffects.TryGetValue(newEffect.EffectName, out var oldEffect))
            {
                if (IsWeakerThan(newEffect, oldEffect)) return;
                _activeEffects.Remove(oldEffect.EffectName);
            }

            _activeEffects[newEffect.EffectName] = newEffect;
            newEffect.OnStart(gameObject);
        }
        
        // Call from manager
        public void RemoveEffect(StatusEffectName effectName)
        {
            if (!TryGetEffect(effectName, out BaseStatusEffect effect)) return;
            _toRemovesQueue.Enqueue(effect);
            _activeEffects.Remove(effectName);
        }

        public void RemoveAllEffect()
        {
            foreach (var kvp in _activeEffects)
                _toRemovesQueue.Enqueue(kvp.Value);
        }

        public bool TryGetEffect(StatusEffectName effectName, out BaseStatusEffect effect)
        {
            return _activeEffects.TryGetValue(effectName, out effect);
        }
        
        private bool IsWeakerThan(BaseStatusEffect newEffect, BaseStatusEffect oldEffect)
        {
            if (newEffect.Level < oldEffect.Level) return true;
            return newEffect.Level == oldEffect.Level && newEffect.CurrentDuration < oldEffect.CurrentDuration;
        }

    }
}
