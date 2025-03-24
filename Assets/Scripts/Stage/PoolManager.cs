using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    #region Properties

    public static PoolManager Instance { get; } = new PoolManager();
    private readonly Queue<GameObject> _pool = new();

    #endregion

    #region Methods

    /// <summary>
    /// Spawns an object from pool or creates new if none available
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool forceNew = false)
    {
        if (prefab == null) return null;

        GameObject obj = (forceNew || _pool.Count == 0) 
            ? InstantiateNewObject(prefab) 
            : _pool.Dequeue();

        SetupObject(obj, position, rotation);
        return obj;
    }

    /// <summary>
    /// Returns an object to the pool
    /// </summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    /// <summary>
    /// Clears the pool and destroys all pooled objects
    /// </summary>
    public void ClearPool()
    {
        while (_pool.Count > 0)
        {
            GameObject obj = _pool.Dequeue();
            Object.Destroy(obj);
        }
    }

    private GameObject InstantiateNewObject(GameObject prefab)
    {
        return Object.Instantiate(prefab);
    }

    private void SetupObject(GameObject obj, Vector3 position, Quaternion rotation)
    {
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
    }
    #endregion
}