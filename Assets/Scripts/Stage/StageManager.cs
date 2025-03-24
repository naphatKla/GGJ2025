using System;
using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

public class StageManager : MonoBehaviour, IEnemySpawnerView
{
    #region Inspector & Variable
    [Title("Stage Data")] [Tooltip("You can add more than 1 stage and change by Function Set Stage or Next Stage.")]
    [SerializeField] private List<StageDataSO> stageData = new();
    [Title("Stage Current Setting")] [Tooltip("The index of the current stage in the stageData list.")]
    [SerializeField] private int currentStageIndex;
    [Tooltip("The current background image.")]
    [SerializeField] private Sprite currentBackground;
    [Tooltip("Parent of the enemy to spawn prefab.")]
    [SerializeField] private Transform enemyParent;
    [Title("Stage Area Setting")] 
    [Tooltip("Region of the spawn area.")]
    [SerializeField] private Vector2 regionSize = Vector2.zero;
    [Tooltip("Minimum spawn area from the player.")]
    [SerializeField] private float minDistanceFromPlayer = 20f;
    
    private EnemySpawner spawner;
    private StageDataSO CurrentStage => stageData.Count > 0 && currentStageIndex < stageData.Count
        ? stageData[currentStageIndex]
        : null;
    
    #endregion

    #region Unity Methods
    
    private void Awake()
    {
        //Sets up enemy parent and spawner with the current stage.
        if (enemyParent == null) enemyParent = new GameObject("EnemyParent").transform;
        if (stageData.Count == 0 || CurrentStage == null || CurrentStage.SpawnTypes.Length == 0) return;
        EnemyPoolCreated();
        spawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
    }
    
    private void Start()
    {
        spawner.StartSpawning();
        SetBackground(currentBackground);
    }
    private void Update()
    {
        spawner?.Update();
    }

    #endregion

    #region Methods
    /// <summary>
    ///     Creates an enemy and adds it to the list.
    /// </summary>
    public void SpawnEnemy(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent)
    {
        
        var enemy = PoolManager.Instance.Spawn(prefab, position, rotation);
        spawner.enemies.Add(enemy);
    }
    
    /// <summary>
    ///     Remove from enemy list
    /// </summary>
    public void RemoveListEnemy(GameObject enemy)
    {
        if (spawner.enemies.Contains(enemy)) { spawner.enemies.Remove(enemy); }
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
        return spawner.enemies.Count(e => e != null && e.activeInHierarchy);
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
            currentBackground = stageData[currentStageIndex].Background;
    }
    
    /// <summary>
    ///     Setting up the enemy pool
    /// </summary>
    private void EnemyPoolCreated()
    {
        foreach (var enemyData in stageData[currentStageIndex].Enemies)
        {
            for (var i = 0; i < stageData[currentStageIndex].MaxEnemySpawnCap; i++)
            {
                var enemy = PoolManager.Instance.Spawn(enemyData.EnemyData.EnemyPrefab, Vector3.zero, Quaternion.identity, true); // forceNew = true
                enemy.transform.SetParent(enemyParent);
                PoolManager.Instance.Despawn(enemy);
            }
        }
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
        EnemyPoolCreated();
        spawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        spawner.StartSpawning();
        SetBackground(CurrentStage.Background);
    }
    
    /// <summary>
    ///     Reset the stage and starts spawning.
    /// </summary>
    [Button]
    public void ResetStage()
    {
        ClearEnemies();
        EnemyPoolCreated();
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
        foreach (var enemy in spawner.enemies)
            if (enemy != null)
            {
                PoolManager.Instance.Despawn(enemy);
                PoolManager.Instance.ClearPool();
                EnemyPoolCreated();
            }
        
        spawner.enemies.Clear();
    }

    #endregion
}