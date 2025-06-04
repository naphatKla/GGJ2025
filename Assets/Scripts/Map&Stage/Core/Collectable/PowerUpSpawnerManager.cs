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
public class PowerUpData
{
    public BaseCollectableItemDataSo powerData;
    public float spawnChance;
}

public class PowerUpSpawnerManager : MMSingleton<PowerUpSpawnerManager>
{
    #region Fields

    [SerializeField, Tooltip("List of power-up prefabs and their spawn chances.")]
    private List<PowerUpData> powerDataList = new();

    [SerializeField, Tooltip("Parent transform for spawned power-up objects.")]
    private Transform powerParent;

    [SerializeField, Tooltip("Size of the spawn area (width and height).")]
    private Vector2 spawnSize = Vector2.zero;

    [SerializeField, Tooltip("Time interval between spawn attempts (in seconds).")]
    private float spawnInterval = 10f;

    [SerializeField, Tooltip("Initial spawn chance percentage (0-1).")]
    private float initialSpawnChance = 0.1f;

    private readonly HashSet<GameObject> _activePowerUps = new();
    private readonly Dictionary<string, GameObject> _powerUpPrefabs = new();
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();
    private CancellationTokenSource _spawningCts;
    private float _totalSpawnChance;
    private float _currentSpawnChance;
    private float _currentCooldown;
    private bool _isSpawning;
    private const int MaxSpawn = 1;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the spawner, sets up the object pool, and starts the spawning loop.
    /// </summary>
    private async UniTaskVoid Start()
    {
        _currentSpawnChance = initialSpawnChance;
        _currentCooldown = spawnInterval;
        InitializePool();
        _spawningCts = new CancellationTokenSource();
        await RunSpawnLoop();
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
        if (powerParent == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(powerParent.position, new Vector3(spawnSize.x, spawnSize.y, 0));
    }

    #endregion

    #region Spawning Logic

    /// <summary>
    /// Sets up the object pool for power-ups and initializes the spawn chance sum.
    /// </summary>
    private void InitializePool()
    {
        foreach (var data in powerDataList)
        {
            if (data.powerData == null) continue;

            var go = new GameObject(data.powerData.name);
            var item = (BaseCollectableItem)go.AddComponent(data.powerData.ItemType);
            item.AssignItemData(data.powerData);
            item.OnThisItemCollected += DespawnPowerUp;
            go.SetActive(false);
            go.transform.parent = powerParent;
            PoolManager.Instance.AddToPool(go);
            _powerUpPrefabs[data.powerData.name] = go;
        }
        _totalSpawnChance = powerDataList.Sum(data => data.spawnChance);
    }

    /// <summary>
    /// Runs the main spawning loop, updating cooldown and attempting to spawn power-ups.
    /// </summary>
    private async UniTask RunSpawnLoop()
    {
        _isSpawning = true;
        while (gameObject.activeInHierarchy && !_spawningCts.Token.IsCancellationRequested)
        {
            try
            {
                UpdateCooldown();
                TrySpawnPowerUp();
                await UniTask.Yield(_spawningCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        _isSpawning = false;
    }

    /// <summary>
    /// Updates the spawn cooldown based on the number of active power-ups.
    /// </summary>
    private void UpdateCooldown()
    {
        if (_activePowerUps.Count >= MaxSpawn) return;
        _currentCooldown = Mathf.Max(0, _currentCooldown - Time.deltaTime);
    }

    /// <summary>
    /// Attempts to spawn a power-up if conditions are met, adjusting spawn chance and cooldown.
    /// </summary>
    private void TrySpawnPowerUp()
    {
        if (_activePowerUps.Count >= MaxSpawn || _currentCooldown > 0) return;

        if (Random.value <= _currentSpawnChance)
        {
            var data = GetRandomPowerUp();
            SpawnPowerUp(data.powerData.name, GetRandomPosition(), Quaternion.identity);
            _currentSpawnChance = initialSpawnChance;
            _currentCooldown = spawnInterval;
        }
        else
        {
            _currentSpawnChance = Mathf.Min(_currentSpawnChance * 2, 1f);
            _currentCooldown = spawnInterval;
        }
    }

    /// <summary>
    /// Selects a random power-up based on weighted spawn chances.
    /// </summary>
    /// <returns>The selected power-up data, or the first in the list if no valid selection is made.</returns>
    private PowerUpData GetRandomPowerUp()
    {
        if (_totalSpawnChance <= 0 || powerDataList.Count == 0) return powerDataList[0];
        var rand = Random.Range(0f, _totalSpawnChance);
        var cumulative = 0f;
        foreach (var data in powerDataList)
        {
            cumulative += data.spawnChance;
            if (rand <= cumulative) return data;
        }
        return powerDataList[0];
    }

    /// <summary>
    /// Generates a random spawn position within the defined spawn area.
    /// </summary>
    /// <returns>A Vector3 representing the spawn position.</returns>
    private Vector3 GetRandomPosition()
    {
        var offset = new Vector3(
            Random.Range(-spawnSize.x / 2f, spawnSize.x / 2f),
            Random.Range(-spawnSize.y / 2f, spawnSize.y / 2f),
            0f
        );
        return powerParent != null ? powerParent.position + offset : offset;
    }

    /// <summary>
    /// Spawns a power-up at the specified position and rotation.
    /// </summary>
    /// <param name="name">The name of the power-up to spawn.</param>
    /// <param name="position">The position to spawn the power-up at.</param>
    /// <param name="rotation">The rotation to apply to the spawned power-up.</param>
    private void SpawnPowerUp(string name, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(name) || !_powerUpPrefabs.TryGetValue(name, out var prefab)) return;
        var item = PoolManager.Instance.Spawn(prefab, position, rotation, powerParent);
        _activePowerUps.Add(item);
    }

    /// <summary>
    /// Despawns a power-up when collected or removed.
    /// </summary>
    /// <param name="item">The power-up GameObject to despawn.</param>
    public void DespawnPowerUp(GameObject item)
    {
        if (_activePowerUps.Remove(item))
        {
            PoolManager.Instance.Despawn(item);
        }
    }

    /// <summary>
    /// Reduces the spawn cooldown when an enemy is defeated.
    /// </summary>
    public void OnEnemyDefeated()
    {
        _currentCooldown = Mathf.Max(0, _currentCooldown - 0.5f);
    }

    #endregion

    #region Public Controls

    /// <summary>
    /// Clears all active power-ups, resets the pool, and reinitializes spawning parameters.
    /// </summary>
    [FoldoutGroup("PowerUp Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void ClearAll()
    {
        foreach (var obj in _activePowerUps.ToList())
        {
            DespawnPowerUp(obj);
        }
        _spawnerService.ClearPool(powerParent);
        InitializePool();
        _currentSpawnChance = initialSpawnChance;
        _currentCooldown = spawnInterval;
    }

    /// <summary>
    /// Starts the power-up spawning loop if not already running.
    /// </summary>
    [FoldoutGroup("PowerUp Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void StartSpawning()
    {
        if (_isSpawning) return;
        _spawningCts?.Dispose();
        _spawningCts = new CancellationTokenSource();
        RunSpawnLoop().Forget();
    }

    /// <summary>
    /// Stops the power-up spawning loop if currently running.
    /// </summary>
    [FoldoutGroup("PowerUp Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void StopSpawning()
    {
        if (!_isSpawning) return;
        _spawningCts?.Cancel();
    }

    #endregion
}