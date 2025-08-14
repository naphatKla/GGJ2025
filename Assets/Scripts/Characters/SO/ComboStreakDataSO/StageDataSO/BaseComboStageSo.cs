using System.Collections.Generic;
using System.Linq;
using Characters.ComboSystem;
using Characters.StatusEffectSystems;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.ComboStreakDataSO.StageDataSO
{
    /// <summary>
    /// Stage เชิงพฤติกรรมล้วน ๆ:
    /// - ไม่มี min threshold / duration / freeze / prevent ใด ๆ ในตัว
    /// - เกณฑ์เข้าและเงื่อนไขเชิงเวลา/ป้องกัน ถูกคุมโดย ComboStreakSystem (Manager) เท่านั้น
    /// - ใช้สำหรับใส่เอฟเฟกต์/ตรรกะเฉพาะตอนเข้า/ระหว่าง/ออก
    /// </summary>
    public abstract class BaseComboStageSo : ScriptableObject
    {
        [Title("Identity")]
        public string stageId = "flow_i";
        public string displayName = "Flow I";

        [Title("Status Effects (Optional)")]
        public List<StatusEffectDataPayload> effectOnEnter = new();
        public bool removeEffectOnExit = true;

        // ---- Hooks ----
        public virtual void OnEnter(StageContext ctx)
        {
            if (effectOnEnter != null && effectOnEnter.Count > 0)
                StatusEffectManager.ApplyEffectTo(ctx.System.gameObject, effectOnEnter);
        }

        public virtual void OnTick(StageContext ctx, float dt) { }

        public virtual void OnExit(StageContext ctx)
        {
            if (!removeEffectOnExit) return;
            if (effectOnEnter == null || effectOnEnter.Count == 0) return;

            foreach (var effectName in effectOnEnter.Select(e => e.EffectData.EffectName))
                StatusEffectManager.RemoveEffectAt(ctx.System.gameObject, effectName);
        }
    }

    /// <summary>บริบทสำหรับสเตจ</summary>
    public readonly struct StageContext
    {
        public readonly ComboStreakSystem System;
        public readonly int CurrentStreak;

        public StageContext(ComboStreakSystem sys, int streak)
        {
            System = sys;
            CurrentStreak = streak;
        }

        public void EmitStageEffect(string key, float v1 = 0, float v2 = 0, float v3 = 0)
            => System.EmitStageEffect(key, v1, v2, v3);
    }
}
