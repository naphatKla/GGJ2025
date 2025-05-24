using System;
using Characters.StatusEffectSystems;
using Characters.StatusEffectSystems.StatusEffects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    /// <summary>
    /// Abstract ScriptableObject that defines the metadata and configuration 
    /// for a status effect, including visuals, description, default values, 
    /// and the runtime class it will instantiate.
    /// </summary>
    public abstract class BaseStatusEffectDataSo : SerializedScriptableObject
    {
        #region Inspector & Variables
        
        [Title("Status Effect Preview")]
        [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 128)]
        [HideLabel, Space(10)]
        [PropertyTooltip("Icon representing this status effect in UI.")]
        [SerializeField]
        private Sprite icon;

        [Title("Details")]
        [PropertyTooltip("Enum identifier for this status effect.")]
        [SerializeField]
        private StatusEffectName effectName;

        [PropertyTooltip("In-editor description of what this status effect does.")]
        [MultiLineProperty]
        [SerializeField]
        private string description;

        [PropertyTooltip("Default duration in seconds when this effect is applied.")]
        [SerializeField]
        private float defaultDuration;

        [PropertyTooltip("Level of the effect used for override priority comparison. Range is 1 (weak) to 4 (strong).")]
        [Range(1, 4)]
        [SerializeField]
        private int level = 1;

        [Title("Type Binding"), Space(10)]
        [PropertyTooltip("Runtime class that will be instantiated when this effect is applied.")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseStatusEffect))]
        private Type _effectType;

        /// <summary>
        /// Icon used for displaying this effect in UI.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Enum identifier used for mapping and activation.
        /// </summary>
        public StatusEffectName EffectName => effectName;

        /// <summary>
        /// Text description for editor/debug reference.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Default duration (in seconds) for the status effect.
        /// </summary>
        public float DefaultDuration => defaultDuration;

        /// <summary>
        /// Priority level of this effect, used in override logic.
        /// </summary>
        public int Level => level;

        /// <summary>
        /// Runtime type that implements the behavior of this effect.
        /// Must inherit from BaseStatusEffect.
        /// </summary>
        public Type EffectType => _effectType;
        
        #endregion
    }
}
