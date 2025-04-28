using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public interface IEnemySpawnerView
{
    void SpawnEnemy(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent, bool isWorldEventEnemy = false);
    Transform GetEnemyParent();
    int GetCurrentEnemyCount();
    Vector2 GetPlayerPosition();
    void SetBackground(Sprite sprite);
    float GetPlayerScore();
    void AddWorldEventEnemy(GameObject enemy);
}

public class EnemySpawner
{
    #region Properties

    private const int MaxSpawnPositionAttempts = 10;
    
    private readonly IEnemySpawnerView _spawnerView;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;
    private readonly Vector2 _screenSize;
    private readonly List<Vector2> _spawnPositionsPool = new List<Vector2>();
    private readonly List<IWorldEvent> _availableEventsPool = new List<IWorldEvent>();

    private ISpawnState currentState;
    private float totalEnemySpawnChance;
    private float totalEventChance;

    public float CurrentSpawnInterval { get; private set; }
    public int CurrentMaxEnemySpawn { get; private set; }
    public float EventIntervalCheck => _stageData.EventIntervalCheck;
    private float nextUnitScoreQuota;
    private float nextIntervalScoreQuota;
    
    public readonly List<GameObject> enemies = new();
    public readonly List<GameObject> eventEnemies = new();

    #endregion

    #region Unity Methods

    public void Update()
    {
        currentState?.Update(this);
    }
    
    #endregion

    #region Methods

    /// <summary>
    /// Initialization Sets up the spawner with view, stage data, area size, and player distance. Starts stopped.
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
        
        nextUnitScoreQuota = stageData.UnitScoreQuota;
        nextIntervalScoreQuota = stageData.DecreaseSpawnInterval;
        
