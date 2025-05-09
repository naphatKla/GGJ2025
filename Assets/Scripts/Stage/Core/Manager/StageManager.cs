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
    
    private EnemySpawner _enemySpawner;
    
    private StageDataSO CurrentStage => stageData.Count > 0 && currentStageIndex < stageData.Count
        ? stageData[currentStageIndex]
        : null;
    
    #endregion

    #region Unity Methods
    
    private void Awake()
    {
        // Sets up enemy parent and spawner with the current stage.
        if (enemyParent == null) enemyParent = new GameObject("EnemyParent").transform;
        if (stageData.Count == 0 || CurrentStage == null) return;
        EnemyPoolCreated();
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
    }
    
    private void Start()
    {
        _enemySpawner.StartSpawning();
        SetBackground(currentBackground);
    }
    private void Update()
    {
        _enemySpawner?.Update();
    }

    #endregion

    #region Methods
    /// <summary>
    /// Creates an enemy and adds it to the appropriate list.
    /// </summary>
    public void SpawnEnemy(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent, bool isWorldEventEnemy = false)
    {
        var enemy = PoolManager.Instance.Spawn(prefab, position, rotation);
        if (isWorldEventEnemy)
            _enemySpawner.eventEnemies.Add(enemy);
        else
            _enemySpawner.enemies.Add(enemy);
    }
    
    /// <summary>
    /// Adds a WorldEvent enemy to the eventEnemies list.
    /// </summary>
    public void AddWorldEventEnemy(GameObject enemy)
    {
        if (!_enemySpawner.eventEnemies.Contains(enemy))
            _enemySpawner.eventEnemies.Add(enemy);
    }

    /// <summary>
    /// Remove from enemy or event enemy list.
    /// </summary>
    public void RemoveListEnemy(GameObject enemy)
    {
        if (_enemySpawner.enemies.Contains(enemy))
            _enemySpawner.enemies.Remove(enemy);
        else if (_enemySpawner.eventEnemies.Contains(enemy))
            _enemySpawner.eventEnemies.Remove(enemy);
    }

    /// <summary>
    /// Returns the parent object for enemies.
    /// </summary>
    public Transform GetEnemyParent()
    {
        return enemyParent;
    }

    /// <summary>
    /// Counts active normal enemies in the scene (excludes WorldEvent enemies).
    /// </summary>
    public int GetCurrentEnemyCount()
    {
        return _enemySpawner.enemies.Count(e => e != null && e.activeInHierarchy);
    }

    /// <summary>
    /// Gets the player’s current position.
    /// </summary>
    public Vector2 GetPlayerPosition()
    {
        return PlayerCharacter.Instance != null ? PlayerCharacter.Instance.transform.position : Vector2.zero;
    }

    /// <summary>
    /// Gets the player’s current score.
    /// </summary>
    public float GetPlayerScore()
    {
        return PlayerCharacter.Instance != null ? PlayerCharacter.Instance.Score : 0f;
    }

    /// <summary>
    /// Sets the background image.
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (currentBackground != null)
            currentBackground = stageData[currentStageIndex].Background;
    }
    
    /// <summary>
    /// Setting up the enemy pool.
    /// </summary>
    private void EnemyPoolCreated()
    {
        EnemyPoolInitializer.Prewarm(CurrentStage, enemyParent);
    }

    #endregion

    #region Gizmos

    /// <summary>
    /// Draws a red box in the editor for the spawn area.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, regionSize);
    }

    #endregion

    #region Control Methods

    /// <summary>
    /// Starts enemy spawning.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void StartSpawning()
    {
        _enemySpawner?.StartSpawning();
    }

    /// <summary>
    /// Stops enemy spawning.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void StopSpawning()
    {
        _enemySpawner?.StopSpawning();
    }

    /// <summary>
    /// Pauses enemy spawning.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void PauseSpawning()
    {
        _enemySpawner?.PauseSpawning();
    }

    /// <summary>
    /// Switches to a specific stage and starts spawning.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void SetStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stageData.Count) return;
        currentStageIndex = stageIndex;
        ClearEnemies();
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        _enemySpawner.StartSpawning();
        SetBackground(CurrentStage.Background);
    }

    /// <summary>
    /// Moves to the next stage and starts spawning.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
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
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        _enemySpawner.StartSpawning();
        SetBackground(CurrentStage.Background);
    }
    
    /// <summary>
    /// Reset the stage and starts spawning.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void ResetStage()
    {
        ClearEnemies();
        EnemyPoolCreated();
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        _enemySpawner.StartSpawning();
        SetBackground(CurrentStage.Background);
    }
    
    /// <summary>
    /// Triggers a world event immediately.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void TriggerWorldEvent()
    {
        if (_enemySpawner == null) return;
        _enemySpawner.TriggerWorldEvent(true); 
    }

    /// <summary>
    /// Deletes all enemies and clears both lists.
    /// </summary>
    [FoldoutGroup("Stage Control"), Button]
    public void ClearEnemies()
    {
        foreach (var enemy in _enemySpawner.enemies)
            if (enemy != null)
                PoolManager.Instance.Despawn(enemy);
        
        foreach (var enemy in _enemySpawner.eventEnemies)
            if (enemy != null)
                PoolManager.Instance.Despawn(enemy);
        
        _enemySpawner.enemies.Clear();
        _enemySpawner.eventEnemies.Clear();
        PoolManager.Instance.ClearPool(enemyParent);
        EnemyPoolCreated();
    }

    #endregion
}