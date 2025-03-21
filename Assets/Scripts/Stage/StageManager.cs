using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

public class StageManager : MonoBehaviour, IEnemySpawnerView
{
    #region Variable

    [SerializeField] private List<StageDataSO> stageData = new();
    [SerializeField] private int currentStageIndex;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private Transform currentBackground;
    [SerializeField] private Vector2 regionSize = Vector2.zero;
    [SerializeField] private float minDistanceFromPlayer = 20f;

    private readonly List<GameObject> enemies = new();
    private EnemySpawner spawner;

    private StageDataSO CurrentStage => stageData.Count > 0 && currentStageIndex < stageData.Count
        ? stageData[currentStageIndex]
        : null;

    #endregion

    #region Initialization

    /// <summary>
    ///     Sets up enemy parent and spawner with the current stage.
    /// </summary>
    private void Awake()
    {
        if (enemyParent == null) enemyParent = new GameObject("EnemyParent").transform;
        if (stageData.Count == 0 || CurrentStage == null || CurrentStage.SpawnTypes.Length == 0) return;
        EnemyPoolCreated();
        spawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
    }

    /// <summary>
    ///     Starts spawning and sets the current stage’s background.
    /// </summary>
    private void Start()
    {
        spawner.StartSpawning();
        if (currentBackground != null && CurrentStage != null)
            currentBackground.GetComponent<SpriteRenderer>().sprite = CurrentStage.Background;
    }

    #endregion
    
    #region Created Enemy Pool
    private void EnemyPoolCreated()
    {
        var prewarmedEnemies = new List<GameObject>();
        foreach (var enemyData in stageData[currentStageIndex].Enemies)
            for (var i = 0; i < stageData[currentStageIndex].MaxEnemySpawnCap; i++)
            {
                var enemy = PoolManager.Instance.Spawn(enemyData.EnemyPrefab, Vector3.zero, Quaternion.identity);
                enemy.transform.SetParent(enemyParent);
                prewarmedEnemies.Add(enemy);
            }
        foreach (var enemy in prewarmedEnemies) PoolManager.Instance.Despawn(enemy);
    }
    #endregion

    #region Update

    /// <summary>
    ///     Updates the spawner every frame.
    /// </summary>
    private void Update()
    {
        spawner?.Update();
    }

    #endregion

    #region IEnemySpawner Implementation

    /// <summary>
    ///     Creates an enemy and adds it to the list.
    /// </summary>
    public void SpawnEnemy(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent)
    {
        var enemy = PoolManager.Instance.Spawn(prefab, position, rotation);
        enemies.Add(enemy);
    }
    
    /// <summary>
    ///     Remove from enemy list
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Contains(enemy)) { enemies.Remove(enemy); }
    }

    /// <summary>
    ///     Returns the parent object for enemies.
    /// </summary>
    public Transform GetEnemyParent()
    {
        return enemyParent;
    }

    /// <summary>
    ///     Counts active enemies in the scene.
    /// </summary>
    public int GetCurrentEnemyCount()
    {
        return enemies.Count(e => e != null && e.activeInHierarchy);
    }

    /// <summary>
    ///     Gets the player’s current position.
    /// </summary>
    public Vector2 GetPlayerPosition()
    {
        return PlayerCharacter.Instance != null ? PlayerCharacter.Instance.transform.position : Vector2.zero;
    }

    /// <summary>
    ///     Gets the player’s current score.
    /// </summary>
    public float GetPlayerScore()
    {
        return PlayerCharacter.Instance != null ? PlayerCharacter.Instance.Score : 0f;
    }

    /// <summary>
    ///     Sets the background image.
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (currentBackground != null)
            currentBackground.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    #endregion

    #region Gizmos

    /// <summary>
    ///     Draws a red box in the editor for the spawn area.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, regionSize);
    }

    #endregion

    #region Control Methods

    /// <summary>
    ///     Starts enemy spawning.
    /// </summary>
    [Button]
    public void StartSpawning()
    {
        ClearEnemies();
        EnemyPoolCreated();
        spawner?.StartSpawning();
    }

    /// <summary>
    ///     Stops enemy spawning.
    /// </summary>
    [Button]
    public void StopSpawning()
    {
        spawner?.StopSpawning();
    }

    /// <summary>
    ///     Pauses enemy spawning.
    /// </summary>
    [Button]
    public void PauseSpawning()
    {
        spawner?.PauseSpawning();
    }

    /// <summary>
    ///     Switches to a specific stage and starts spawning.
    /// </summary>
    [Button]
    public void SetStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stageData.Count) return;
        currentStageIndex = stageIndex;
        ClearEnemies();
        spawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        spawner.StartSpawning();
        SetBackground(CurrentStage.Background);
    }

    /// <summary>
    ///     Moves to the next stage and starts spawning.
    /// </summary>
    [Button]
    public void NextStage()
    {
        if (stageData.Count == 0 || currentStageIndex >= stageData.Count - 1)
        {
            StopSpawning();
            return;
        }

        currentStageIndex++;
        ClearEnemies();
        spawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        spawner.StartSpawning();
        SetBackground(CurrentStage.Background);
    }

    /// <summary>
    ///     Deletes all enemies and clears the list.
    /// </summary>
    [Button]
    public void ClearEnemies()
    {
        foreach (var enemy in enemies)
            if (enemy != null)
                PoolManager.Instance.Despawn(enemy);
        enemies.Clear();
    }

    #endregion
}