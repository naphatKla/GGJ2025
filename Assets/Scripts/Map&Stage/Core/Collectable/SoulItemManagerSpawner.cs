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
using Random = UnityEngine.Random;

[Serializable]
public class SoulData
{
    public SoulItemDataSo soulData;
    public float spawnChance;
    public float preSpawnObject;
}

public class SoulItemManagerSpawner : MMSingleton<SoulItemManagerSpawner>
{
    #region Fields

    [SerializeField, Tooltip("List of soul prefabs, their spawn chances, and pre-spawn counts.")]
    private List<SoulData> soulDataList = new();

    [SerializeField, Tooltip("Parent transform for spawned soul objects.")]
    private Transform soulParent;

    [SerializeField, Tooltip("If true, spawns the maximum number of soul objects on start.")]
    private bool spawnMaximumOnStart;

    [SerializeField, Tooltip("Maximum number of soul objects allowed in the scene.")]
    private int maxSpawn;

    [SerializeField, Tooltip("Number of soul objects to spawn per tick.")]
    private int spawnPerTick = 1;

    [SerializeField, Tooltip("Time interval between spawn ticks (in seconds).")]
    private float timeTick = 1f;

    [SerializeField, Tooltip("Size of the spawn area (width and height).")]
    private Vector2 spawnSize = Vector2.zero;

    [SerializeField, Tooltip("If true, prioritizes fewer soul items for EXP drops; otherwise, uses smaller EXP items.")]
    private bool preferFewerItems = true;

    private readonly HashSet<GameObject> _activeSouls = new();
    private readonly Dictionary<string, GameObject> _soulPrefabs = new();
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();
    private CancellationTokenSource _spawningCts;
    private float _totalSpawnChance;
    private bool _isSpawning;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the spawner, sets up the object pool, and starts the spawning process.
    /// </summary>
    private void Start()
    {
        InitializePool();
        _spawningCts = new CancellationTokenSource();

        if (spawnMaximumOnStart)
            SpawnInitialSouls();
        RunSpawnLoop();
    }

