using System;
using System.Collections.Generic;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems.StatusEffects;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.StatusEffectSystems
{
    /// <summary>
    /// Represents the data payload required to apply a status effect.
    /// Includes effect data and optional duration override.
    /// </summary>
    [Serializable]
    public struct StatusEffectDataPayload
    {
        #region Inspector & Variables
        
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

        /// <summary>
        /// Gets the ScriptableObject data of the status effect.
        /// </summary>
        public BaseStatusEffectDataSo EffectData => effectData;

        /// <summary>
        /// Gets a value indicating whether to override the default duration.
        /// </summary>
        public bool IsOverrideDuration => isOverrideDuration;

        /// <summary>
        /// Gets the overridden duration for the effect if enabled.
        /// </summary>
        public float OverrideDuration => overrideDuration;

        public void SetOverrideDuration(float newDuration)
        { 
            if (!isOverrideDuration) return;
            overrideDuration = newDuration;
        }
        #endregion
    }

    /// <summary>
    /// Enum representing all status effect types used in the game.
    /// </summary>
    public enum StatusEffectName
    {
        Iframe = 0,
        //DamageOnTouch = 1,
        Stun = 2,
        // Add more statuses here
    }

    /// <summary>
    /// Manages active status effects on a character. 
    /// Handles update cycle, priority checks, and safe effect removal.
    /// </summary>
    public class StatusEffectSystem : MonoBehaviour
    {
        #region Inspector & Variables
        
        /// <summary>
        /// The dictionary of currently active effects mapped by their enum identifier.
        /// </summary>
        private Dictionary<StatusEffectName, BaseStatusEffect> _activeEffects = new Dictionary<StatusEffectName, BaseStatusEffect>();

        /// <summary>
        /// Queue for deferred removal of completed effects to avoid modifying collection during iteration.
        /// </summary>
        private readonly Queue<BaseStatusEffect> _toRemovesQueue = new Queue<BaseStatusEffect>();

        #endregion

        #region Unity Methods
        
        /// <summary>
        /// Updates all active effects and schedules removal for completed ones.
        /// </summary>
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
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Adds or replaces an effect on the character. 
        /// If an existing effect is weaker, it will be overridden.
        /// </summary>
        /// <param name="newEffect">The new status effect to apply.</param>
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
        
        /// <summary>
        /// Removes a specific status effect from the character.
        /// </summary>
        /// <param name="effectName">The enum name of the effect to remove.</param>
        [Button]
        public void RemoveEffect(StatusEffectName effectName)
        {
            if (!TryGetEffect(effectName, out BaseStatusEffect effect)) return;
            _toRemovesQueue.Enqueue(effect);
            _activeEffects.Remove(effectName);
        }

        /// <summary>
        /// Queues all active effects for removal at the end of the frame.
        /// </summary>
        public void RemoveAllEffect()
        {
            foreach (var kvp in _activeEffects)
                _toRemovesQueue.Enqueue(kvp.Value);
        }

        /// <summary>
        /// Attempts to get an active effect by its name.
        /// </summary>
        /// <param name="effectName">The enum name of the effect.</param>
        /// <param name="effect">The resulting effect if found.</param>
        /// <returns>True if the effect exists, false otherwise.</returns>
        public bool TryGetEffect(StatusEffectName effectName, out BaseStatusEffect effect)
        {
            return _activeEffects.TryGetValue(effectName, out effect);
        }
        
        /// <summary>
        /// Compares two effects and determines if the new one is weaker.
        /// </summary>
        /// <param name="newEffect">The new effect to compare.</param>
        /// <param name="oldEffect">The existing active effect.</param>
        /// <returns>True if the new effect is weaker and should not replace the old one.</returns>
        private bool IsWeakerThan(BaseStatusEffect newEffect, BaseStatusEffect oldEffect)
        {
            if (newEffect.Level < oldEffect.Level) return true;
            return newEffect.Level == oldEffect.Level && newEffect.CurrentDuration < oldEffect.CurrentDuration;
        }
        
        /// <summary>
        /// Resets the status effect system by removing all currently applied effects.
        /// Typically used during full character resets such as respawn or revive.
        /// </summary>
        public void ResetStatusEffectSystem()
        {
            RemoveAllEffect();
        }
        
        #endregion
    }
}
