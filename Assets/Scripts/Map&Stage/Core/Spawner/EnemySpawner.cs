using System;
using System.Collections.Generic;
using System.Linq;
using Characters;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class EnemySpawner
{
    #region Fields

    // Core data and config references
    private const int MaxSpawnPositionAttempts = 10;
    private readonly StageManager _stageManager;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;
    private readonly Vector2 _screenSize;

    // Spawn state and progression tracking
    private ISpawnState _currentState;
    private float _totalEnemySpawnChance;
    private float _nextUnitKillQuota;
    private float _nextIntervalKillQuota;
    private float _killCount;
    private int _addMaxEnemySpawn;
    private float _removeIntervalEnemySpawn;
   
    // Active enemies in the world
    public readonly HashSet<EnemyController> enemies = new();
    
    // Spawning interface
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();

    #endregion

    #region Properties
    
    public float DebugNextIntervalStartMax => _nextIntervalKillQuota;
    public float DebugNextKillStartMax => _nextUnitKillQuota;

    // Current delay between normal spawns
    public float CurrentSpawnInterval { get; private set; }

    // Limit of how many enemies can be alive at once
    public int CurrentMaxEnemySpawn { get; private set; }
    
    // Kill count from enemy despawn
    public float CurrentKillCount => _killCount;

    // Time between event trigger checks
    public float EventIntervalCheck => _stageData.EventIntervalCheck;

    public event Action<EnemyController> OnEnemySpawned;
    public event Action<EnemyController> OnEnemyDespawned;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes all required spawn parameters and default state.
    /// </summary>
    public EnemySpawner(StageManager manager , StageDataSO stageData, Vector2 regionSize, float minDistanceFromPlayer)
    {
        _stageManager = manager ?? throw new ArgumentNullException(nameof(manager));
        _stageData = stageData ?? throw new ArgumentNullException(nameof(stageData));
        _regionSize = regionSize;
        _minDistanceFromPlayer = minDistanceFromPlayer;
        _screenSize = CalculateScreenSize(Camera.main);

        CurrentSpawnInterval = stageData.EnemySpawnInterval;
        CurrentMaxEnemySpawn = stageData.CurrentMaxEnemySpawn;

        _nextUnitKillQuota = stageData.SpawnKillQuota;
        _nextIntervalKillQuota = stageData.IntervalKillQuota;
        _addMaxEnemySpawn = stageData.AddMaxEnemySpawn;
        _removeIntervalEnemySpawn = stageData.RemoveIntervalEnemySpawn;
        
        CalculateTotalEnemySpawnChance();
        _stageManager.SetState(new StopState());
    }

    #endregion

    #region Spawn Control

    /// <summary>
    /// Begin spawning loop via SpawningState.
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log("Start Spawning");
        _stageManager.SetState(new SpawningState());
        Timer.Instance.ResumeTimer();
    }

    /// <summary>
    /// Stop all spawning via StopState.
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("Stop Spawning");
        _stageManager.SetState(new StopState());
        Timer.Instance.PauseTimer();
    }

    /// <summary>
    /// Pause spawn activity without clearing state.
    /// </summary>
    public void PauseSpawning()
    {
        Debug.Log("Pause Spawning");
        _stageManager.SetState(new PausedState());
    }

    /// <summary>
    /// Determines if current number of enemies allows more to spawn.
    /// </summary>
    public bool CanSpawn()
    {
        if (CurrentKillCount >= _stageData.KillQuota) return false;
        return enemies.Count(e => e.gameObject.activeInHierarchy) < CurrentMaxEnemySpawn;
    }
    
    /// <summary>
    /// Reset quota
    /// </summary>
    public void ResetQuota()
    {
        _killCount = 0;
    }

    /// <summary>
    /// Increase max enemies and reduce delay based on score.
    /// </summary>
    public void UpdateQuota()
    {
        var killCount = _stageManager.GetPlayerKill();

        if (killCount >= _nextUnitKillQuota)
        {
            CurrentMaxEnemySpawn =
                Mathf.Clamp(CurrentMaxEnemySpawn + _addMaxEnemySpawn, 1, _stageData.MaxEnemySpawnCap);
            _nextUnitKillQuota += _stageData.KillQuota;
        }

        if (killCount >= _nextIntervalKillQuota)
        {
            CurrentSpawnInterval = Mathf.Clamp(CurrentSpawnInterval - _removeIntervalEnemySpawn,
                _stageData.SpawnIntervalCap, CurrentSpawnInterval);
            _nextIntervalKillQuota += _stageData.IntervalKillQuota;
        }
    }

    #endregion

    #region Enemy Spawning

    /// <summary>
    /// Handles full async spawn process for an event.
    /// </summary>
    public async UniTask SpawnEventEnemies(SpawnEventSO.SpawnEventData spawnData)
    {
        if (!ValidateSpawnData(spawnData)) return;
        await ApplyInitialDelay(spawnData);
        await LoopSpawnEnemies(spawnData);
    }

    /// <summary>
    /// Ensures event spawn data is valid.
    /// </summary>
    private bool ValidateSpawnData(SpawnEventSO.SpawnEventData data)
    {
        return data != null && data.EnemiesWithChance != null && data.EnemiesWithChance.Count > 0;
    }

    /// <summary>
    /// Waits for delay before spawning any enemies.
    /// </summary>
    private async UniTask ApplyInitialDelay(SpawnEventSO.SpawnEventData data)
    {
        if (data.SpawnDelayAll > 0)
            await UniTask.Delay(TimeSpan.FromSeconds(data.SpawnDelayAll));
    }

    /// <summary>
    /// Spawns multiple enemies in sequence with optional per-enemy delay.
    /// </summary>
    private async UniTask LoopSpawnEnemies(SpawnEventSO.SpawnEventData data)
    {
        var count = Mathf.Min(data.Positions.Count, data.EnemyCount);

        for (var i = 0; i < count; i++)
        {
            var pos = data.Positions[i];
            
            var enemyData = GetRandomEnemy(data.EnemiesWithChance, 
                e => e.SpawnChance, 
                e => e.GetCharacterData());
            
            SpawnEnemyWithPosition(enemyData , pos, data);

            if (data.SpawnDelayPerEnemy)
            {
                var delay = CalculatePerEnemyDelay(i, count, data);
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
        }
    }

    /// <summary>
    /// Spawns 1 enemy at position with visual effect and setup.
    /// </summary>
    private void SpawnEnemyWithPosition(EnemyController enemyType , Vector2 position, SpawnEventSO.SpawnEventData data)
    {
        if (data.SpawnEffectPrefab != null)
            _spawnerService.Spawn(data.SpawnEffectPrefab, position, Quaternion.identity);

        var enemyObj = _spawnerService.Spawn(
            enemyType.gameObject,
            position,
            Quaternion.identity,
            _stageManager.GetEnemyParent()
        );
            
        //Deactivate Collider on Spawn
        enemyObj.GetComponent<CircleCollider2D>().isTrigger = true;

        if (enemyObj == null || !enemyObj.TryGetComponent(out EnemyController enemyController)) return;
            
        enemies.Add(enemyController);
        enemyController.HealthSystem.OnDead += () => DespawnEnemy(enemyController);
        OnEnemySpawned?.Invoke(enemyController);
    }

    /// <summary>
    /// Computes delay between each spawned enemy.
    /// </summary>
    private float CalculatePerEnemyDelay(int index, int total, SpawnEventSO.SpawnEventData data)
    {
        float t = (float)index / Mathf.Max(1, total - 1);
        return Mathf.Lerp(data.MinDelay, data.MaxDelay, data.DelayCurve.Evaluate(t));
    }

    /// <summary>
    /// Spawns a single normal enemy at random off-screen position.
    /// </summary>
    public void SpawnEnemy()
    {
        if (!CanSpawn()) return;

        var currentTime = Timer.Instance.GlobalTimerDown;

        var availableEnemies = _stageData.Enemies
            .Where(e => e.IsAvailableAtTime(currentTime))
            .ToList();

        if (availableEnemies.Count == 0) return;

        var enemyType = GetRandomEnemy(
            availableEnemies,
            e => e is EnemyProperties dynamicEnemy ? dynamicEnemy.GetCurrentSpawnChance(currentTime) : e.GetSpawnChance(),
            e => e.GetCharacterData()
        );
        var spawnPosition = GetRandomSpawnPosition(_stageManager.GetPlayerPosition());
        
        var enemyObj = _spawnerService.Spawn(enemyType.gameObject, spawnPosition, Quaternion.identity, _stageManager.GetEnemyParent());
        enemyObj.GetComponent<CircleCollider2D>().isTrigger = true;

        if (!enemyObj.TryGetComponent(out EnemyController enemyController)) return;
            
        enemies.Add(enemyController);
        enemyController.HealthSystem.OnDead += () => DespawnEnemy(enemyController);
        OnEnemySpawned?.Invoke(enemyController);
    }



    /// <summary>
    /// Removes enemy from world and pool.
    /// </summary>
    public void DespawnEnemy(EnemyController enemy)
    {
        if (!enemies.Contains(enemy)) return;
        enemy.HealthSystem.OnDead -= () => DespawnEnemy(enemy);
        enemy.ResetAllDependentBehavior();
        enemies.Remove(enemy);
        //PowerUpSpawnerManager.Instance.OnEnemyDefeated();
        OnEnemyDespawned?.Invoke(enemy);
        _spawnerService.Despawn(enemy.gameObject);
        if (_killCount < _stageData.KillQuota) _killCount += 1;
        
        SoulItemManagerSpawner.Instance.SpawnExpDrop(enemy.CharacterData.ExpDrop, enemy.transform.position);
        
    }

    /// <summary>
    /// Instantiates and despawns enemies into the pool ahead of time.
    /// </summary>
    public void Prewarm(StageDataSO stageData, Transform enemyParent)
    {
        foreach (var enemyData in stageData.Enemies)
        {
            for (var i = 0; i < stageData.PreObjectSpawn; i++)
            {
                var enemy = _spawnerService.Spawn(enemyData.EnemyType.gameObject, Vector3.zero,
                    Quaternion.identity, enemyParent, true);
                if (!enemy.TryGetComponent(out EnemyController enemyController)) return;
                enemies.Add(enemyController);
                _spawnerService.Despawn(enemy);
            }
        }

        foreach (var spawnEvent in stageData.SpawnEvents)
        foreach (var enemyData in spawnEvent.EventEnemies)
        {
            if (spawnEvent is not SpawnEventSO eventSO) continue;
            for (var i = 0; i < eventSO.EnemyCount; i++)
            {
                var enemy = _spawnerService.Spawn(enemyData.EnemyType.gameObject, Vector3.zero, Quaternion.identity,
                    enemyParent,
                    true);
                if (!enemy.TryGetComponent(out EnemyController controller)) continue;
                enemies.Add(controller);
                _spawnerService.Despawn(enemy);
            }
        }
    }


    #endregion

    #region Utility Methods

    /// <summary>
    /// Calculates total chance sum used in weighted selection.
    /// </summary>
    private void CalculateTotalEnemySpawnChance()
    {
        _totalEnemySpawnChance = _stageData.Enemies.Sum(enemy => enemy.SpawnChance);
    }

    /// <summary>
    /// Selects a random enemy based on weighted probability with customizable chance calculation.
    /// </summary>
    /// <typeparam name="T">Type of the enemy data (must implement IWeightedEnemy or SpawnEnemyProperties)</typeparam>
    /// <typeparam name="TResult">Type of the result (EnemyController or IEnemyData)</typeparam>
    /// <param name="enemies">List of enemies to choose from</param>
    /// <param name="getChance">Function to calculate spawn chance for an enemy</param>
    /// <param name="getResult">Function to extract result data from an enemy</param>
    /// <returns>Selected enemy data or null if the list is empty</returns>
    public static TResult GetRandomEnemy<T, TResult>(List<T> enemies, Func<T, float> getChance, Func<T, TResult> getResult)
    {
        if (enemies == null || enemies.Count == 0) return default;

        var totalChance = enemies.Sum(getChance);
        if (totalChance <= 0) return getResult(enemies[0]);

        var randomValue = Random.Range(0f, totalChance);
        var cumulative = 0f;

        foreach (var enemy in enemies)
        {
            cumulative += getChance(enemy);
            if (randomValue <= cumulative)
                return getResult(enemy);
        }

        return getResult(enemies[0]);
    }

    
    /// <summary>
    /// Chooses a spawn position outside player's view.
    /// </summary>
    private Vector2 GetRandomSpawnPosition(Vector2 playerPosition)
    {
        var spawnPosition = CalculateOffScreenPosition(playerPosition);
        return SpawnUtility.ClampToBounds(spawnPosition, _regionSize);
    }

    /// <summary>
    /// Finds an off-screen position using random angle and distance.
    /// </summary>
    private Vector2 CalculateOffScreenPosition(Vector2 playerPosition)
    {
        Vector2 spawnPosition;
        var attempts = 0;

        do
        {
            var angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
            var radius = UnityEngine.Random.Range(_minDistanceFromPlayer, _minDistanceFromPlayer + _screenSize.x);
            spawnPosition = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            attempts++;
        } while (SpawnUtility.IsOnScreen(spawnPosition, playerPosition, _screenSize) && attempts < MaxSpawnPositionAttempts);

        return spawnPosition;
    }

    /// <summary>
    /// Returns the camera's current screen size in world units.
    /// </summary>
    private Vector2 CalculateScreenSize(Camera camera)
    {
        if (camera == null) return Vector2.zero;
        var screenHeight = camera.orthographicSize * 2f;
        var screenWidth = screenHeight * camera.aspect;
        return new Vector2(screenWidth, screenHeight);
    }

    #endregion
}