    /// <summary>
    /// Cleans up the spawning cancellation token when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        _spawningCts?.Cancel();
        _spawningCts?.Dispose();
    }

    /// <summary>
    /// Draws a gizmo to visualize the spawn area in the Unity editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (soulParent == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(soulParent.position, new Vector3(spawnSize.x, spawnSize.y, 0));
    }

    #endregion

    #region Spawning Logic

    /// <summary>
    /// Sets up the object pool for soul items based on pre-spawn counts and calculates total spawn chance.
    /// </summary>
    private void InitializePool()
    {
        _soulPrefabs.Clear();
        _totalSpawnChance = 0f;
        foreach (var data in soulDataList)
        {
            if (data.soulData == null) continue;

            int spawnCount = Mathf.Max(1, Mathf.CeilToInt(data.preSpawnObject));
            for (int i = 0; i < spawnCount; i++)
            {
                var go = new GameObject(data.soulData.name);
                var item = (BaseCollectableItem)go.AddComponent(data.soulData.ItemType);
                item.AssignItemData(data.soulData);
                item.OnThisItemCollected += DespawnSoul;
                go.SetActive(false);
                go.transform.parent = soulParent;
                PoolManager.Instance.AddToPool(go);
                _soulPrefabs[data.soulData.name] = go;
            }
        }
        _totalSpawnChance = soulDataList.Sum(data => data.spawnChance);
    }

    /// <summary>
    /// Spawns soul objects up to the maximum limit at random positions on start.
    /// </summary>
    private void SpawnInitialSouls()
    {
        for (int i = 0; i < maxSpawn && _activeSouls.Count < maxSpawn; i++)
        {
            var data = GetRandomSoul();
            SpawnSoul(data.soulData.name, GetRandomPosition(), Quaternion.identity);
        }
    }

    /// <summary>
    /// Runs the main spawning loop, spawning souls at regular intervals.
    /// </summary>
    private async UniTask RunSpawnLoop()
    {
        _isSpawning = true;
        var token = _spawningCts.Token;

        try
        {
            while (gameObject.activeInHierarchy && !token.IsCancellationRequested)
            {
                TrySpawnSouls();
                await UniTask.Delay(TimeSpan.FromSeconds(timeTick), cancellationToken: token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isSpawning = false;
        }
    }

    /// <summary>
    /// Attempts to spawn souls up to the per-tick limit if the maximum spawn count allows.
    /// </summary>
    private void TrySpawnSouls()
    {
        if (_activeSouls.Count >= maxSpawn) return;

        for (int i = 0; i < spawnPerTick && _activeSouls.Count < maxSpawn; i++)
        {
            var data = GetRandomSoul();
            SpawnSoul(data.soulData.name, GetRandomPosition(), Quaternion.identity);
        }
    }
    

    /// <summary>
    /// Spawns soul items to match a target EXP value at the specified position.
    /// </summary>
    /// <param name="targetExp">The total EXP value to drop.</param>
    /// <param name="position">The position to spawn the soul items.</param>
    public void SpawnExpDrop(int targetExp, Vector3 position)
    {
        var drops = CalculateExpDrop(targetExp);
        foreach (var drop in drops)
        {
            SpawnSoul(drop.soulData.name, position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Calculates the combination of soul items to match the target EXP value.
    /// </summary>
    /// <param name="targetExp">The total EXP value to achieve.</param>
    /// <returns>A list of soul data items to spawn.</returns>
    private List<SoulData> CalculateExpDrop(int targetExp)
    {
        var drops = new List<SoulData>();
        var remainingExp = targetExp;
        
        var sortedSouls = preferFewerItems
            ? soulDataList.OrderByDescending(data => data.soulData.Score).ToList()
            : soulDataList.OrderBy(data => data.soulData.Score).ToList();

        //Find fitable exp to drop
        foreach (var data in sortedSouls)
        {
            if (data.soulData.Score <= 0) continue;

            int count = remainingExp / data.soulData.Score;
            for (int i = 0; i < count; i++)
            {
                drops.Add(data);
                remainingExp -= data.soulData.Score;
            }
        }
        
        //Remaining exp 
        if (remainingExp > 0)
        {
            var smallestFittable = sortedSouls
                .Where(d => d.soulData.Score <= remainingExp)
                .OrderByDescending(d => d.soulData.Score)
                .FirstOrDefault();

            if (smallestFittable != null)
            {
                drops.Add(smallestFittable);
                remainingExp -= smallestFittable.soulData.Score;
            }
        }
        return drops;
    }

    /// <summary>
    /// Selects a random soul item based on weighted spawn chances.
    /// </summary>
    /// <returns>The selected soul data, or the first in the list if no valid selection is made.</returns>
    private SoulData GetRandomSoul()
    {
        if (_totalSpawnChance <= 0 || soulDataList.Count == 0) return soulDataList[0];
        var rand = Random.Range(0f, _totalSpawnChance);
        var cumulative = 0f;
        foreach (var data in soulDataList)
        {
            cumulative += data.spawnChance;
            if (rand <= cumulative) return data;
        }
        return soulDataList[0];
    }

    /// <summary>
    /// Generates a random position within the defined spawn area.
    /// </summary>
    /// <returns>A Vector3 representing the spawn position.</returns>
    private Vector3 GetRandomPosition()
    {
        var offset = new Vector3(
            Random.Range(-spawnSize.x / 2f, spawnSize.x / 2f),
            Random.Range(-spawnSize.y / 2f, spawnSize.y / 2f),
            0f
        );
        return soulParent != null ? soulParent.position + offset : offset;
    }

    /// <summary>
    /// Spawns a soul object at the specified position and rotation.
    /// </summary>
    /// <param name="name">The name of the soul to spawn.</param>
    /// <param name="position">The position to spawn the soul at.</param>
    /// <param name="rotation">The rotation to apply to the spawned soul.</param>
    private void SpawnSoul(string name, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(name) || !_soulPrefabs.TryGetValue(name, out var prefab)) return;
        var item = PoolManager.Instance.Spawn(prefab, position, rotation, soulParent);
        _activeSouls.Add(item);
    }

    /// <summary>
    /// Despawns a soul object and removes it from the active list.
    /// </summary>
    /// <param name="soul">The soul GameObject to despawn.</param>
    public void DespawnSoul(GameObject soul)
    {
        if (_activeSouls.Remove(soul))
        {
            PoolManager.Instance.Despawn(soul);
        }
    }

    #endregion

    #region Public Controls

    /// <summary>
    /// Despawns all active soul objects, clears the pool, and reinitializes it.
    /// </summary>
    [FoldoutGroup("Soul Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void ClearAll()
    {
        StopSpawning();
        foreach (var obj in _activeSouls.ToList())
        {
            DespawnSoul(obj);
        }
        _spawnerService.ClearPool(soulParent);
        InitializePool();
        StartSpawning();
    }


    /// <summary>
    /// Starts the soul spawning loop if not already running.
    /// </summary>
    [FoldoutGroup("Soul Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void StartSpawning()
    {
        if (_isSpawning) return;
        _spawningCts?.Cancel();
        _spawningCts = new CancellationTokenSource();

        RunSpawnLoop().Forget();
    }


    /// <summary>
    /// Stops the soul spawning loop if currently running.
    /// </summary>
    [FoldoutGroup("Soul Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void StopSpawning()
    {
        if (!_isSpawning) return;

        _spawningCts?.Cancel();
        _isSpawning = false;
    }
    

    #endregion
}