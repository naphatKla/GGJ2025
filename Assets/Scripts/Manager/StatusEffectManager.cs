using Characters.StatusEffectSystem;
using Characters.StatusEffectSystem.StatusEffectRuntimes;
using UnityEngine;

namespace Manager
{
    public static class StatusEffectManager
    {
        public static void ApplyStatusEffect(GameObject target, BaseStatusEffectRuntime runtimeEffect)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.ApplyEffect(runtimeEffect);
        }

        public static void RemoveStatus(GameObject target, StatusEffectName effectName)
        {
            if (!target.TryGetComponent(out StatusEffectSystem targetSystem)) return;
            targetSystem.RemoveEffect(effectName);
        }

        public static bool IsStatusActive(GameObject target, StatusEffectName effectName)
        {
            return target.TryGetComponent(out StatusEffectSystem targetSystem) && targetSystem.IsStatusActive(effectName);
        }
    }
}