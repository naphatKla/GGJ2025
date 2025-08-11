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

    public class FixedUpdateManager : MMSingleton<FixedUpdateManager>
    {
        // -------- Core storage (remove O(1)) --------
        private readonly List<IFixedUpdateable> _items = new();                 // active list
        private readonly Dictionary<IFixedUpdateable, int> _indexOf = new();    // map -> index

        // Queues (avoid duplicates)
        private readonly List<IFixedUpdateable> _toAdd = new();
        private readonly HashSet<IFixedUpdateable> _toAddSet = new();
        private readonly List<IFixedUpdateable> _toRemove = new();
        private readonly HashSet<IFixedUpdateable> _toRemoveSet = new();

        // -------- 0.2s tick event --------
        public event Action OnTick;
        [SerializeField] private float tickInterval = 0.2f;
        private float _tickAccum;

        public float TickInterval
        {
            get => tickInterval;
            set => tickInterval = Mathf.Max(0.0001f, value);
        }

        public int Count => _items.Count;

        // -------- Public API --------
        public void Register(IFixedUpdateable instance)
        {
            if (instance == null) return;

            if (_indexOf.ContainsKey(instance) || _toAddSet.Contains(instance))
                return;

            // If it was scheduled to remove, cancel that removal
            if (_toRemoveSet.Remove(instance))
            {
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    if (!ReferenceEquals(_toRemove[i], instance)) continue;
                    _toRemove.RemoveAt(i);
                    break;
                }
                return;
            }

            _toAdd.Add(instance);
            _toAddSet.Add(instance);
        }

        public void Unregister(IFixedUpdateable instance)
        {
            if (instance == null) return;

            // If not added yet (in add-queue), cancel add instead
            if (_toAddSet.Remove(instance))
            {
                for (int i = 0; i < _toAdd.Count; i++)
                {
                    if (!ReferenceEquals(_toAdd[i], instance)) continue;
                    _toAdd.RemoveAt(i);
                    break;
                }
                return;
            }

            if (!_indexOf.ContainsKey(instance) || _toRemoveSet.Contains(instance))
                return;

            _toRemove.Add(instance);
            _toRemoveSet.Add(instance);
        }

        private void FixedUpdate()
        {
            // 1) Apply removals (swap-remove, O(1) per item)
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

            // 2) Apply additions (append only)
            if (_toAdd.Count > 0)
            {
                for (int i = 0; i < _toAdd.Count; i++)
                {
                    var inst = _toAdd[i];
                    if (_indexOf.ContainsKey(inst)) continue; // safety
                    _indexOf[inst] = _items.Count;
                    _items.Add(inst);
                }
                _toAdd.Clear();
                _toAddSet.Clear();
            }

            // 3) Update loop (tight for-loop)
            var list = _items;
            for (int i = 0, len = list.Count; i < len; i++)
            {
                list[i]?.OnFixedUpdate();
            }

            // 4) Tick every tickInterval (may fire multiple times if needed)
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

        // ---- Helpers ----
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
