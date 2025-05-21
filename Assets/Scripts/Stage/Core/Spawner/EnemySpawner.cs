using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner
{
    #region Fields

    // Core data and config references
    private const int MaxSpawnPositionAttempts = 10;
    private readonly IEnemySpawnerView _spawnerView;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;
    private readonly Vector2 _screenSize;
    private readonly SpawnEventManager _spawnEventManager;

    // Spawn state and progression tracking
    private ISpawnState _currentState;
    private float _totalEnemySpawnChance;
    private float _nextUnitScoreQuota;
    private float _nextIntervalScoreQuota;

    // Spawning interface
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();

    // Active enemies in the world
    public readonly HashSet<EnemyController> enemies = new();

    #endregion

    #region Properties

    // Current delay between normal spawns
    public float CurrentSpawnInterval { get; private set; }

    // Limit of how many enemies can be alive at once
    public int CurrentMaxEnemySpawn { get; private set; }

    // Time between event trigger checks
    public float EventIntervalCheck => _stageData.EventIntervalCheck;

    public event Action<EnemyController> OnEnemySpawned;
    public event Action<EnemyController> OnEnemyDespawned;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes all required spawn parameters and default state.
    /// </summary>
    public EnemySpawner(IEnemySpawnerView view, StageDataSO stageData, Vector2 regionSize, float minDistanceFromPlayer)
    {
        _spawnerView = view ?? throw new ArgumentNullException(nameof(view));
        _stageData = stageData ?? throw new ArgumentNullException(nameof(stageData));
        _regionSize = regionSize;
        _minDistanceFromPlayer = minDistanceFromPlayer;
        _screenSize = CalculateScreenSize(Camera.main);

        CurrentSpawnInterval = stageData.EnemySpawnInterval;
        CurrentMaxEnemySpawn = stageData.CurrentMaxEnemySpawn;

        _nextUnitScoreQuota = stageData.UnitScoreQuota;
        _nextIntervalScoreQuota = stageData.DecreaseSpawnInterval;

        _spawnEventManager = new SpawnEventManager(view, stageData, regionSize, minDistanceFromPlayer, _spawnerService);

        CalculateTotalEnemySpawnChance();
        SetState(new StopState());
    }

    #endregion

    #region State Handling

    /// <summary>
    /// Called each frame to update spawn behavior based on state.
    /// </summary>
    public void Update()
    {
        _currentState?.Update(this);
    }

    /// <summary>
    /// Change the active spawning state, calling exit/enter lifecycle.
    /// </summary>
    private void SetState(ISpawnState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    /// <summary>
    /// Returns true if the spawner is not actively spawning.
    /// </summary>
    public bool IsStoppedOrPaused()
    {
        return _currentState is StopState || _currentState is PausedState;
    }

    #endregion

    #region Spawn Control

    /// <summary>
    /// Begin spawning loop via SpawningState.
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log("Start Spawning");
        SetState(new SpawningState());
    }

    /// <summary>
    /// Stop all spawning via StopState.
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("Stop Spawning");
        SetState(new StopState());
    }

    /// <summary>
    /// Pause spawn activity without clearing state.
    /// </summary>
    public void PauseSpawning()
    {
        Debug.Log("Pause Spawning");
        SetState(new PausedState());
    }

    /// <summary>
    /// Determines if current number of enemies allows more to spawn.
    /// </summary>
    public bool CanSpawn()
    {
        return enemies.Count(e => e.gameObject.activeInHierarchy) < CurrentMaxEnemySpawn;
    }

    /// <summary>
    /// Triggers an event-based spawn from the event manager.
    /// </summary>
    public void TriggerSpawnEvent(bool bypassCooldown = false, bool noChance = false)
    {
        _spawnEventManager.TriggerSpawnEvent(bypassCooldown, noChance);
    }

    /// <summary>
    /// Increase max enemies and reduce delay based on score.
    /// </summary>
    public void UpdateQuota()
    {
        var score = _spawnerView.GetPlayerScore();

        if (score >= _nextUnitScoreQuota)
        {
            CurrentMaxEnemySpawn = Mathf.Clamp(CurrentMaxEnemySpawn + 1, 1, _stageData.MaxEnemySpawnCap);
            _nextUnitScoreQuota += _stageData.UnitScoreQuota;
        }

        if (score >= _nextIntervalScoreQuota)
        {
            CurrentSpawnInterval = Mathf.Clamp(CurrentSpawnInterval - 0.1f, _stageData.SpawnIntervalCap, CurrentSpawnInterval);
            _nextIntervalScoreQuota += _stageData.DecreaseSpawnInterval;
        }
    }

    /// <summary>
    /// Check timer-based event trigger points.
    /// </summary>
    public void UpdateTimerTriggers()
    {
        _spawnEventManager.UpdateTimerTriggers(enemies);
    }

    /// <summary>
    /// Check kill-count-based event trigger points.
    /// </summary>
    public void UpdateKillTriggers()
    {
        _spawnEventManager.UpdateKillTriggers(enemies);
    }

    #endregion

    #region Enemy Spawning

    /// <summary>
    /// Handles full async spawn process for an event.
    /// </summary>
    public async void SpawnEventEnemies(SpawnEventSO.SpawnEventData spawnData)
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
        int count = Mathf.Min(data.Positions.Count, data.EnemyCount);

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = data.Positions[i];
            IEnemyData enemyData = GetRandomEnemyByChance(data.EnemiesWithChance);

            if (enemyData == null) continue;

            SpawnSingleEnemy(enemyData, pos, data);

            if (data.SpawnDelayPerEnemy)
            {
                float delay = CalculatePerEnemyDelay(i, count, data);
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
        }
    }
    
    /// <summary>
    /// Random Chance Enemy Selection
    /// </summary>
    /// <param name="enemies"></param>
    /// <returns></returns>
    private IEnemyData GetRandomEnemyByChance(List<SpawnEnemyProperties> enemies)
    {
        if (enemies == null || enemies.Count == 0) return null;

        var totalChance = enemies.Sum(e => e.SpawnChance);
        var randomValue = Random.Range(0f, totalChance);
        var cumulative = 0f;

        foreach (var e in enemies)
        {
            cumulative += e.SpawnChance;
            if (randomValue <= cumulative)
                return e.EnemyData;
        }

        return enemies[0].EnemyData;
    }



    /// <summary>
    /// Spawns 1 enemy at position with visual effect and setup.
    /// </summary>
    private void SpawnSingleEnemy(IEnemyData enemyData, Vector2 position, SpawnEventSO.SpawnEventData data)
    {
        if (data.SpawnEffectPrefab != null)
            _spawnerService.Spawn(data.SpawnEffectPrefab, position, Quaternion.identity);

        var enemyObj = _spawnerService.Spawn(
            enemyData.EnemyController.gameObject,
            position,
            Quaternion.identity,
            _spawnerView.GetEnemyParent()
        );

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
        var enemyData = GetRandomEnemy();
        var spawnPosition = GetRandomSpawnPosition(_spawnerView.GetPlayerPosition());
        var enemyObj = _spawnerService.Spawn(
            enemyData.EnemyController.gameObject,
            spawnPosition,
            Quaternion.identity,
            _spawnerView.GetEnemyParent()
        );

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
        _spawnerService.Despawn(enemy.gameObject);
        OnEnemyDespawned?.Invoke(enemy);
        _spawnEventManager.UpdateKillCount();
    }

    /// <summary>
    /// Instantiates and despawns enemies into the pool ahead of time.
    /// </summary>
    public void Prewarm(StageDataSO stageData, Transform enemyParent)
    {
        foreach (var enemyData in stageData.Enemies)
        {
            for (var i = 0; i < enemyData.PreObjectSpawn; i++)
            {
                var enemy = _spawnerService.Spawn(enemyData.EnemyData.EnemyController.gameObject, Vector3.zero,
                    Quaternion.identity, enemyParent, true);
                if (!enemy.TryGetComponent(out EnemyController enemyController)) return;
                enemies.Add(enemyController);
                _spawnerService.Despawn(enemy);
            }
        }

        foreach (var spawnEvent in stageData.SpawnEvents)
            if (spawnEvent is SpawnEventSO eventSO)
                foreach (var enemyData in eventSO.EventEnemies)
                {
                    var count = eventSO.EnemyCount;
                    for (var i = 0; i < count; i++)
                    {
                        var enemy = _spawnerService.Spawn(enemyData.EnemyController.gameObject, Vector3.zero,
                            Quaternion.identity, enemyParent, true);
                        if (!enemy.TryGetComponent(out EnemyController enemyController)) return;
                        enemies.Add(enemyController);
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
    /// Selects a random enemy based on weighted probability.
    /// </summary>
    private IEnemyData GetRandomEnemy()
    {
        if (_totalEnemySpawnChance <= 0) return _stageData.Enemies[0].EnemyData;

        var randomValue = UnityEngine.Random.Range(0f, _totalEnemySpawnChance);
        var cumulativeChance = 0f;

        foreach (var enemy in _stageData.Enemies)
        {
            cumulativeChance += enemy.SpawnChance;
            if (randomValue <= cumulativeChance)
                return enemy.EnemyData;
        }

        return _stageData.Enemies[0].EnemyData;
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