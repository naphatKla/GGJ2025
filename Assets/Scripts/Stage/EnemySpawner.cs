using System.Collections.Generic;
using UnityEngine;

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

    private readonly IEnemySpawnerView view;
    private readonly StageDataSO stageData;
    private readonly Vector2 regionSize;
    private readonly float minDistanceFromPlayer;

    private ISpawnState currentState;
    private float totalEnemySpawnChance;
    private float totalEventChance;

    public float CurrentSpawnInterval { get; private set; }
    public int CurrentMaxEnemySpawn { get; private set; }
    public float EventIntervalCheck => stageData.EventIntervalCheck;
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
        this.view = view;
        this.stageData = stageData;
        this.regionSize = regionSize;
        this.minDistanceFromPlayer = minDistanceFromPlayer;
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
        return view.GetCurrentEnemyCount() < CurrentMaxEnemySpawn;
    }

    /// <summary>
    /// Spawns a single normal enemy at a random position.
    /// </summary>
    public void SpawnNormalEnemy()
    {
        if (!CanSpawn()) return;

        var enemyData = GetRandomEnemy();
        var spawnPosition = GetRandomSpawnPosition(view.GetPlayerPosition());
        view.SpawnEnemy(enemyData.EnemyPrefab, spawnPosition, Quaternion.identity, view.GetEnemyParent());
    }

    /// <summary>
    /// Triggers a world event, spawning multiple enemies based on the event's configuration.
    /// </summary>
    public void TriggerWorldEvent(bool bypassCooldown = false)
    {
        var worldEvent = GetRandomWorldEvent(bypassCooldown);
        if (worldEvent == null)
        {
            Debug.Log("No WorldEvent triggered (no available events)");
            return;
        }

        var spawnPositions = new List<Vector2>();
        worldEvent.GetSpawnPositions(view.GetPlayerPosition(), regionSize, minDistanceFromPlayer, 0, spawnPositions);

        foreach (var position in spawnPositions)
        {
            var enemyData = GetRandomRaidEnemy(worldEvent.RaidEnemies);
            view.SpawnEnemy(enemyData.EnemyPrefab, position, Quaternion.identity, view.GetEnemyParent(), isWorldEventEnemy: true);
        }

        worldEvent.OnSpawned();
        Debug.Log($"WorldEvent triggered with {spawnPositions.Count} enemies");
    }
    
    /// <summary>
    /// Updates spawn limits and speed based on player score.
    /// </summary>
    public void UpdateQuota()
    {
        var score = view.GetPlayerScore();

        if (score >= nextUnitScoreQuota)
        {
            CurrentMaxEnemySpawn = Mathf.Clamp(CurrentMaxEnemySpawn + 1, 1, stageData.MaxEnemySpawnCap);
            nextUnitScoreQuota += stageData.UnitScoreQuota;
        }

        if (score >= nextIntervalScoreQuota)
        {
            CurrentSpawnInterval =
                Mathf.Clamp(CurrentSpawnInterval - 0.1f, stageData.SpawnIntervalCap, CurrentSpawnInterval);
            nextIntervalScoreQuota += stageData.DecreaseSpawnInterval;
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
        foreach (var enemy in stageData.Enemies)
            totalEnemySpawnChance += enemy.SpawnChance;
    }

    /// <summary>
    /// Calculates total chance for picking world events randomly.
    /// </summary>
    private void CalculateTotalEventChance()
    {
        totalEventChance = 0f;
        foreach (var worldEvent in stageData.WorldEvents)
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

        foreach (var enemy in stageData.Enemies)
        {
            cumulativeChance += enemy.SpawnChance;
            if (randomValue <= cumulativeChance)
                return enemy.EnemyData;
        }

        return stageData.Enemies[0].EnemyData;
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
        foreach (var worldEvent in stageData.WorldEvents)
        {
            bool isCooldownActive = worldEvent.IsCooldownActive(Time.time);
            if (bypassCooldown || !isCooldownActive)
                availableEvents.Add(worldEvent);
            Debug.Log($"WorldEvent: {((WorldEventSO)worldEvent).Type}, Cooldown Active: {isCooldownActive}, Chance: {worldEvent.Chance}");
        }

        Debug.Log($"Available WorldEvents: {availableEvents.Count}");
        if (availableEvents.Count == 0)
        {
            Debug.LogWarning("No WorldEvents available. Check StageDataSO.worldEvent array or cooldown settings.");
            return null;
        }

        return availableEvents[Random.Range(0, availableEvents.Count)];
    }

    /// <summary>
    /// Generates a random spawn position around the player, ensuring it's off-screen and not too close.
    /// </summary>
    private Vector2 GetRandomSpawnPosition(Vector2 playerPosition)
    {
        var mainCamera = Camera.main;
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;
        var screenSize = new Vector2(screenWidth, screenHeight);

        Vector2 spawnPosition;
        var attempts = 0;
        const int maxAttempts = 10;

        do
        {
            // Random angle and radius for spawning around the player
            var angle = Random.Range(0f, 2f * Mathf.PI);
            var radius = Random.Range(minDistanceFromPlayer, minDistanceFromPlayer + screenWidth);
            spawnPosition = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            attempts++;
        } while (IsPositionOnScreen(spawnPosition, playerPosition, screenSize) && attempts < maxAttempts);

        // Clamp to region bounds
        spawnPosition.x = Mathf.Clamp(spawnPosition.x, -regionSize.x / 2, regionSize.x / 2);
        spawnPosition.y = Mathf.Clamp(spawnPosition.y, -regionSize.y / 2, regionSize.y / 2);

        return spawnPosition;
    }

    /// <summary>
    /// Checks if a position is within the camera's view.
    /// </summary>
    private bool IsPositionOnScreen(Vector2 position, Vector2 playerPosition, Vector2 screenSize)
    {
        var screenMin = playerPosition - screenSize / 2;
        var screenMax = playerPosition + screenSize / 2;
        return position.x >= screenMin.x && position.x <= screenMax.x &&
               position.y >= screenMin.y && position.y <= screenMax.y;
    }

    #endregion
}