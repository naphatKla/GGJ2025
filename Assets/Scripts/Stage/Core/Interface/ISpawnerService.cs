using UnityEngine;

public interface ISpawnerService
{
    /// <summary>
    /// Spawns a new instance of the prefab at the given position and rotation.
    /// </summary>
    GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool forceInstantiate = false);

    /// <summary>
    /// Despawns (releases) the given GameObject back to the pool.
    /// </summary>
    void Despawn(GameObject obj);

    /// <summary>
    /// Clears all pooled objects under a parent.
    /// </summary>
    void ClearPool(Transform parent);
}