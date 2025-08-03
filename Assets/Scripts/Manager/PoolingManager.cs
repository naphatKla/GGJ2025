using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using MoreMountains.Tools;

namespace Manager
{
    public static class PoolingGroupName
    {
        public static string VFX => nameof(VFX);
        public static string Enemy => nameof(Enemy);
        public static string SkillObject => nameof(SkillObject);
        public static string Item => nameof(Item);
    }

    public class PoolingManager : MMSingleton<PoolingManager>
    {
        private readonly Dictionary<string, object> _typedPools = new();
        private readonly Dictionary<string, Transform> _parentFolders = new();
        private Transform _root;

        protected override void Awake()
        {
            base.Awake();
            _root = new GameObject("[Pool]").transform;
            _root.SetParent(transform);
        }

        // -------- Public API --------

        public void Create<T>(
            string nameOrID,
            Func<T> createFunc,
            string groupName = "Default",
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            bool collectionCheck = false,
            int defaultCapacity = 10,
            int maxSize = 100
        ) where T : Component
        {
            if (string.IsNullOrEmpty(nameOrID) || createFunc == null)
            {
                Debug.LogError("[PoolingManager] Cannot create pool. Key or createFunc is null.");
                return;
            }

            if (_typedPools.ContainsKey(nameOrID))
                return;

            Transform parent = GetOrCreateParent(nameOrID, groupName);

            var pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    var obj = createFunc();
                    obj.transform.SetParent(parent);
                    obj.gameObject.SetActive(false);
                    return obj;
                },
                actionOnGet: obj =>
                {
                    obj.gameObject.SetActive(true);
                    onGet?.Invoke(obj);
                },
                actionOnRelease: obj =>
                {
                    obj.gameObject.SetActive(false);
                    onRelease?.Invoke(obj);
                },
                actionOnDestroy: obj =>
                {
                    onDestroy?.Invoke(obj);
                    Destroy(obj.gameObject);
                },
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _typedPools[nameOrID] = pool;
        }

        public T Get<T>(string nameOrID) where T : Component
        {
            if (_typedPools.TryGetValue(nameOrID, out var obj) && obj is ObjectPool<T> pool)
            {
                return pool.Get();
            }

            Debug.LogError($"[PoolingManager] Pool for key '{nameOrID}' has not been created.");
            return null;
        }

        public void Release<T>(string nameOrID, T instance) where T : Component
        {
            if (instance == null)
            {
                Debug.LogWarning("[PoolingManager] Release failed. Instance is null.");
                return;
            }

            if (_typedPools.TryGetValue(nameOrID, out var obj) && obj is ObjectPool<T> pool)
            {
                pool.Release(instance);
            }
            else
            {
                Debug.LogWarning($"[PoolingManager] Unknown pool key '{nameOrID}'. Destroying {instance.name}");
                Destroy(instance.gameObject);
            }
        }

        public void ClearPool(string nameOrID)
        {
            if (_typedPools.TryGetValue(nameOrID, out var poolObj))
            {
                if (poolObj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _typedPools.Remove(nameOrID);

                if (_parentFolders.TryGetValue(nameOrID, out var folder))
                {
                    Destroy(folder.gameObject);
                    _parentFolders.Remove(nameOrID);
                }
            }
        }

        public void ClearGroup(string groupName)
        {
            var toRemove = new List<string>();
            foreach (var pair in _parentFolders)
            {
                if (pair.Value.parent.name == groupName)
                {
                    toRemove.Add(pair.Key);
                }
            }

            foreach (var key in toRemove)
            {
                ClearPool(key);
            }
        }

        public void ClearAllPools()
        {
            foreach (var poolObj in _typedPools.Values)
            {
                if (poolObj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _typedPools.Clear();

            foreach (var folder in _parentFolders.Values)
            {
                if (folder != null)
                    Destroy(folder.gameObject);
            }

            _parentFolders.Clear();
        }

        // -------- Internal --------

        private Transform GetOrCreateParent(string key, string groupName)
        {
            if (_parentFolders.TryGetValue(key, out var existing))
                return existing;

            Transform group = _root.Find(groupName) ?? new GameObject(groupName).transform;
            group.SetParent(_root);

            var folder = new GameObject(key).transform;
            folder.SetParent(group);

            _parentFolders[key] = folder;
            return folder;
        }
    }
}
