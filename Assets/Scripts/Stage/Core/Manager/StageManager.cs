using System;
using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

public class StageManager : MonoBehaviour, IEnemySpawnerView
{
    #region Inspector & Variable
    [Title("Map Data")] [Tooltip("You can add more than 1 stage and change by Function Set Stage or Next Stage.")]
    [SerializeField] private List<MapDataSO> mapDataList = new();
    [Space]
    [Tooltip("The index of the current map in the mapData list.")]
    [SerializeField] private int currentMapIndex;
    [Tooltip("The current background image.")]
    [SerializeField] private Sprite currentBackground;
    
    [Title("Map Current Setting")] [Tooltip("The index of the current stage in the stageData list.")]
    [SerializeField] private int currentStageIndexInMap;
    [Tooltip("Parent of the enemy to spawn prefab.")]
    [SerializeField] private Transform enemyParent;
    
    [Title("Map Area Setting")] 
    [Tooltip("Region of the spawn area.")]
    [SerializeField] private Vector2 regionSize = Vector2.zero;
    [Tooltip("Minimum spawn area from the player.")]
    [SerializeField] private float minDistanceFromPlayer = 20f;
    
    private EnemySpawner _enemySpawner;
    private Timer globalTimer;
    
    private MapDataSO CurrentMap => 
        mapDataList.Count > 0 && currentMapIndex < mapDataList.Count
            ? mapDataList[currentMapIndex]
            : null;

    private StageDataSO CurrentStage => 
        CurrentMap != null && currentStageIndexInMap < CurrentMap.stages.Count
            ? CurrentMap.stages[currentStageIndexInMap]
            : null;
    #endregion

    #region Unity Methods
    
    private void Awake()
    {
        // Sets up enemy parent and spawner with the current stage.
        if (globalTimer == null) globalTimer = GetComponent<Timer>();
        if (enemyParent == null) enemyParent = new GameObject("EnemyParent").transform;
        if (CurrentMap.stages.Count == 0 || CurrentStage == null) return;
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
    /// Check stop or pause state
    /// </summary>
    /// <returns></returns>
    public bool IsSpawningStoppedOrPaused()
    {
        if (_enemySpawner == null) return true;
        // ตรวจสอบสถานะของ EnemySpawner
        return _enemySpawner.IsStoppedOrPaused();
    }
    
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

    public Vector2 GetRegionSize()
    {
        return regionSize;
    }

    public float GetMinDistanceFromPlayer()
    {
        return minDistanceFromPlayer;
    }

    /// <summary>
    /// Sets the background image.
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (currentBackground != null)
            currentBackground = CurrentMap.background;
    }
    
    /// <summary>
    /// Setting up the enemy pool.
    /// </summary>
    private void EnemyPoolCreated()
    {
        EnemyPoolInitializer.Prewarm(CurrentStage, enemyParent);
    }
    
    /// <summary>
    /// Reload the stage
    /// </summary>
    private void ReloadStage()
    {
        ClearEnemies();
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        _enemySpawner.StartSpawning();
        SetBackground(CurrentMap?.background);
        Timer.Instance.ResetTimer();
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
    [FoldoutGroup("Enemy Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void StartSpawning()
    {
        _enemySpawner?.StartSpawning();
    }

    /// <summary>
    /// Stops enemy spawning.
    /// </summary>
    [FoldoutGroup("Enemy Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void StopSpawning()
    {
        _enemySpawner?.StopSpawning();
    }

    /// <summary>
    /// Pauses enemy spawning.
    /// </summary>
    [FoldoutGroup("Enemy Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void PauseSpawning()
    {
        _enemySpawner?.PauseSpawning();
    }
    
    /// <summary>
    /// Chanage the map index
    /// </summary>
    /// <param name="mapIndex"></param>
    [FoldoutGroup("Map Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void SetMap(int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= mapDataList.Count) return;

        currentMapIndex = mapIndex;
        currentStageIndexInMap = 0;
        ReloadStage();
    }

    /// <summary>
    /// Increase map index by 1
    /// </summary>
    [FoldoutGroup("Map Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void NextMap()
    {
        if (mapDataList.Count == 0 || currentMapIndex >= mapDataList.Count - 1) return;

        currentMapIndex++;
        currentStageIndexInMap = 0;
        ReloadStage();
    }

    /// <summary>
    /// Set current of stage index
    /// </summary>
    /// <param name="stageIndex"></param>
    [FoldoutGroup("Stage Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void SetStageInMap(int stageIndex)
    {
        if (CurrentMap == null || stageIndex < 0 || stageIndex >= CurrentMap.stages.Count) return;

        currentStageIndexInMap = stageIndex;
        ReloadStage();
    }

    /// <summary>
    /// Increase stage index by 1
    /// </summary>
    [FoldoutGroup("Stage Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void NextStageInMap()
    {
        if (CurrentMap == null || currentStageIndexInMap >= CurrentMap.stages.Count - 1) return;

        currentStageIndexInMap++;
        ReloadStage();
    }
    
    /// <summary>
    /// Deletes all enemies and clears both lists.
    /// </summary>
    [PropertySpace(SpaceBefore = 30)]
    [Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
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
    
    /// <summary>
    /// Triggers a spawn event immediately.
    /// </summary>
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
    public void TriggerSpawnEvent()
    {
        if (_enemySpawner == null) return;
        _enemySpawner.TriggerSpawnEvent(true, true); 
    }
    #endregion
}