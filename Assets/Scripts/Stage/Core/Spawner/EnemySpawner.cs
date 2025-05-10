using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawner
{
    #region Fields

    private const int MaxSpawnPositionAttempts = 10;
    private readonly IEnemySpawnerView _spawnerView;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;
    private readonly Vector2 _screenSize;
    private readonly List<Vector2> _spawnPositionsPool = new();
    private readonly WorldEventManager _worldEventManager;

    private ISpawnState _currentState;
    private float _totalEnemySpawnChance;
    private float _nextUnitScoreQuota;
    private float _nextIntervalScoreQuota;
    
    private readonly EnemySpawnLogic _spawnLogic;
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();

    /// <summary>
    /// List of active normal enemies.
    /// </summary>
    public readonly HashSet<GameObject> enemies = new();

    /// <summary>
    /// List of active event enemies.
    /// </summary>
    public readonly List<GameObject> eventEnemies = new();

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
    
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyDespawned;

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

        _worldEventManager = new WorldEventManager(view, stageData, regionSize, minDistanceFromPlayer);

        CalculateTotalEnemySpawnChance();
        PreWarmPools();
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
        TriggerWorldEvent(true);
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
        return _spawnerView.GetCurrentEnemyCount() < CurrentMaxEnemySpawn;
    }

    /// <summary>
    /// Spawns a single normal enemy at a random off-screen position.
    /// </summary>
    public void SpawnEnemy()
    {
        if (!CanSpawn()) return;

        var enemyData = GetRandomEnemy();
        var spawnPosition = GetRandomSpawnPosition(_spawnerView.GetPlayerPosition());
        var enemy = _spawnerService.Spawn(
            enemyData.EnemyPrefab,
            spawnPosition,
            Quaternion.identity,
            _spawnerView.GetEnemyParent()
        );
        enemies.Add(enemy);
        OnEnemySpawned?.Invoke(enemy);
    }
    
    /// <summary>
    /// Despawn Enemy and remove from list
    /// </summary>
    /// <param name="enemy"></param>
    public void DespawnEnemy(GameObject enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            _spawnerService.Despawn(enemy);
            OnEnemyDespawned?.Invoke(enemy);
        }
    }

    /// <summary>
    /// Triggers a world event, optionally bypassing the cooldown.
    /// </summary>
    /// <param name="bypassCooldown">If true, ignores the event cooldown.</param>
    public void TriggerWorldEvent(bool bypassCooldown = false)
    {
        _worldEventManager.TriggerWorldEvent(bypassCooldown, eventEnemies);
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

    #endregion

    #region Private Methods

    /// <summary>
    /// Pre-warms the object pool for all enemy prefabs.
    /// </summary>
    private void PreWarmPools()
    {
        foreach (var enemy in _stageData.Enemies)
        {
            if (enemy.EnemyData?.EnemyPrefab != null)
            {
                _spawnerService.Spawn(enemy.EnemyData.EnemyPrefab, Vector3.zero, Quaternion.identity, _spawnerView.GetEnemyParent(), true);
                _spawnerService.ClearPool(_spawnerView.GetEnemyParent());
            }
        }
    }


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