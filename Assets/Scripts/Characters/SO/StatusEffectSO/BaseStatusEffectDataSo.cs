using System;
using Characters.StatusEffectSystem;
using Characters.StatusEffectSystem.StatusEffects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.StatusEffectSO
{
    [CreateAssetMenu(fileName = "StatusEffect", menuName = "GameData/StatusEffect/Base")]
    public abstract class BaseStatusEffectDataSo : SerializedScriptableObject
    {
        [Title("Status Effect Preview")]
        [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 128)]
        [HideLabel, Space(10)]
        [SerializeField] private Sprite icon;

        [Title("Details")]
        [SerializeField] private StatusEffectName effectName;

        [MultiLineProperty]
        [SerializeField] private string description;
        
        [SerializeField] private float defaultDuration;

        [Range(1, 4)]
        [SerializeField] private int level;

        [Space(10)]
        [Title("Type Binding")]
        [PropertyTooltip("Runtime class that will be instantiated when this effect is applied.")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseStatusEffect))]
        private Type _effectType;

        public Sprite Icon => icon;
        public StatusEffectName EffectName => effectName;
        public string Description => description;
        public float DefaultDuration => defaultDuration;
        public int Level => level;
        public Type EffectType => _effectType;
    }
}