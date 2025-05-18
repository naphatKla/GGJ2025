using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using Manager;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class EnemySpawner
{
    #region Fields

    private const int MaxSpawnPositionAttempts = 10;
    private readonly IEnemySpawnerView _spawnerView;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;
    private readonly Vector2 _screenSize;
    private readonly SpawnEventManager _spawnEventManager;

    private ISpawnState _currentState;
    private float _totalEnemySpawnChance;
    private float _nextUnitScoreQuota;
    private float _nextIntervalScoreQuota;
    
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();

    /// <summary>
    /// List of active normal enemies.
    /// </summary>
    public readonly HashSet<EnemyController> enemies = new();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the current spawn interval for normal enemies.
    /// </summary>
    public float CurrentSpawnInterval { get; private set; }

    /// <summary>
    /// Gets or sets the maximum number of normal enemies that can spawn.
    /// </summary>
    public int CurrentMaxEnemySpawn { get; private set; }

    /// <summary>
    /// Gets the interval for checking world events.
    /// </summary>
    public float EventIntervalCheck => _stageData.EventIntervalCheck;
    
    public event Action<EnemyController> OnEnemySpawned;
    
    public event Action<EnemyController> OnEnemyDespawned;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the enemy spawner with view, stage data, region size, and minimum player distance.
    /// </summary>
    /// <param name="view">The view interface for spawning enemies.</param>
    /// <param name="stageData">The stage data containing enemy and spawn settings.</param>
    /// <param name="regionSize">The size of the spawn region.</param>
    /// <param name="minDistanceFromPlayer">The minimum distance from the player for spawning.</param>
    /// <exception cref="ArgumentNullException">Thrown if view or stageData is null.</exception>
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

    #region Unity Methods
    
    /// <summary>
    /// Updates the current spawn state.
    /// </summary>
    public void Update()
    {
        _currentState?.Update(this);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts spawning enemies and triggers the initial world event.
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log("Start Spawning");
        SetState(new SpawningState());
    }

    /// <summary>
    /// Stops spawning enemies.
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("Stop Spawning");
        SetState(new StopState());
    }

    /// <summary>
    /// Pauses spawning enemies.
    /// </summary>
    public void PauseSpawning()
    {
        Debug.Log("Pause Spawning");
        SetState(new PausedState());
    }

    /// <summary>
    /// Checks if more normal enemies can be spawned based on the current limit.
    /// </summary>
    /// <returns>True if the enemy count is below the maximum limit.</returns>
    public bool CanSpawn()
    {
        return enemies.Count(e => e.gameObject.activeInHierarchy) < CurrentMaxEnemySpawn;
    }
    
    public async void SpawnEventEnemies(SpawnEventSO.SpawnEventData spawnData, Action<EnemyController> onEnemySpawned)
    {
        if (spawnData == null || spawnData.Enemies == null || spawnData.Enemies.Count == 0) { return; }
        if (spawnData.SpawnDelayAll > 0)
            await UniTask.Delay(TimeSpan.FromSeconds(spawnData.SpawnDelayAll));

        var count = Math.Min(spawnData.Positions.Count, spawnData.EnemyCount);
        for (var i = 0; i < count; i++)
        {
            var pos = spawnData.Positions[i];
            var enemyData = spawnData.Enemies[UnityEngine.Random.Range(0, spawnData.Enemies.Count)];

            if (enemyData == null) continue;
            
            if (spawnData.SpawnEffectPrefab != null)
                _spawnerService.Spawn(spawnData.SpawnEffectPrefab, pos, Quaternion.identity);
            
            var enemyObj = _spawnerService.Spawn(
                enemyData.EnemyController.gameObject,
                pos,
                Quaternion.identity,
                _spawnerView.GetEnemyParent(),
                false
            );

            if (enemyObj != null && enemyObj.TryGetComponent(out EnemyController enemyController))
            {
                enemyController.ResetAllDependentBehavior();
                enemies.Add(enemyController);
                enemyController.HealthSystem.OnThisCharacterDead += () => DespawnEnemy(enemyController);
                OnEnemySpawned?.Invoke(enemyController);
            }

            if (spawnData.SpawnDelayPerEnemy)
            {
                var t = (float)i / Mathf.Max(1, count - 1);
                var delay = Mathf.Lerp(spawnData.MinDelay, spawnData.MaxDelay, spawnData.DelayCurve.Evaluate(t));
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
        }
    }


    /// <summary>
    /// Spawns a single normal enemy at a random off-screen position.
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

        if (!enemyObj.TryGetComponent(out EnemyController enemyController)) { return; }
        enemies.Add(enemyController);
        enemyController.HealthSystem.OnThisCharacterDead += () => DespawnEnemy(enemyController);
        OnEnemySpawned?.Invoke(enemyController);
    }
  
    /// <summary>
    /// Despawn Enemy and remove from list
    /// </summary>
    /// <param name="enemy"></param>
    public void DespawnEnemy(EnemyController enemy)
    {
        if (!enemies.Contains(enemy)) return;
        enemy.HealthSystem.OnThisCharacterDead -= () => DespawnEnemy(enemy);
        enemy.ResetAllDependentBehavior();
        enemies.Remove(enemy);
        _spawnerService.Despawn(enemy.gameObject);
        OnEnemyDespawned?.Invoke(enemy);
        _spawnEventManager.onDespawntrigger?.Invoke();
    }
    
    /// <summary>
    /// Setting up the enemy pool.
    /// </summary>
    // ใน EnemySpawner.cs, แก้ไข Prewarm
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
                foreach (var enemyData in spawnEvent.EventEnemies)
                {
                    var count = eventSO.EnemySpawnEventCount;
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

    /// <summary>
    /// Triggers a world event, optionally bypassing the cooldown.
    /// </summary>
    /// <param name="bypassCooldown">If true, ignores the event cooldown.</param>
    public void TriggerSpawnEvent(bool bypassCooldown = false, bool noChance = false)
    {
        _spawnEventManager.TriggerSpawnEvent(bypassCooldown, enemies, noChance);
    }

    /// <summary>
    /// Updates spawn limits and interval based on the player's score.
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
    /// check state of spawner and return
    /// </summary>
    /// <returns></returns>
    public bool IsStoppedOrPaused()
    {
        return _currentState is StopState || _currentState is PausedState;
    }
    
    /// <summary>
    /// Updates the timer triggers in the spawn event manager.
    /// </summary>
    public void UpdateTimerTriggers()
    {
        _spawnEventManager.UpdateTimerTriggers(enemies);
    }
    
    /// <summary>
    /// Updates the kill triggers in the spawn event manager.
    /// </summary>
    public void UpdateKillTriggers()
    {
        _spawnEventManager.UpdateKillTriggers(enemies);
    }

    #endregion

    #region Private Methods

  

    /// <summary>
    /// Transitions to a new spawn state, handling exit and entry.
    /// </summary>
    /// <param name="newState">The new state to enter.</param>
    private void SetState(ISpawnState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    /// <summary>
    /// Calculates the total spawn chance for all enemies.
    /// </summary>
    private void CalculateTotalEnemySpawnChance()
    {
        _totalEnemySpawnChance = _stageData.Enemies.Sum(enemy => enemy.SpawnChance);
    }

    /// <summary>
    /// Selects a random enemy based on spawn chance weights.
    /// </summary>
    /// <returns>The selected enemy data.</returns>
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
    /// Generates a random off-screen spawn position.
    /// </summary>
    /// <param name="playerPosition">The player's current position.</param>
    /// <returns>A random spawn position outside the screen.</returns>
    private Vector2 GetRandomSpawnPosition(Vector2 playerPosition)
    {
        var spawnPosition = CalculateOffScreenPosition(playerPosition);
        return SpawnUtility.ClampToBounds(spawnPosition, _regionSize);
    }

    /// <summary>
    /// Calculates an off-screen position for spawning.
    /// </summary>
    /// <param name="playerPosition">The player's current position.</param>
    /// <returns>An off-screen position.</returns>
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
    /// Calculates the screen size based on the camera's properties.
    /// </summary>
    /// <param name="camera">The camera to use.</param>
    /// <returns>The screen size in world units.</returns>
    private Vector2 CalculateScreenSize(Camera camera)
    {
        if (camera == null) return Vector2.zero;
        var screenHeight = camera.orthographicSize * 2f;
        var screenWidth = screenHeight * camera.aspect;
        return new Vector2(screenWidth, screenHeight);
    }

    #endregion
}