using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    #region Properties

    public static PoolManager Instance { get; } = new();
    private readonly Queue<GameObject> _pool = new();

    #endregion

    #region Methods

    /// <summary>
    /// Spawns a GameObject from the pool or creates a new one if none available.
    /// </summary>
    /// <param name="prefab">The prefab to spawn.</param>
    /// <param name="position">The spawn position.</param>
    /// <param name="rotation">The spawn rotation.</param>
    /// <param name="forceNew">If true, creates a new object instead of reusing.</param>
    /// <returns>The spawned GameObject or null if prefab is null.</returns>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool forceNew = false)
    {
        if (prefab == null) return null;

        var obj = forceNew || _pool.Count == 0
            ? InstantiateNewObject(prefab)
            : _pool.Dequeue();

        SetupObject(obj, position, rotation);
        return obj;
    }

    /// <summary>
    ///     Returns a GameObject to the pool for reuse.
    /// </summary>
    /// <param name="obj">The GameObject to despawn.</param>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    /// <summary>
    ///     Clears the pool and destroys all pooled objects
    /// </summary>
    public void ClearPool()
    {
        while (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
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