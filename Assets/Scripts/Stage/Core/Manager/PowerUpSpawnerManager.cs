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

#region Data Classes
[Serializable]
public class PowerUpData
{
    public BaseCollectableItemDataSo powerData;
    public float spawnChance;
}
#endregion

public class PowerUpSpawnerManager : MMSingleton<PowerUpSpawnerManager>
{
    #region Inspector Fields
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

    #endregion

    #region Private Fields
    private float _totalSpawnChance;
    private readonly HashSet<GameObject> _powerGO = new();
    private readonly Dictionary<string, GameObject> _powerPrefabLookup = new();
    private CancellationTokenSource _spawningCts;
    private bool _isSpawning;
    private float _currentSpawnChance;
    private float _currentCooldown;
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();
    private const int MAX_SPAWN = 1;

    #endregion

    #region Properties
    private bool CanSpawnMore => _powerGO.Count < MAX_SPAWN;
    #endregion

    #region Unity Lifecycle
    private async UniTaskVoid Start()
    {
        _currentSpawnChance = initialSpawnChance;
        SetupPool();
        _spawningCts = new CancellationTokenSource();
        await StartSpawningLoop();
    }

    private void OnDestroy()
    {
        ResetToken();
    }

    private void ResetToken()
    {
        _spawningCts?.Cancel();
        _spawningCts?.Dispose();
        _spawningCts = null;
    }

    private void OnDrawGizmos()
    {
        if (powerParent == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(powerParent.position, new Vector3(spawnSize.x, spawnSize.y, 0));
    }
    #endregion

    #region Spawning Logic
    private void SetupPool()
    {
        foreach (var data in powerDataList)
        {
            if (data.powerData != null)
            {
                for (var a = 0; a < MAX_SPAWN; a++)
                {
                    var go = new GameObject(data.powerData.name);
                    var item = (BaseCollectableItem)go.AddComponent(data.powerData.ItemType);
                    item.AssignItemData(data.powerData);
                    item.OnThisItemCollected += DespawnPower;
                    go.SetActive(false);
                    go.transform.parent = powerParent;
                    PoolManager.Instance.AddToPool(go);

                    if (!_powerPrefabLookup.ContainsKey(data.powerData.name))
                        _powerPrefabLookup[data.powerData.name] = go;
                }
            }
        }
        CalculateTotalChance();
    }

    private void CalculateTotalChance()
    {
        _totalSpawnChance = powerDataList.Sum(data => data.spawnChance);
    }

    private async UniTask StartSpawningLoop()
    {
        _isSpawning = true;
        _currentCooldown = spawnInterval;
        while (gameObject.activeInHierarchy && !_spawningCts.Token.IsCancellationRequested)
            try
            {
                if (CanSpawnMore)
                {
                    _currentCooldown -= Time.deltaTime;
                    if (_currentCooldown <= 0)
                    {
                        if (Random.value <= _currentSpawnChance)
                        {
                            var data = GetRandomPower();
                            var pos = GetRandomPosition();
                            SpawnPower(data.powerData.name, pos, Quaternion.identity);
                            _currentSpawnChance = initialSpawnChance;
                        }
                        else
                        {
                            _currentSpawnChance = Mathf.Min(_currentSpawnChance * 2, 1f);
                        }

                        _currentCooldown = spawnInterval;
                    }
                }

                await UniTask.Yield(_spawningCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

        _isSpawning = false;
    }

    private PowerUpData GetRandomPower()
    {
        if (_totalSpawnChance <= 0) return powerDataList[0];
        var rand = UnityEngine.Random.Range(0f, _totalSpawnChance);
        var cum = 0f;
        foreach (var data in powerDataList)
        {
            cum += data.spawnChance;
            if (rand <= cum)
                return data;
        }
        return powerDataList[0];
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(-spawnSize.x / 2f, spawnSize.x / 2f),
            UnityEngine.Random.Range(-spawnSize.y / 2f, spawnSize.y / 2f),
            0f
        ) + (powerParent?.position ?? Vector3.zero);
    }

    private void SpawnPower(string name, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (!_powerPrefabLookup.TryGetValue(name, out var prefab)) return;
        var item = PoolManager.Instance.Spawn(prefab, position, rotation, powerParent);
        _powerGO.Add(item);
    }

    public void DespawnPower(GameObject item)
    {
        PoolManager.Instance.Despawn(item);
        _powerGO.Remove(item);
    }

    public void OnEnemyDefeated()
    {
        if (_currentCooldown > 0)
        {
            _currentCooldown = Mathf.Max(0, _currentCooldown - 0.5f);
        }
    }
    #endregion

    #region Public Controls
    [FoldoutGroup("PowerUp Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void ClearAll()
    {
        foreach (var obj in _powerGO)
        {
            if (obj != null)
                PoolManager.Instance.Despawn(obj);
        }
        _powerGO.Clear();
        _spawnerService.ClearPool(powerParent);
        SetupPool();
        _currentSpawnChance = initialSpawnChance;
        _currentCooldown = spawnInterval;
    }

    [FoldoutGroup("PowerUp Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void StartSpawning()
    {
        if (_isSpawning) return;
        _spawningCts?.Cancel();
        _spawningCts?.Dispose();
        _spawningCts = new CancellationTokenSource();
        StartSpawningLoop().Forget();
    }

    [FoldoutGroup("PowerUp Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void StopSpawning()
    {
        if (!_isSpawning) return;
        _spawningCts?.Cancel();
    }
    #endregion
}