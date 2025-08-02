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
        private readonly Dictionary<string, ObjectPool<GameObject>> _pools = new();
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
            GameObject prefab,
            string groupName = "Default",
            System.Action<T> onCreate = null,
            System.Action<T> onGet = null,
            System.Action<T> onRelease = null,
            System.Action<T> onDestroy = null
        ) where T : Component
        {
            if (string.IsNullOrEmpty(nameOrID) || prefab == null)
            {
                Debug.LogError("[PoolingManager] Cannot create pool. Key or prefab is null.");
                return;
            }

            if (_pools.ContainsKey(nameOrID))
                return;

            var pool = CreatePool(nameOrID, prefab, groupName, onCreate, onGet, onRelease, onDestroy);
            _pools.Add(nameOrID, pool);
        }

        public T Get<T>(string nameOrID) where T : Component
        {
            if (!_pools.TryGetValue(nameOrID, out var pool))
            {
                Debug.LogError($"[PoolingManager] Pool for key '{nameOrID}' has not been created.");
                return null;
            }

            var instance = pool.Get();
            return instance.GetComponent<T>();
        }

        public void Release<T>(string nameOrID, T instance) where T : Component
        {
            if (instance == null)
            {
                Debug.LogWarning("[PoolingManager] Release failed. Instance is null.");
                return;
            }

            if (_pools.TryGetValue(nameOrID, out var pool))
            {
                pool.Release(instance.gameObject);
            }
            else
            {
                Debug.LogWarning($"[PoolingManager] Unknown pool key '{nameOrID}'. Destroying {instance.name}");
                Destroy(instance.gameObject);
            }
        }

        public void ClearPool(string nameOrID)
        {
            if (_pools.TryGetValue(nameOrID, out var pool))
            {
                pool.Dispose(); // Destroys all inactive objects and disables pool
                _pools.Remove(nameOrID);

                if (_parentFolders.TryGetValue(nameOrID, out var folder))
                {
                    Destroy(folder.gameObject);
                    _parentFolders.Remove(nameOrID);
                }
            }
        }
        
        public void ClearAllPools()
        {
            foreach (var pair in _pools)
            {
                pair.Value.Dispose();
            }

            _pools.Clear();

            foreach (var folder in _parentFolders.Values)
            {
                if (folder != null)
                    Destroy(folder.gameObject);
            }

            _parentFolders.Clear();
        }

        // -------- Internal Pool Creation --------

        private ObjectPool<GameObject> CreatePool<T>(
            string key,
            GameObject prefab,
            string groupName,
            System.Action<T> onCreate,
            System.Action<T> onGet,
            System.Action<T> onRelease,
            System.Action<T> onDestroy
        ) where T : Component
        {
            var parent = GetOrCreateParent(key, groupName);

            return new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(prefab, parent);
                    obj.name = prefab.name;
                    obj.SetActive(false);
                    onCreate?.Invoke(obj.GetComponent<T>());
                    return obj;
                },
                actionOnGet: go =>
                {
                    go.SetActive(true);
                    onGet?.Invoke(go.GetComponent<T>());
                },
                actionOnRelease: go =>
                {
                    go.SetActive(false);
                    onRelease?.Invoke(go.GetComponent<T>());
                },
                actionOnDestroy: go =>
                {
                    onDestroy?.Invoke(go.GetComponent<T>());
                    Destroy(go);
                },
                collectionCheck: false,
                defaultCapacity: 0,
                maxSize: int.MaxValue
            );
        }

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
