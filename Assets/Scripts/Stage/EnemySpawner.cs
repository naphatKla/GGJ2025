using UnityEngine;

public interface IEnemySpawnerView
{
    void SpawnEnemy(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent);
    Transform GetEnemyParent();
    int GetCurrentEnemyCount();
    Vector2 GetPlayerPosition();
    void SetBackground(Sprite sprite);
    float GetPlayerScore();
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
    private float totalSpawnTypeChance;
    public float CurrentSpawnInterval { get; private set; }
    public int CurrentMaxEnemySpawn { get; private set; }
    private float nextUnitScoreQuota;
    private float nextIntervalScoreQuota;

    #endregion

    #region Initialization

    /// <summary>
    ///     Sets up the spawner with view, stage data, area size, and player distance. Starts stopped.
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
        CalculateTotalSpawnTypeChance();
        SetState(new StopState());
    }

    #endregion

    #region State Management

    /// <summary>
    ///     Starts spawning enemies and logs it.
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log("Start Spawning");
        SetState(new SpawningState());
    }

    /// <summary>
    ///     Stops spawning enemies and logs it.
    /// </summary>
    public void StopSpawning()
    {
        Debug.Log("Stop Spawning");
        SetState(new StopState());
    }

    /// <summary>
    ///     Pauses spawning enemies and logs it.
    /// </summary>
    public void PauseSpawning()
    {
        Debug.Log("Pause Spawning");
        SetState(new PausedState());
    }

    /// <summary>
    ///     Switches to a new state, running exit and enter steps.
    /// </summary>
    private void SetState(ISpawnState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    #endregion

    #region Spawning Logic

    /// <summary>
    ///     Updates the current state (e.g., spawns enemies if active).
    /// </summary>
    public void Update()
    {
        currentState?.Update(this);
    }

    /// <summary>
    ///     Checks if more enemies can spawn based on the limit.
    /// </summary>
    public bool CanSpawn()
    {
        return view.GetCurrentEnemyCount() < CurrentMaxEnemySpawn;
    }

    /// <summary>
    ///     Spawns one random enemy at a random spot.
    /// </summary>
    public void Spawn()
    {
        var enemyData = GetRandomEnemy();
        var spawnType = GetRandomSpawnType();
        var spawnPosition = spawnType.GetSpawnPosition(view.GetPlayerPosition(), regionSize, minDistanceFromPlayer);
        view.SpawnEnemy(enemyData.EnemyPrefab, spawnPosition, Quaternion.identity, view.GetEnemyParent());
        spawnType.OnSpawned();
    }

    #endregion

    #region Quota Management

    /// <summary>
    ///     Updates spawn limits and speed based on player score.
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

    #endregion

    #region Random Selection

    /// <summary>
    ///     Calculates total chance for picking enemies randomly.
    /// </summary>
    private void CalculateTotalEnemySpawnChance()
    {
        totalEnemySpawnChance = 0f;
        foreach (var enemy in stageData.Enemies)
            totalEnemySpawnChance += enemy.SpawnChance;
    }

    /// <summary>
    ///     Calculates total chance for picking spawn types randomly.
    /// </summary>
    private void CalculateTotalSpawnTypeChance()
    {
        totalSpawnTypeChance = 0f;
        foreach (var spawnType in stageData.SpawnTypes)
            if (!spawnType.IsCooldownActive(Time.time))
                totalSpawnTypeChance += spawnType.Chance;
    }

    /// <summary>
    ///     Picks a random enemy based on their chances.
    /// </summary>
    private IEnemyData GetRandomEnemy()
    {
        var randomValue = Random.Range(0, totalEnemySpawnChance);
        var cumulativeChance = 0f;

        foreach (var enemy in stageData.Enemies)
        {
            cumulativeChance += enemy.SpawnChance;
            if (randomValue <= cumulativeChance)
                return enemy;
        }

        return stageData.Enemies[0];
    }

    /// <summary>
    ///     Picks a random spawn type that’s not on cooldown.
    /// </summary>
    private ISpawnType GetRandomSpawnType()
    {
        CalculateTotalSpawnTypeChance();
        if (totalSpawnTypeChance <= 0) return stageData.SpawnTypes[0];

        var randomValue = Random.Range(0, totalSpawnTypeChance);
        var cumulativeChance = 0f;

        foreach (var spawnType in stageData.SpawnTypes)
            if (!spawnType.IsCooldownActive(Time.time))
            {
                cumulativeChance += spawnType.Chance;
                if (randomValue <= cumulativeChance)
                    return spawnType;
            }

        return stageData.SpawnTypes[0];
    }

    #endregion
}