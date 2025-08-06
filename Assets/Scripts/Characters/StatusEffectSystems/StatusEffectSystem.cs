using System;
using System.Collections.Generic;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems.StatusEffects;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.StatusEffectSystems
{
    [Serializable]
    public struct StatusEffectDataPayload
    {
        [PropertyTooltip("The base ScriptableObject data for this status effect.")]
        [SerializeField]
        private BaseStatusEffectDataSo effectData;

        [PropertyTooltip("Whether to override the default duration defined in the effect data.")]
        [SerializeField] [HorizontalGroup("OverrideDuration", LabelWidth = 105f)]
        [LabelText("OverrideDuration")]
        private bool isOverrideDuration;

        [EnableIf(nameof(isOverrideDuration))]
        [PropertyTooltip("Custom duration to override the default value. Used only if override is enabled.")]
        [SerializeField] [HorizontalGroup("OverrideDuration")] [HideLabel]
        private float overrideDuration;

        public BaseStatusEffectDataSo EffectData => effectData;
        public bool IsOverrideDuration => isOverrideDuration;
        public float OverrideDuration => overrideDuration;

        public void SetOverrideDuration(float newDuration)
        { 
            if (!isOverrideDuration) return;
            overrideDuration = newDuration;
        }
    }

    public enum StatusEffectName
    {
        Iframe = 0,
        Stun = 2,
    }

    public class StatusEffectSystem : MonoBehaviour, IFixedUpdateable
    {
        private Dictionary<StatusEffectName, BaseStatusEffect> _activeEffects = new();
        private readonly Queue<BaseStatusEffect> _toRemovesQueue = new();

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            FixedUpdateManager.Instance.Unregister(this);
        }

        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            foreach (var kvp in _activeEffects)
            {
                var effect = kvp.Value;
                effect.OnUpdate(dt);
                effect.CurrentDuration -= dt;

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

        [Button]
        private void TestAddEffect(StatusEffectDataPayload effectPayload)
        {
            StatusEffectManager.ApplyEffectTo(gameObject, effectPayload);
        }

        [Button]
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

        public void ResetStatusEffectSystem()
        {
            RemoveAllEffect();
        }
    }
}