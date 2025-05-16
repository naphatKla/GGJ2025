using System;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystem;
using Characters.StatusEffectSystem.StatusEffects;
using UnityEngine;

namespace Manager
{
    public static class StatusEffectManager
    {
        public static void ApplyEffectTo(GameObject target, StatusEffectDataPayload effectPayload)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            BaseStatusEffectDataSo effectData = effectPayload.EffectData;
            BaseStatusEffect newEffect = (BaseStatusEffect)Activator.CreateInstance(effectData.EffectType);
            float effectDuration = effectPayload.IsOverrideDuration
                ? effectPayload.OverrideDuration
                : effectData.DefaultDuration;
            newEffect.AssignEffectData(effectData.EffectName, effectDuration, effectData.Level);
            targetSystem.AddEffect(newEffect);
        }

        public static void RemoveEffectAt(GameObject target, StatusEffectName effectName)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveEffect(effectName);
        }

        public static void RemoveAllEffectAt(GameObject target)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveAllEffect();
        }
    }
}