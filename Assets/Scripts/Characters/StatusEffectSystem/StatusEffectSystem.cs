using System;
using System.Collections.Generic;
using Characters.SO.StatusEffectMetaSO;
using Characters.StatusEffectSystem.DrawerSettings;
using Characters.StatusEffectSystem.StatusEffects;
using ProjectExtensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.StatusEffectSystem
{
    public enum StatusEffectName
    {
        Iframe = 0,
        Slow = 1,
        Stun = 2,
        // Add more statuses here
    }
    
    [Serializable]
    public class SkillStatusEffectConfig
    {
        [OnValueChanged(nameof(UpdateEffectInstance))]
        [LabelText("Effect Meta")]
        public StatusEffectMetaDataSo meta;

        [ShowIf(nameof(meta)), SerializeReference, InlineProperty]
        [StatusEffectParamDrawer]
        public BaseStatusEffect effectParams;

        private void UpdateEffectInstance()
        {
#if UNITY_EDITOR
            if (meta == null || meta.ClassType == null) return;

            if (effectParams == null || effectParams.GetType() != meta.ClassType)
            {
                if (typeof(BaseStatusEffect).IsAssignableFrom(meta.ClassType))
                {
                    effectParams = (BaseStatusEffect)Activator.CreateInstance(meta.ClassType);
                }
            }
#endif
        }
    }
    
   public class StatusEffectSystem : MonoBehaviour
    {
        private readonly Dictionary<StatusEffectName, BoolRequestFlag> _statusFlags = new();
        private readonly Dictionary<StatusEffectName, BaseStatusEffect> _activeEffects = new();
        private readonly Dictionary<StatusEffectName, StatusEffectMetaDataSo> _activeEffectMetaData = new();

        private void Awake()
        {
            foreach (StatusEffectName status in Enum.GetValues(typeof(StatusEffectName)))
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
                effect.currentDuration -= deltaTime;
                if (!effect.IsDone) continue;
                toRemove.Add(pair.Key);
            }

            foreach (var status in toRemove)
                RemoveEffect(status);
        }

        public bool IsStatusActive(StatusEffectName statusEffect) => _statusFlags[statusEffect].IsActive;

        public void ApplyEffect(BaseStatusEffect effect, StatusEffectMetaDataSo meta)
        {
            StatusEffectName effectName = meta.EffectName;

            if (_activeEffects.TryGetValue(effectName, out var existing))
            {
                if (existing.currentDuration >= effect.currentDuration) return;
                RemoveEffect(effectName);
            }

            _statusFlags[effectName].Request(effect);
            _activeEffects[effectName] = effect;
            _activeEffectMetaData[effectName] = meta;
            effect.OnStart(gameObject);
        }

        public void RemoveEffect(StatusEffectName effectName)
        {
            if (!_activeEffects.TryGetValue(effectName, out var effect)) return;
            effect.OnEnd(gameObject);
            _statusFlags[effectName].Release(effect);
            _activeEffects.Remove(effectName);
            _activeEffectMetaData.Remove(effectName);
        }

        public StatusEffectMetaDataSo GetMetaData(StatusEffectName effectName)
        {
            return _activeEffectMetaData.GetValueOrDefault(effectName);
        }
    }
}