using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class OxygenDataNew
{
    public Oxygen oxygenPrefab;

    /// <summary>
    /// The probability weight for spawning this oxygen type.
    /// </summary>
    public float spawnChance;
}

public class OxygenSpawnManagerNew : MMSingleton<OxygenSpawnManagerNew>
{
    #region Inspectors & Fields

    [SerializeField, Tooltip("List of oxygen prefabs and their spawn chances.")]
    private List<OxygenDataNew> oxygenDataList = new();

    [SerializeField, Tooltip("Parent transform for spawned oxygen objects.")]
    private Transform oxygenParent;

    [SerializeField, Tooltip("If true, spawns the maximum number of oxygen objects on start.")]
    private bool spawnMaximumOnStart;

    [SerializeField, Tooltip("Maximum number of oxygen objects allowed in the scene.")]
    private int maxSpawn;

    [SerializeField, Tooltip("Number of oxygen objects to spawn per tick.")]
    private int spawnPerTick = 1;

    [SerializeField, Tooltip("Time interval between spawn ticks (in seconds).")]
    private float timeTick = 1f;

    [SerializeField, Tooltip("Size of the spawn area (width and height).")]
    private Vector2 spawnSize = Vector2.zero;

    /// <summary>
    /// Total sum of spawn chances for all oxygen types.
    /// </summary>
    private float _totalSpawnChance;

    /// <summary>
    /// List of currently active oxygen GameObjects.
    /// </summary>
    private readonly List<GameObject> _oxygenGO = new();

    #endregion

    #region Unity Methods

    /// <summary>
    /// Initializes the spawn manager, sets up the object pool, and starts spawning.
    /// </summary>
    private async UniTaskVoid Start()
    {
        SetupPool();

        if (spawnMaximumOnStart)
            PreSpawnToMax();

        await UniTask.Delay(TimeSpan.FromSeconds(1));
        await StartSpawningLoop();
    }

    /// <summary>
    /// Draws a wireframe cube in the editor to visualize the spawn area.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (oxygenParent == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(oxygenParent.position, new Vector3(spawnSize.x, spawnSize.y, 0));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sets up the object pool for all oxygen prefabs.
    /// </summary>
    private void SetupPool()
    {
        foreach (var data in oxygenDataList)
        {
            if (data.oxygenPrefab != null)
            {
                PoolManager.Instance.PreWarm(data.oxygenPrefab.gameObject, maxSpawn, oxygenParent);
            }
        }

        CalculateTotalChance();
    }

    /// <summary>
    /// Calculates the total spawn chance from all oxygen data.
    /// </summary>
    private void CalculateTotalChance()
    {
        _totalSpawnChance = oxygenDataList.Sum(data => data.spawnChance);
    }

    /// <summary>
    /// Spawns oxygen objects up to the maximum limit at random positions.
    /// </summary>
    private void PreSpawnToMax()
    {
        for (var i = 0; i < maxSpawn; i++)
        {
            if (!CanSpawn()) break;
            var data = GetRandomOxygen();
            var pos = GetRandomPosition();
            SpawnOxygen(data.oxygenPrefab.gameObject, pos, Quaternion.identity);
        }
    }

    /// <summary>
    /// Starts an asynchronous loop to spawn oxygen objects at regular intervals.
    /// </summary>
    private async UniTask StartSpawningLoop()
    {
        while (gameObject.activeInHierarchy)
        {
            if (CanSpawn())
            {
                for (var i = 0; i < spawnPerTick && CanSpawn(); i++)
                {
                    var data = GetRandomOxygen();
                    var pos = GetRandomPosition();
                    SpawnOxygen(data.oxygenPrefab.gameObject, pos, Quaternion.identity);
                }
            }
            await UniTask.Delay(TimeSpan.FromSeconds(timeTick), cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }

    /// <summary>
    /// Selects a random oxygen prefab based on spawn chance weights.
    /// </summary>
    /// <returns>The selected oxygen data.</returns>
    private OxygenDataNew GetRandomOxygen()
    {
        if (_totalSpawnChance <= 0) return oxygenDataList[0];

        var rand = UnityEngine.Random.Range(0f, _totalSpawnChance);
        var cum = 0f;
        foreach (var data in oxygenDataList)
        {
            cum += data.spawnChance;
            if (rand <= cum)
                return data;
        }
        return oxygenDataList[0];
    }

    /// <summary>
    /// Generates a random position within the spawn area.
    /// </summary>
    /// <returns>A random position vector.</returns>
    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(-spawnSize.x / 2f, spawnSize.x / 2f),
            UnityEngine.Random.Range(-spawnSize.y / 2f, spawnSize.y / 2f),
            0f
        ) + (oxygenParent?.position ?? Vector3.zero);
    }

    /// <summary>
    /// Spawns an oxygen object at the specified position and rotation.
    /// </summary>
    /// <param name="prefab">The oxygen prefab to spawn.</param>
    /// <param name="position">The spawn position.</param>
    /// <param name="rotation">The spawn rotation.</param>
    private void SpawnOxygen(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        var obj = PoolManager.Instance.Spawn(prefab, position, rotation, oxygenParent);
        _oxygenGO.Add(obj);
    }

    /// <summary>
    /// Checks if more oxygen objects can be spawned.
    /// </summary>
    /// <returns>True if the current number of oxygen objects is below the maximum limit.</returns>
    private bool CanSpawn()
    {
        return _oxygenGO.Count < maxSpawn;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Despawns all active oxygen objects, clears the tracking list, and resets the pool.
    /// </summary>
    [Button]
    public void ClearAll()
    {
        foreach (var obj in _oxygenGO)
        {
            if (obj != null)
            {
                PoolManager.Instance.Despawn(obj);
            }
        }
        _oxygenGO.Clear();
        PoolManager.Instance.ClearPool(oxygenParent);
        SetupPool();
    }

    #endregion
}