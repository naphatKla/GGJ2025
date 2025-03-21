using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    public static PoolManager Instance { get; private set; } = new();
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab)) pools[prefab] = new Queue<GameObject>();
        GameObject obj;
        if (pools[prefab].Count > 0)
        { obj = pools[prefab].Dequeue(); }
        else
        { obj = Object.Instantiate(prefab); }

        var pooled = obj.GetComponent<PooledObject>();
        if (pooled == null) pooled = obj.AddComponent<PooledObject>();
        pooled.OriginalPrefab = prefab;
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        var pooled = obj.GetComponent<PooledObject>();
        var key = pooled != null ? pooled.OriginalPrefab : obj;
        if (key == null) { return; }

        if (!pools.ContainsKey(key)) pools[key] = new Queue<GameObject>();
        pools[key].Enqueue(obj);
    }
}

public class PooledObject : MonoBehaviour
{
    public GameObject OriginalPrefab { get; set; }
}