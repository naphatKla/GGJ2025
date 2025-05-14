using System;
using System.Collections.Generic;
using Characters.StatusEffectSystem;
using Characters.StatusEffectSystem.StatusEffectRuntimes;
using Characters.SO.StatusEffectMetaSO;
using UnityEngine;

namespace Manager
{
    public static class StatusEffectManager
    {
        private static readonly Dictionary<StatusEffectName, StatusEffectMetaDataSo> AllEffectMetaData = new();

        public static void Initialize(List<StatusEffectMetaDataSo> allMeta)
        {
            AllEffectMetaData.Clear();
            foreach (var meta in allMeta)
                AllEffectMetaData[meta.EffectName] = meta;
        }

        public static void ApplyStatusEffect(GameObject target, StatusEffectName name, Dictionary<string, object> parameters)
        {
            if (!AllEffectMetaData.TryGetValue(name, out var meta))
            {
                Debug.LogError($"[StatusEffectManager] Meta not found for {name}");
                return;
            }

            if (!(Activator.CreateInstance(meta.Runtime) is BaseStatusEffectRuntime runtime))
            {
                Debug.LogError($"[StatusEffectManager] Could not create runtime for {name}");
                return;
            }

            foreach (var kv in parameters)
            {
                var field = runtime.GetType().GetField(kv.Key);
                if (field != null) field.SetValue(runtime, kv.Value);
            }

            if (!target.TryGetComponent(out StatusEffectSystem system)) return;
            system.ApplyEffect(runtime, meta);
        }

        public static void RemoveStatus(GameObject target, StatusEffectName effectName)
        {
            if (!target.TryGetComponent(out StatusEffectSystem system)) return;
            system.RemoveEffect(effectName);
        }

        public static bool IsStatusActive(GameObject target, StatusEffectName effectName)
        {
            return target.TryGetComponent(out StatusEffectSystem system) && system.IsStatusActive(effectName);
        }
    }
}