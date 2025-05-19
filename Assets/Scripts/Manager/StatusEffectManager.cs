using System;
using System.Collections.Generic;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems;
using Characters.StatusEffectSystems.StatusEffects;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Static utility class for applying and removing status effects from GameObjects.
    /// Acts as a central API for managing status effect lifecycle at runtime.
    /// </summary>
    public static class StatusEffectManager
    {
        #region Methods
        
        /// <summary>
        /// Applies a single status effect to the specified GameObject using the given payload.
        /// Automatically determines duration (override or default) and instantiates the runtime class.
        /// Does nothing if the GameObject lacks a StatusEffectSystem.
        /// </summary>
        /// <param name="target">The GameObject to apply the effect to.</param>
        /// <param name="effectPayload">Payload containing effect data and optional override settings.</param>
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
        /// Applies multiple status effects to the specified GameObject.
        /// Internally calls <see cref="ApplyEffectTo(GameObject, StatusEffectDataPayload)"/> for each payload.
        /// </summary>
        /// <param name="target">The GameObject to apply the effects to.</param>
        /// <param name="effectPayloadList">A list of status effect payloads to apply.</param>
        public static void ApplyEffectTo(GameObject target, List<StatusEffectDataPayload> effectPayloadList)
        {
            foreach (var effectPayload in effectPayloadList)
                ApplyEffectTo(target, effectPayload);
        }

        /// <summary>
        /// Removes a specific status effect from the target GameObject.
        /// Skips if the effect is not active or the system is not present.
        /// </summary>
        /// <param name="target">The GameObject to remove the effect from.</param>
        /// <param name="effectName">The enum name of the effect to remove.</param>
        public static void RemoveEffectAt(GameObject target, StatusEffectName effectName)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveEffect(effectName);
        }

        /// <summary>
        /// Removes all active status effects from the target GameObject.
        /// Skips if there are no effects or the system is missing.
        /// </summary>
        /// <param name="target">The GameObject to clear all effects from.</param>
        public static void RemoveAllEffectAt(GameObject target)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveAllEffect();
        }
        
        #endregion
    }
}
