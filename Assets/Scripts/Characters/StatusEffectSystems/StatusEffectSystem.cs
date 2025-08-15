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
        [PropertyTooltip("The base ScriptableObject data for this status effect.")]
        [SerializeField] private BaseStatusEffectDataSo effectData;

        [PropertyTooltip("Whether to override the default duration defined in the effect data.")]
        [SerializeField, HorizontalGroup("OverrideDuration", LabelWidth = 105f)]
        [LabelText("OverrideDuration")] private bool isOverrideDuration;

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

    public enum StatusEffectName
    {
        Iframe = 0,
        Stun = 2,
        FlowStage = 3,
    }

    /// <summary>
    /// Safe, two-phase status effect runner:
    /// - ไม่แก้ไขดิกชันนารีระหว่าง iterate
    /// - replace เรียก OnExit ของตัวเก่าเสมอ
    /// - Exit เรียกครั้งเดียวด้วยคิว + HashSet
    /// </summary>
    public class StatusEffectSystem : MonoBehaviour, IFixedUpdateable
    {
        private readonly Dictionary<StatusEffectName, BaseStatusEffect> _active = new();

        // คิว Exit + เซ็ตกันซ้ำ
        private readonly Queue<BaseStatusEffect> _exitQueue = new();
        private readonly HashSet<BaseStatusEffect> _exitSet = new();

        // คำสั่งที่เกิดระหว่างกำลังอัปเดต (defer)
        private readonly List<BaseStatusEffect> _pendingAdd = new();
        private readonly List<StatusEffectName> _pendingRemove = new();

        private readonly List<BaseStatusEffect> _tmpIter = new(); // snapshot iterate

        private BaseController _owner;
        private bool _stepping; // กำลัง iterate อยู่ไหม

        public virtual void AssignData(BaseController owner) => _owner = owner;

        private void OnEnable()  => FixedUpdateManager.Instance.Register(this);
        private void OnDisable() => FixedUpdateManager.Instance.Unregister(this);

        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // ----- SNAPSHOT & STEP -----
            _stepping = true;
            _tmpIter.Clear();
            foreach (var kvp in _active) _tmpIter.Add(kvp.Value);

            for (int i = 0; i < _tmpIter.Count; i++)
            {
                var effect = _tmpIter[i];
                // อาจถูกคิว exit ไปแล้วในเฟสก่อนหน้า
                if (effect == null || !_active.TryGetValue(effect.EffectName, out var stillAlive) || stillAlive != effect)
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
                ProcessExitQueue(); // ทำเลยเพื่อให้เรียก OnExit ทันทีนอกเฟส
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
            ProcessExitQueue();
        }

        public bool TryGetEffect(StatusEffectName effectName, out BaseStatusEffect effect)
            => _active.TryGetValue(effectName, out effect);

        public void ResetStatusEffectSystem() => RemoveAllEffect();

        // ===== Internals =====

        private void AddInternal(BaseStatusEffect newEffect)
        {
            // ถ้ามีของเก่า → เปรียบเทียบความแรงแล้ว exit ของเก่าเสมอ (ถ้าจะแทน)
            if (_active.TryGetValue(newEffect.EffectName, out var old))
            {
                if (IsWeakerThan(newEffect, old)) return; // ของใหม่อ่อนกว่า → ไม่ทำอะไร

                EnqueueExit(old);
                _active.Remove(old.EffectName);
                ProcessExitQueue(); // ให้ OnExit ทำงานก่อน OnStart ของตัวใหม่
            }

            _active[newEffect.EffectName] = newEffect;
            newEffect.OnStart(_owner);
        }

        private void EnqueueExit(BaseStatusEffect effect)
        {
            if (effect == null) return;
            if (_exitSet.Add(effect)) // กัน enqueue ซ้ำ
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
                    _active.Remove(eff.EffectName); // เผื่อยังหลงเหลือ
                }
            }
        }

        private static bool IsWeakerThan(BaseStatusEffect newer, BaseStatusEffect older)
        {
            if (newer.Level < older.Level) return true;
            return newer.Level == older.Level && newer.CurrentDuration < older.CurrentDuration;
        }
    }
}
