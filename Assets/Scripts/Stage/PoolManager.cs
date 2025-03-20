using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    public static PoolManager Instance { get; private set; } = new();
    private Dictionary<GameObject, Queue<GameObject>> pools = new();

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab)) pools[prefab] = new Queue<GameObject>();
        GameObject obj = pools[prefab].Count > 0 ? pools[prefab].Dequeue() : UnityEngine.Object.Instantiate(prefab);
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }
}