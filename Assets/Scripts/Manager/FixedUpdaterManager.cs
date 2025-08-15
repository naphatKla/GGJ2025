using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace Manager
{
    public interface IFixedUpdateable
    {
        void OnFixedUpdate();
    }

    /// <summary>
    /// FixedUpdate manager ที่ปลอดภัยต่อการสลับ Register/Unregister ภายในเฟรมเดียวกัน:
    /// - ใช้ Last-Op-Wins: รวมคำสั่ง (Enable/Disable) ของแต่ละ instance ในเฟรม แล้วค่อยตัดสินทีเดียวต้นเฟรม
    /// - ไม่แก้คอลเลกชันขณะ iterate
    /// - Remove แบบ O(1) ด้วย swap-remove + index map
    /// - กันอัปเดตให้ตัวที่ถูก "จองถอด" ภายในเฟรมนี้ และกันกรณี UnityEngine.Object ถูกทำลาย
    /// - รองรับ tick event ทุก ๆ tickInterval วินาที (อาจยิงหลายครั้งในเฟรมถ้า CPU ตก)
    /// </summary>
    public class FixedUpdateManager : MMSingleton<FixedUpdateManager>
    {
        // ===== Active storage (swap-remove) =====
        private readonly List<IFixedUpdateable> _items = new();
        private readonly Dictionary<IFixedUpdateable, int> _indexOf = new();

        // ===== Queues (apply ที่ต้นเฟรม) =====
        private readonly List<IFixedUpdateable> _toAdd = new();
        private readonly HashSet<IFixedUpdateable> _toAddSet = new();

        private readonly List<IFixedUpdateable> _toRemove = new();
        private readonly HashSet<IFixedUpdateable> _toRemoveSet = new();

        // ===== Last-Op-Wins (รวมคำสั่งในเฟรม) =====
        // true = อยากอยู่/Enable/Register, false = อยากออก/Disable/Unregister
        private readonly Dictionary<IFixedUpdateable, bool> _lastOp = new();

        // ===== Tick event =====
        public event Action OnTick;
        [SerializeField] private float tickInterval = 0.2f;
        private float _tickAccum;

        public float TickInterval
        {
            get => tickInterval;
            set => tickInterval = Mathf.Max(0.0001f, value);
        }

        public int Count => _items.Count;

        // ---------- Public API ----------

        /// <summary>ประกาศความต้องการ "อยู่ในลิสต์" ของ instance นี้ในเฟรมปัจจุบัน (Last-Op-Wins)</summary>
        public void Register(IFixedUpdateable instance)
        {
            if (instance == null) return;
            _lastOp[instance] = true;
        }

        /// <summary>ประกาศความต้องการ "ออกจากลิสต์" ของ instance นี้ในเฟรมปัจจุบัน (Last-Op-Wins)</summary>
        public void Unregister(IFixedUpdateable instance)
        {
            if (instance == null) return;
            _lastOp[instance] = false;
        }

        // ---------- Main Loop ----------
        private void FixedUpdate()
        {
            // 0) Reconcile last operations (Last-Op-Wins) -> translate เป็นคิว Add/Remove
            if (_lastOp.Count > 0)
            {
                foreach (var kv in _lastOp)
                {
                    var inst = kv.Key;
                    bool wantAdd = kv.Value;

                    if (!wantAdd)
                    {
                        // ต้องการถอด
                        if (_indexOf.ContainsKey(inst) && !_toRemoveSet.Contains(inst))
                        {
                            _toRemove.Add(inst);
                            _toRemoveSet.Add(inst);
                        }
                        // ยกเลิกคิวเพิ่มถ้าเผลอคิวไว้
                        if (_toAddSet.Remove(inst))
                        {
                            for (int i = 0; i < _toAdd.Count; i++)
                            {
                                if (ReferenceEquals(_toAdd[i], inst))
                                {
                                    _toAdd.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // ต้องการอยู่
                        if (!_indexOf.ContainsKey(inst) && !_toAddSet.Contains(inst))
                        {
                            _toAdd.Add(inst);
                            _toAddSet.Add(inst);
                        }
                        // ยกเลิกคิวลบถ้ามี
                        if (_toRemoveSet.Remove(inst))
                        {
                            for (int i = 0; i < _toRemove.Count; i++)
                            {
                                if (ReferenceEquals(_toRemove[i], inst))
                                {
                                    _toRemove.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }
                _lastOp.Clear();
            }

            // 1) Apply removals (O(1) swap-remove)
            if (_toRemove.Count > 0)
            {
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    var inst = _toRemove[i];
                    if (_indexOf.TryGetValue(inst, out int idx))
                        SwapRemoveAt(idx);
                }
                _toRemove.Clear();
                _toRemoveSet.Clear();
            }

            // 2) Apply additions (append)
            if (_toAdd.Count > 0)
            {
                for (int i = 0; i < _toAdd.Count; i++)
                {
                    var inst = _toAdd[i];
                    if (_indexOf.ContainsKey(inst)) continue; // safety (อาจถูกเพิ่มไปแล้ว)
                    _indexOf[inst] = _items.Count;
                    _items.Add(inst);
                }
                _toAdd.Clear();
                _toAddSet.Clear();
            }

            // 3) Update loop (skip ที่ถูก "จองถอด" + skip ถ้าถูก Destroy)
            var list = _items;
            for (int i = 0, len = list.Count; i < len; i++)
            {
                var item = list[i];
                if (item == null) continue; // เผื่ออินเตอร์เฟซลอย
                if (_toRemoveSet.Contains(item)) continue; // เฟรมนี้มีคำสั่งถอดแล้ว → ไม่ต้องอัปเดต

                // ถ้าเป็น UnityEngine.Object และถูกลบ/Destroy ไปแล้ว ให้ข้าม
                if (item is UnityEngine.Object uo && uo == null) continue;

                item.OnFixedUpdate();
            }

            // 4) Tick every tickInterval (อาจยิงซ้ำหลายครั้งถ้า accum เกิน)
            _tickAccum += Time.fixedDeltaTime;
            if (_tickAccum >= tickInterval)
            {
                int ticks = Mathf.FloorToInt(_tickAccum / tickInterval);
                _tickAccum -= ticks * tickInterval;

                var tick = OnTick;
                if (tick != null)
                {
                    for (int k = 0; k < ticks; k++)
                        tick.Invoke();
                }
            }
        }

        // ---------- Helpers ----------
        private void SwapRemoveAt(int idx)
        {
            int last = _items.Count - 1;
            var removed = _items[idx];

            if (idx != last)
            {
                var lastItem = _items[last];
                _items[idx] = lastItem;
                _indexOf[lastItem] = idx;
            }

            _items.RemoveAt(last);
            _indexOf.Remove(removed);
        }
    }
}
