using UnityEngine;

public class ObjectPoolSpawnerService : ISpawnerService
{
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool forceInstantiate = false)
    {
        return PoolManager.Instance.Spawn(prefab, position, rotation, parent, forceInstantiate);
    }
    public void Despawn(GameObject obj)
    {
        PoolManager.Instance.Despawn(obj);
    }

    public void ClearPool(Transform parent)
    {
        PoolManager.Instance.ClearPool(parent);
    }
}