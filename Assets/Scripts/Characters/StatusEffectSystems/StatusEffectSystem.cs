using System;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.StatusEffectSO;
using Characters.StatusEffectSystems.StatusEffects;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.StatusEffectSystems
{
    [Serializable]
    public struct StatusEffectDataPayload
    {
        [PropertyTooltip("The base ScriptableObject data for this status effect.")] [SerializeField]
        private BaseStatusEffectDataSo effectData;

        [PropertyTooltip("Whether to override the default duration defined in the effect data.")]
        [SerializeField, HorizontalGroup("OverrideDuration", LabelWidth = 105f)]
        [LabelText("OverrideDuration")]
        private bool isOverrideDuration;

        [EnableIf(nameof(isOverrideDuration))]
        [PropertyTooltip("Custom duration to override the default value. Used only if override is enabled.")]
        [SerializeField, HorizontalGroup("OverrideDuration"), HideLabel]
        private float overrideDuration;

        public BaseStatusEffectDataSo EffectData => effectData;
        public bool IsOverrideDuration => isOverrideDuration;
        public float OverrideDuration => overrideDuration;

        public void SetOverrideDuration(float newDuration)
        {
            if (!isOverrideDuration) return;
            overrideDuration = newDuration;
        }
    }

    public struct StatusEffectUIData
    {
        public StatusEffectName Name;
        public Sprite Icon;
        public bool isDebuff;
        public float MaxDuration;
        public float CurrentDuration;
    }

    public enum StatusEffectName
    {
        Iframe = 0,
        Stun = 2,
        FlowState = 3,
        IronBody = 4,
    }

    public class StatusEffectSystem : MonoBehaviour, IFixedUpdateable
    {
        private readonly Dictionary<StatusEffectName, BaseStatusEffect> _active = new();

        // เก็บ “ลำดับที่ถูก Add” ไว้คงลำดับเดิมของบัฟ/ดีบัฟ
        private readonly List<StatusEffectName> _order = new();

        private readonly Queue<BaseStatusEffect> _exitQueue = new();
        private readonly HashSet<BaseStatusEffect> _exitSet = new();

        private readonly List<BaseStatusEffect> _pendingAdd = new();
        private readonly List<StatusEffectName> _pendingRemove = new();
        private readonly List<BaseStatusEffect> _tmpIter = new();

        private BaseController _owner;
        private bool _stepping;

        public event Action<IReadOnlyList<StatusEffectUIData>> OnStatusUIUpdate;

        private readonly List<StatusEffectUIData> _uiBuffer = new(8);

        public virtual void AssignData(BaseController owner) => _owner = owner;

        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable() => FixedUpdateManager.Current?.Unregister(this);

        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // ----- STEP -----
            _stepping = true;
            _tmpIter.Clear();
            foreach (var kvp in _active) _tmpIter.Add(kvp.Value);

            for (int i = 0; i < _tmpIter.Count; i++)
            {
                var effect = _tmpIter[i];
                if (effect == null || !_active.TryGetValue(effect.EffectName, out var stillAlive) ||
                    stillAlive != effect)
                    continue;

                effect.OnUpdate(_owner, dt);
                effect.CurrentDuration -= dt;

                if (effect.IsDone)
                    EnqueueExit(effect);
            }

            _stepping = false;

            // ----- APPLY REMOVES -----
            ProcessExitQueue();

            // ----- APPLY DEFERRED REMOVES -----
            if (_pendingRemove.Count > 0)
            {
                for (int i = 0; i < _pendingRemove.Count; i++)
                {
                    var name = _pendingRemove[i];
                    if (_active.TryGetValue(name, out var eff))
                    {
                        EnqueueExit(eff);
                        _active.Remove(name);
                        RemoveFromOrder(name);
                    }
                }

                _pendingRemove.Clear();
                ProcessExitQueue();
            }

            // ----- APPLY DEFERRED ADDS -----
            if (_pendingAdd.Count > 0)
            {
                for (int i = 0; i < _pendingAdd.Count; i++)
                    AddInternal(_pendingAdd[i]);
                _pendingAdd.Clear();
            }

            // ----- BUILD UI BUFFER (Buff ก่อน / Debuff ท้าย) -----
            BuildUIBufferPartitioned();

            // ----- PUSH UI -----
            OnStatusUIUpdate?.Invoke(_uiBuffer);
        }

        // ===== Public API =====

        public void AddEffect(BaseStatusEffect newEffect)
        {
            if (newEffect == null) return;
            if (_stepping)
            {
                _pendingAdd.Add(newEffect);
                return;
            }

            AddInternal(newEffect);
        }

        public void RemoveEffect(StatusEffectName effectName)
        {
            if (_stepping)
            {
                _pendingRemove.Add(effectName);
                return;
            }

            if (_active.TryGetValue(effectName, out var eff))
            {
                EnqueueExit(eff);
                _active.Remove(effectName);
                RemoveFromOrder(effectName);
                ProcessExitQueue(); // OnExit ทันที
            }
        }

        [Button]
        private void TestAddEffect(StatusEffectDataPayload effectPayload)
        {
            StatusEffectManager.ApplyEffectTo(gameObject, effectPayload);
        }

        public void RemoveAllEffect()
        {
            foreach (var kvp in _active) EnqueueExit(kvp.Value);
            _active.Clear();
            _order.Clear();
            ProcessExitQueue();
        }

        public bool TryGetEffect(StatusEffectName effectName, out BaseStatusEffect effect)
            => _active.TryGetValue(effectName, out effect);

        public void ResetStatusEffectSystem() => RemoveAllEffect();

        // ===== Internals =====

        private bool AddInternal(BaseStatusEffect newEffect)
        {
            // ถ้ามีของเก่า → เปรียบเทียบความแรง
            if (_active.TryGetValue(newEffect.EffectName, out var old))
            {
                if (IsWeakerThan(newEffect, old)) return false;

                // replace: ออกจากของเก่าก่อน (คงตำแหน่งใน _order ไว้)
                EnqueueExit(old);
                _active.Remove(old.EffectName);
                ProcessExitQueue();
                // _order ไม่ต้องขยับ เพราะชื่อ effect เหมือนเดิม
            }
            else
            {
                // effect ใหม่นับเป็น entry ใหม่ → put ไว้ท้ายสุด
                _order.Add(newEffect.EffectName);
            }

            _active[newEffect.EffectName] = newEffect;
            newEffect.OnStart(_owner);
            return true;
        }

        private void EnqueueExit(BaseStatusEffect effect)
        {
            if (effect == null) return;
            if (_exitSet.Add(effect))
                _exitQueue.Enqueue(effect);
        }

        private void ProcessExitQueue()
        {
            while (_exitQueue.Count > 0)
            {
                var eff = _exitQueue.Dequeue();
                _exitSet.Remove(eff);
                try
                {
                    eff.OnExit(_owner);
                }
                finally
                {
                    _active.Remove(eff.EffectName);
                    RemoveFromOrder(eff.EffectName); // ลบออกจากลำดับด้วย
                }
            }
        }

        private void RemoveFromOrder(StatusEffectName name)
        {
            // O(N) แต่ N มักน้อย (หลักสิบ) — พอใช้และเรียบง่าย
            int idx = _order.IndexOf(name);
            if (idx >= 0) _order.RemoveAt(idx);
        }

        private static bool IsWeakerThan(BaseStatusEffect newer, BaseStatusEffect older)
        {
            if (newer.Level < older.Level) return true;
            return newer.Level == older.Level && newer.CurrentDuration < older.CurrentDuration;
        }

        // ===== UI buffer (No sort by name — แยกสองพาส) =====
        private void BuildUIBufferPartitioned()
        {
            _uiBuffer.Clear();

            // 1) ใส่ “บัฟ” ทั้งหมดก่อน ตามลำดับที่ถูก Add เข้ามา
            for (int i = 0; i < _order.Count; i++)
            {
                var key = _order[i];
                if (!_active.TryGetValue(key, out var eff) || eff == null) continue;

                var data = eff.DataSo;
                bool isDebuff = data != null && data.IsDebuff;
                if (isDebuff) continue; // ข้ามก่อน (ไว้ไปรอบถัดไป)

                _uiBuffer.Add(new StatusEffectUIData
                {
                    Name = eff.EffectName,
                    Icon = data ? data.Icon : null,
                    isDebuff = data.IsDebuff,
                    MaxDuration = eff.MaxDuration,
                    CurrentDuration = eff.CurrentDuration
                });
            }

            // 2) ตามด้วย “ดีบัฟ” ทั้งหมด ตามลำดับที่ถูก Add เข้ามา
            for (int i = 0; i < _order.Count; i++)
            {
                var key = _order[i];
                if (!_active.TryGetValue(key, out var eff) || eff == null) continue;

                var data = eff.DataSo;
                bool isDebuff = data != null && data.IsDebuff;
                if (!isDebuff) continue;

                _uiBuffer.Add(new StatusEffectUIData
                {
                    Name = eff.EffectName,
                    Icon = data ? data.Icon : null,
                    isDebuff = data.IsDebuff,
                    MaxDuration = eff.MaxDuration,
                    CurrentDuration = eff.CurrentDuration
                });
            }
        }
    }

    // (ยังคง ListPool ได้ถ้าจะใช้ที่อื่น — ที่นี่ไม่ใช้แล้ว)
    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new();
        public static List<T> Get() => _pool.Count > 0 ? _pool.Pop() : new List<T>(8);

        public static void Release(List<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }
    }
}