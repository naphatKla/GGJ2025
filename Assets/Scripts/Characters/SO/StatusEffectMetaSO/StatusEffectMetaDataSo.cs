#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Linq;
using Characters.StatusEffectSystem;
using Characters.StatusEffectSystem.StatusEffects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Characters.SO.StatusEffectMetaSO
{
    [CreateAssetMenu(fileName = "StatusEffectMetaData", menuName = "GameData/StatusEffectMetaData")]
    public class StatusEffectMetaDataSo : SerializedScriptableObject
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private StatusEffectName effectName;
        [SerializeField] [TextArea] private string description;

        [Title("Type Binding")]
        [PropertyTooltip(" ")]
        [ShowInInspector, OdinSerialize, PropertyOrder(10000)]
        [TypeDrawerSettings(BaseType = typeof(BaseStatusEffect))]
        private Type _classType;

        public Sprite Icon => icon;
        public StatusEffectName EffectName => effectName;
        public string Description => description;
        public Type ClassType => _classType;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var allAssets = AssetDatabase.FindAssets("t:StatusEffectMetaDataSo")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StatusEffectMetaDataSo>)
                .Where(meta => meta != null && meta != this);

            foreach (var meta in allAssets)
            {
                if (meta.EffectName == EffectName)
                {
                    Debug.LogError($"[DUPLICATE ENUM] {EffectName} already used in: {AssetDatabase.GetAssetPath(meta)}");
                }

                if (meta.ClassType == ClassType && ClassType != null)
                {
                    Debug.LogError($"[DUPLICATE RUNTIME] {ClassType.Name} already used in: {AssetDatabase.GetAssetPath(meta)}");
                }
            }
        }
#endif
    }
}