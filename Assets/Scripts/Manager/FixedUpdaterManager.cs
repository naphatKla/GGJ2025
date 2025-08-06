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
        private readonly List<IFixedUpdateable> _instances = new();
        private readonly List<IFixedUpdateable> _toAdd = new();
        private readonly List<IFixedUpdateable> _toRemove = new();

        public void Register(IFixedUpdateable instance)
        {
            if (!_instances.Contains(instance) && !_toAdd.Contains(instance))
                _toAdd.Add(instance);
        }

        public void Unregister(IFixedUpdateable instance)
        {
            if (!_toRemove.Contains(instance))
                _toRemove.Add(instance);
        }

        private void FixedUpdate()
        {
            // 1. Safe Update
            foreach (var instance in _instances)
            {
                instance.OnFixedUpdate();
            }

            // 2. Apply Removals
            if (_toRemove.Count > 0)
            {
                foreach (var instance in _toRemove)
                {
                    _instances.Remove(instance);
                }

                _toRemove.Clear();
            }

            // 3. Apply Additions
            if (_toAdd.Count > 0)
            {
                foreach (var instance in _toAdd)
                {
                    if (!_instances.Contains(instance))
                        _instances.Add(instance);
                }

                _toAdd.Clear();
            }
        }
    }
}