        CalculateTotalEnemySpawnChance();
        CalculateTotalEventChance();
        SetState(new StopState());
    }
    
    /// <summary>
    /// Starts spawning enemies and logs it.
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log("Start Spawning");
        SetState(new SpawningState());
        TriggerWorldEvent(true);
    }

    /// <summary>
    /// Stops spawning enemies and logs it.
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("Stop Spawning");
        SetState(new StopState());
    }

    /// <summary>
    /// Pauses spawning enemies and logs it.
    /// </summary>
    public void PauseSpawning()
    {
        Debug.Log("Pause Spawning");
        SetState(new PausedState());
    }
    
    /// <summary>
    /// Checks if more normal enemies can spawn based on the limit.
    /// </summary>
    public bool CanSpawn()
    {
        return _spawnerView.GetCurrentEnemyCount() < CurrentMaxEnemySpawn;
    }

    /// <summary>
    /// Spawns a single normal enemy at a random position if spawning is allowed.
    /// </summary>
    public void SpawnNormalEnemy()
    {
        if (!CanSpawn()) return;
        var enemyData = GetRandomEnemy();
        var spawnPosition = GetRandomSpawnPosition(_spawnerView.GetPlayerPosition());
        _spawnerView.SpawnEnemy(enemyData.EnemyPrefab, spawnPosition, Quaternion.identity, _spawnerView.GetEnemyParent());
    }

    /// <summary>
    /// Triggers a world event, spawning multiple enemies based on the event's configuration.
    /// </summary>
    public void TriggerWorldEvent(bool bypassCooldown = false)
    {
        var worldEvent = GetRandomWorldEvent(bypassCooldown);
        if (worldEvent == null) { return; }

        _spawnPositionsPool.Clear();
        worldEvent.GetSpawnPositions(_spawnerView.GetPlayerPosition(), _regionSize, _minDistanceFromPlayer, 0, _spawnPositionsPool);

        foreach (var position in _spawnPositionsPool)
        {
            var enemyData = GetRandomRaidEnemy(worldEvent.RaidEnemies);
            _spawnerView.SpawnEnemy(enemyData.EnemyPrefab, position, Quaternion.identity, _spawnerView.GetEnemyParent(), isWorldEventEnemy: true);
        }
        Debug.Log($"WorldEvent: {((WorldEventSO)worldEvent).Type}, Cooldown Active: {worldEvent.IsCooldownActive(Time.time)}, Chance: {worldEvent.Chance}");
        worldEvent.OnSpawned();
    }
    
    /// <summary>
    /// Updates spawn limits and speed based on player score.
    /// </summary>
    public void UpdateQuota()
    {
        var score = _spawnerView.GetPlayerScore();

        if (score >= nextUnitScoreQuota)
        {
            CurrentMaxEnemySpawn = Mathf.Clamp(CurrentMaxEnemySpawn + 1, 1, _stageData.MaxEnemySpawnCap);
            nextUnitScoreQuota += _stageData.UnitScoreQuota;
        }

        if (score >= nextIntervalScoreQuota)
        {
            CurrentSpawnInterval = Mathf.Clamp(CurrentSpawnInterval - 0.1f, _stageData.SpawnIntervalCap, CurrentSpawnInterval);
            nextIntervalScoreQuota += _stageData.DecreaseSpawnInterval;
        }
    }

    /// <summary>
    /// Switches to a new state, running exit and enter steps.
    /// </summary>
    private void SetState(ISpawnState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }
    
    /// <summary>
    /// Calculates total chance for picking enemies randomly.
    /// </summary>
    private void CalculateTotalEnemySpawnChance()
    {
        totalEnemySpawnChance = 0f;
        foreach (var enemy in _stageData.Enemies)
            totalEnemySpawnChance += enemy.SpawnChance;
    }

    /// <summary>
    /// Calculates total chance for picking world events randomly.
    /// </summary>
    private void CalculateTotalEventChance()
    {
        totalEventChance = 0f;
        foreach (var worldEvent in _stageData.WorldEvents)
            if (!worldEvent.IsCooldownActive(Time.time))
                totalEventChance += worldEvent.Chance;
    }

    /// <summary>
    /// Picks a random enemy based on their chances.
    /// </summary>
    private IEnemyData GetRandomEnemy()
    {
        var randomValue = Random.Range(0, totalEnemySpawnChance);
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
    /// Picks a random enemy from the raid enemies list.
    /// </summary>
    private IEnemyData GetRandomRaidEnemy(List<IEnemyData> raidEnemies)
    {
        if (raidEnemies == null || raidEnemies.Count == 0)
            return GetRandomEnemy();

        return raidEnemies[Random.Range(0, raidEnemies.Count)];
    }

    /// <summary>
    /// Picks a random world event, optionally bypassing cooldown.
    /// </summary>
    private IWorldEvent GetRandomWorldEvent(bool bypassCooldown)
    {
        var availableEvents = new List<IWorldEvent>();
        foreach (var worldEvent in _stageData.WorldEvents)
        {
            bool isCooldownActive = worldEvent.IsCooldownActive(Time.time);
            if (bypassCooldown || !isCooldownActive)
                availableEvents.Add(worldEvent);
        }
        
        if (availableEvents.Count == 0) { return null; }
        return availableEvents[Random.Range(0, availableEvents.Count)];
    }

    private Vector2 GetRandomSpawnPosition(Vector2 playerPosition)
    {
        var spawnPosition = CalculateOffScreenPosition(playerPosition);
        return ClampToRegionBounds(spawnPosition);
    }
    
    private Vector2 CalculateOffScreenPosition(Vector2 playerPosition)
    {
        Vector2 spawnPosition;
        var attempts = 0;

        do
        {
            var angle = Random.Range(0f, 2f * Mathf.PI);
            var radius = Random.Range(_minDistanceFromPlayer, _minDistanceFromPlayer + _screenSize.x);
            spawnPosition = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            attempts++;
        } while (IsPositionOnScreen(spawnPosition, playerPosition, _screenSize) && attempts < MaxSpawnPositionAttempts);

        return spawnPosition;
    }

    private Vector2 ClampToRegionBounds(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.x, -_regionSize.x / 2, _regionSize.x / 2),
            Mathf.Clamp(position.y, -_regionSize.y / 2, _regionSize.y / 2)
        );
    }

    private bool IsPositionOnScreen(Vector2 position, Vector2 playerPosition, Vector2 screenSize)
    {
        var screenMin = playerPosition - screenSize / 2;
        var screenMax = playerPosition + screenSize / 2;
        return position.x >= screenMin.x && position.x <= screenMax.x &&
               position.y >= screenMin.y && position.y <= screenMax.y;
    }

    private Vector2 CalculateScreenSize(Camera camera)
    {
        if (camera == null) return Vector2.zero;
        var screenHeight = camera.orthographicSize * 2f;
        var screenWidth = screenHeight * camera.aspect;
        return new Vector2(screenWidth, screenHeight);
    }
    
    #endregion
}