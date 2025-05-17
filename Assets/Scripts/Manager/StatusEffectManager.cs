using System;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystem;
using Characters.StatusEffectSystem.StatusEffects;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Static utility class for applying and removing status effects from GameObjects.
    /// Acts as a central API for managing status effect lifecycle at runtime.
    /// </summary>
    public static class StatusEffectManager
    {
        /// <summary>
        /// Applies a status effect to the specified GameObject using the given payload.
        /// If the target does not contain a StatusEffectSystem, the operation is skipped.
        /// </summary>
        /// <param name="target">The GameObject to apply the effect to.</param>
        /// <param name="effectPayload">Payload containing effect data and override settings.</param>
        public static void ApplyEffectTo(GameObject target, StatusEffectDataPayload effectPayload)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;

            BaseStatusEffectDataSo effectData = effectPayload.EffectData;

            // Create new runtime instance of the effect using reflection
            BaseStatusEffect newEffect = (BaseStatusEffect)Activator.CreateInstance(effectData.EffectType);

            float effectDuration = effectPayload.IsOverrideDuration
                ? effectPayload.OverrideDuration
                : effectData.DefaultDuration;

            newEffect.AssignEffectData(effectData, effectDuration);
            targetSystem.AddEffect(newEffect);
        }

        /// <summary>
        /// Removes a specific status effect from the target GameObject.
        /// If the effect is not active or the system is missing, nothing happens.
        /// </summary>
        /// <param name="target">The GameObject to remove the effect from.</param>
        /// <param name="effectName">The name (enum) of the effect to remove.</param>
        public static void RemoveEffectAt(GameObject target, StatusEffectName effectName)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveEffect(effectName);
        }

        /// <summary>
        /// Removes all active status effects from the target GameObject.
        /// If no effects are active or the system is missing, nothing happens.
        /// </summary>
        /// <param name="target">The GameObject to clear effects from.</param>
        public static void RemoveAllEffectAt(GameObject target)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveAllEffect();
        }
    }
}
