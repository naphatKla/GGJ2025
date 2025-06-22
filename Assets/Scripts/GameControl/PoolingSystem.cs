using System;
using System.Collections.Generic;
using System.Linq;
using GameControl.Interface;
using MoreMountains.Tools;
using UnityEngine;

namespace GameControl
{
    /// <summary>
    /// Simple pooling system for reusing GameObjects to improve performance by avoiding frequent instantiation and destruction.
    /// </summary>
    public class PoolingSystem : MMSingleton<PoolingSystem>
    {
        private readonly Dictionary<string, Queue<GameObject>> pool = new();
        private readonly Dictionary<string, GameObject> prefabs = new();
        private readonly Dictionary<string, List<GameObject>> activeObjects = new();

        /// <summary>
        /// Pre-instantiates a number of GameObjects and stores them in the pool for future use.
        /// </summary>
        /// <param name="id">Identifier for the pool type.</param>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="amount">Number of instances to prewarm.</param>
        /// <param name="parent">Parent transform to assign the spawned objects to.</param>
        public void Prewarm(string id, GameObject prefab, int amount, Transform parent)
        {
            if (!prefabs.ContainsKey(id)) prefabs[id] = prefab;
            if (!pool.ContainsKey(id)) pool[id] = new Queue<GameObject>();

            for (var i = 0; i < amount; i++)
            {
                var obj = Instantiate(prefab, parent);
                obj.SetActive(false);
                pool[id].Enqueue(obj);
            }
        }

        /// <summary>
        /// Spawns a GameObject from the pool using the specified ID and prefab at the given position.
        /// Registers the prefab if it has not been registered yet.
        /// </summary>
        /// <param name="id">Identifier for the pool type.</param>
        /// <param name="prefab">Prefab to register and spawn.</param>
        /// <param name="pos">World position to place the spawned object.</param>
        /// <returns>The spawned GameObject.</returns>
        public GameObject Spawn(string id, GameObject prefab, Vector3 pos)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "Prefab cannot be null when registering new spawnId.");

            if (!prefabs.ContainsKey(id))
                prefabs[id] = prefab;

            return Spawn(id, pos);
        }

        /// <summary>
        /// Spawns a GameObject from the pool at the given position.
        /// </summary>
        /// <param name="id">Identifier for the pool type.</param>
        /// <param name="pos">World position to place the spawned object.</param>
        /// <returns>The spawned GameObject.</returns>
        public GameObject Spawn(string id, Vector3 pos)
        {
            if (!prefabs.ContainsKey(id)) throw new ArgumentException($"SpawnId '{id}' is not registered. Provide prefab to register it first.");
            if (!pool.ContainsKey(id)) pool[id] = new Queue<GameObject>();
            if (!activeObjects.ContainsKey(id)) activeObjects[id] = new List<GameObject>();

            var obj = pool[id].Count > 0
                ? pool[id].Dequeue()
                : Instantiate(prefabs[id]);

            obj.transform.position = pos;
            obj.SetActive(true);
            obj.GetComponent<ISpawnable>()?.OnSpawned();

            activeObjects[id].Add(obj);
            return obj;
        }

        /// <summary>
        /// Despawns (deactivates) a GameObject and returns it to the pool.
        /// </summary>
        /// <param name="id">Identifier for the pool type.</param>
        /// <param name="obj">The GameObject to despawn.</param>
        public void Despawn(string id, GameObject obj)
        {
            if (!activeObjects.ContainsKey(id)) return;
            if (!pool.ContainsKey(id)) pool[id] = new Queue<GameObject>();

            obj.SetActive(false);
            obj.GetComponent<IDespawnable>()?.OnDespawned();

            activeObjects[id].Remove(obj);
            pool[id].Enqueue(obj);
        }

        /// <summary>
        /// Clears all pooled and active objects of a specific type.
        /// Ensures active objects are despawned before destroying.
        /// </summary>
        /// <param name="id">Identifier for the pool type to clear.</param>
        public void ClearAllType(string id)
        {
            if (activeObjects.ContainsKey(id))
            {
                foreach (var obj in activeObjects[id].ToArray()) Despawn(id, obj);
                activeObjects[id].Clear();
            }

            if (pool.ContainsKey(id))
            {
                foreach (var obj in pool[id])
                    Destroy(obj);

                pool[id].Clear();
            }

            prefabs.Remove(id);
        }

        /// <summary>
        /// Clears all pooled and active GameObjects of all types.
        /// Despawns all active objects before destroying them.
        /// </summary>
        public void ClearAll()
        {
            foreach (var id in activeObjects.Keys)
            foreach (var obj in activeObjects[id].ToArray())
                Despawn(id, obj);

            foreach (var kvp in pool)
            foreach (var obj in kvp.Value)
                Destroy(obj);

            pool.Clear();
            prefabs.Clear();
            activeObjects.Clear();
        }

        /// <summary>
        /// Returns a list of all active GameObjects of a specific type.
        /// </summary>
        /// <param name="id">Identifier for the pool type.</param>
        /// <returns>List of active GameObjects.</returns>
        public List<GameObject> GetActiveObjects(string id)
        {
            if (!activeObjects.ContainsKey(id)) return new List<GameObject>();

            return activeObjects[id].Where(obj => obj != null && obj.activeSelf).ToList();
        }

        /// <summary>
        /// Returns a dictionary of all active GameObjects across all pool types.
        /// </summary>
        /// <returns>Dictionary of active GameObjects grouped by type.</returns>
        public Dictionary<string, List<GameObject>> GetAllActiveObjects()
        {
            var result = new Dictionary<string, List<GameObject>>();

            foreach (var kvp in activeObjects)
            {
                var activeList = kvp.Value.Where(obj => obj != null && obj.activeSelf).ToList();
                if (activeList.Count > 0)
                    result[kvp.Key] = activeList;
            }

            return result;
        }
    }
}
