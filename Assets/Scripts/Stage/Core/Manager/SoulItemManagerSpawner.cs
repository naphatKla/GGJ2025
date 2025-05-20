using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.CollectItemSystems.CollectableItems;
using Characters.SO.CollectableItemDataSO;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class SoulData
{
    public BaseCollectableItemDataSo soulData;
    public float spawnChance;
}

public class SoulItemManagerSpawner : MMSingleton<SoulItemManagerSpawner>
{
    #region Inspectors & Fields

    [SerializeField, Tooltip("List of soul prefabs and their spawn chances.")]
    private List<SoulData> soulDataList = new();

    [SerializeField, Tooltip("Parent transform for spawned oxygen objects.")]
    private Transform soulParent;

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
    /// List of currently active soul GameObjects.
    /// </summary>
    private readonly List<GameObject> _soulGO = new();
    
    /// <summary>
    /// Look up for Gameobject with name
    /// </summary>
    private readonly Dictionary<string, GameObject> _soulPrefabLookup = new();
    
    /// <summary>
    /// Cancellation token source for controlling spawning loop
    /// </summary>
    private CancellationTokenSource _spawningCts;
    
    /// <summary>
    /// Tracks if spawning is currently active
    /// </summary>
    private bool _isSpawning;
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();

    #endregion

    #region Properties

    private bool CanSpawnMore => _soulGO.Count < maxSpawn;

    #endregion

    #region Unity Methods
    

    /// <summary>
    /// Initializes the spawn manager, sets up the object pool, and starts spawning.
    /// </summary>
    private async UniTaskVoid Start()
    {
        SetupPool();
        _spawningCts = new CancellationTokenSource();

        if (spawnMaximumOnStart)
            PreSpawnToMax();

        await UniTask.Delay(TimeSpan.FromSeconds(1));
        await StartSpawningLoop();
    }
    
    /// <summary>
    /// Cleans up cancellation token source on destroy
    /// </summary>
    private void OnDestroy()
    {
        ResetToken();
    }
    
    /// <summary>
    /// Reset token
    /// </summary>
    private void ResetToken()
    {
        _spawningCts?.Cancel();
        _spawningCts?.Dispose();
        _spawningCts = null;
    }

    /// <summary>
    /// Draws a wireframe cube in the editor to visualize the spawn area.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (soulParent == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(soulParent.position, new Vector3(spawnSize.x, spawnSize.y, 0));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sets up the object pool for all soul item.
    /// </summary>
    private void SetupPool()
    {
        foreach (var data in soulDataList)
            if (data.soulData != null)
                for (var a = 0; a < maxSpawn; a++)
                {
                    var go = new GameObject(data.soulData.name);
                    var item = (BaseCollectableItem)go.AddComponent(data.soulData.ItemType);
                    item.AssignItemData(data.soulData);
                    item.OnThisItemCollected += DespawnSoul;
                    go.SetActive(false);
                    go.transform.parent = soulParent;
                    PoolManager.Instance.AddToPool(go);
                    
                    if (!_soulPrefabLookup.ContainsKey(data.soulData.name)) 
                        _soulPrefabLookup[data.soulData.name] = go;
                }
        CalculateTotalChance();
    }

    /// <summary>
    /// Calculates the total spawn chance from all soul data.
    /// </summary>
    private void CalculateTotalChance()
    {
        _totalSpawnChance = soulDataList.Sum(data => data.spawnChance);
    }

    /// <summary>
    /// Spawns oxygen objects up to the maximum limit at random positions.
    /// </summary>
    private void PreSpawnToMax()
    {
        for (var i = 0; i < maxSpawn; i++)
        {
            if (!CanSpawnMore) break;
            var data = GetRandomSoul();
            var pos = GetRandomPosition();
            SpawnSoul(data.soulData.name, pos, Quaternion.identity);
        }
    }

    /// <summary>
    /// Starts an asynchronous loop to spawn oxygen objects at regular intervals.
    /// </summary>
    /// <summary>
    /// Starts an asynchronous loop to spawn oxygen objects at regular intervals.
    /// </summary>
    private async UniTask StartSpawningLoop()
    {
        _isSpawning = true;
        while (gameObject.activeInHierarchy && !_spawningCts.Token.IsCancellationRequested)
        {
            try
            {
                if (CanSpawnMore)
                {
                    for (var i = 0; i < spawnPerTick && CanSpawnMore; i++)
                    {
                        var data = GetRandomSoul();
                        Debug.Log(data);
                        var pos = GetRandomPosition();
                        SpawnSoul(data.soulData.name, pos, Quaternion.identity);
                    }
                }
                await UniTask.Delay(TimeSpan.FromSeconds(timeTick), cancellationToken: _spawningCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        _isSpawning = false;
    }

    /// <summary>
    /// Selects a random soul Item based on spawn chance weights.
    /// </summary>
    /// <returns>The selected soul data.</returns>
    private SoulData GetRandomSoul()
    {
        if (_totalSpawnChance <= 0) return soulDataList[0];

        var rand = UnityEngine.Random.Range(0f, _totalSpawnChance);
        var cum = 0f;
        foreach (var data in soulDataList)
        {
            cum += data.spawnChance;
            if (rand <= cum)
                return data;
        }
        return soulDataList[0];
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
        ) + (soulParent?.position ?? Vector3.zero);
    }

    /// <summary>
    /// Spawns an oxygen object at the specified position and rotation.
    /// </summary>
    /// <param name="prefab">The oxygen prefab to spawn.</param>
    /// <param name="position">The spawn position.</param>
    /// <param name="rotation">The spawn rotation.</param>
    private void SpawnSoul(string name, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (!_soulPrefabLookup.TryGetValue(name, out var prefab)) return;
        var item = PoolManager.Instance.Spawn(prefab, position, rotation, soulParent);
        _soulGO.Add(item);
    }

    /// <summary>
    /// Despawn soul
    /// </summary>
    /// <param name="soul"></param>
    public void DespawnSoul(GameObject soul)
    {
        PoolManager.Instance.Despawn(soul);
        _soulGO.Remove(soul);
    }
    
    #endregion

    #region Public Methods

    /// <summary>
    /// Despawns all active oxygen objects, clears the tracking list, and resets the pool.
    /// </summary>
    [FoldoutGroup("Soul Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void ClearAll()
    {
        foreach (var obj in _soulGO)
        {
            if (obj != null)
            {
                PoolManager.Instance.Despawn(obj);
            }
        }
        _soulGO.Clear();
        _spawnerService.ClearPool(soulParent);
        SetupPool();
    }
    
    /// <summary>
    /// Starts the spawning process
    /// </summary>
    [FoldoutGroup("Soul Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void StartSpawning()
    {
        if (_isSpawning) return;
        
        _spawningCts?.Cancel();
        _spawningCts?.Dispose();
        _spawningCts = new CancellationTokenSource();
        StartSpawningLoop().Forget();
    }

    /// <summary>
    /// Stops the spawning process
    /// </summary>
    [FoldoutGroup("Soul Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void StopSpawning()
    {
        if (!_isSpawning) return;
        _spawningCts?.Cancel();
    }

    #endregion
}