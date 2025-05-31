using System;
using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using TMPro;
using UnityEngine.SceneManagement;

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
    
    [Title("Change Stage Setting")] 
    [Tooltip("How much delay before next stage start")]
    [SerializeField] private float delayNextStage;
    
    [Space][Tooltip("Show debug stage")]
    [SerializeField] private bool enableDebug;

    [FoldoutGroup("Stage UI")] [SerializeField]
    private TMP_Text stageText;
    
    [FoldoutGroup("Stage UI")] [SerializeField]
    private TMP_Text killquotaText;
  
    private EnemySpawner _enemySpawner;
    private CountdownTimer _countdown;
    private Timer globalTimer;
    private bool hasTriggeredResult = false;
    
    private MapDataSO CurrentMap => 
        mapDataList.Count > 0 && currentMapIndex < mapDataList.Count
            ? mapDataList[currentMapIndex]
            : null;

    private StageDataSO CurrentStage => 
        CurrentMap != null && currentStageIndexInMap < CurrentMap.stages.Count
            ? CurrentMap.stages[currentStageIndexInMap]
            : null;
    #endregion

    #region Debug

    [FoldoutGroup("Debug Zone")]
    [ShowInInspector] [ReadOnly] [ShowIf("enableDebug")]
    public float DebugSpawnMax { get; set; }
    [FoldoutGroup("Debug Zone")]
    [ShowInInspector] [ReadOnly] [ShowIf("enableDebug")]
    public float DebugNextSpawnMax { get; set; }
    [FoldoutGroup("Debug Zone")]
    [ShowInInspector] [ReadOnly] [ShowIf("enableDebug")]
    public float DebugSpawnInterval { get; set; }
    [FoldoutGroup("Debug Zone")]
    [ShowInInspector] [ReadOnly] [ShowIf("enableDebug")]
    public float DebugNextSpawnInterval { get; set; }
    [FoldoutGroup("Debug Zone")]
    [ShowInInspector] [ReadOnly] [ShowIf("enableDebug")]
    public float DebugKillCount { get; set; }

    #endregion
    
    #region Unity Methods
    
    private void Awake()
    {
        // Sets up enemy parent and spawner with the current stage.
        if (globalTimer == null) globalTimer = GetComponent<Timer>();
        if (_countdown == null) _countdown = GetComponent<CountdownTimer>();
        if (enemyParent == null) enemyParent = new GameObject("EnemyParent").transform;
        if (GameController.Instance != null && GameController.Instance.selectedMapData != null)
        {
            var selectedMap = GameController.Instance.selectedMapData;
            if (!mapDataList.Contains(selectedMap)) mapDataList.Add(selectedMap);

            currentMapIndex = mapDataList.IndexOf(selectedMap);
            currentStageIndexInMap = GameController.Instance.nextStageIndex;
        }
        if (CurrentMap.stages.Count == 0 || CurrentStage == null) return;
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
    }

    private void DebugZone()
    {
        if (_enemySpawner == null) return;
        DebugSpawnMax = _enemySpawner.CurrentMaxEnemySpawn;
        DebugNextSpawnMax = _enemySpawner.DebugNextKillStartMax;
        DebugSpawnInterval = _enemySpawner.CurrentSpawnInterval;
        DebugNextSpawnInterval = _enemySpawner.DebugNextIntervalStartMax;
        DebugKillCount = GetPlayerKill();

    }
    
    private void Start()
    {
        SetBackground(currentBackground);
        StartPlay();
        UpdateStageText();
    }
    private async void Update()
    {
        _enemySpawner?.Update();
        UpdateKillQuotaText();
        if (Application.isPlaying && enableDebug) DebugZone();
        if (!hasTriggeredResult && CanGotoNextStage())
        {
            hasTriggeredResult = true;
            
            // Last Stage in the map list
            if (IsLastStageInMap())
            {
                StopSpawning();
                UIManager.Instance.OpenPanel(UIPanelType.ResultLastStage);
                await UniTask.Delay(300);
                ResetStage();
            }
            else // Still have next stage
            {
                StopSpawning();
                UIManager.Instance.OpenPanel(UIPanelType.ResultWithNextStage);
                Timer.Instance.PauseTimer();
                ResetStage();
            }
        }

    }

    #endregion

    #region Methods
    /// <summary>
    /// Check player quota to go next stage
    /// </summary>
    /// <returns></returns>
    public bool CanGotoNextStage()
    {
        return GetPlayerKill() >= CurrentMap.stages[currentStageIndexInMap].KillQuota;
    }
    
    /// <summary>
    /// Check that there is no next stage
    /// </summary>
    /// <returns></returns>
    private bool IsLastStageInMap()
    {
        return CurrentMap != null && currentStageIndexInMap >= CurrentMap.stages.Count - 1;
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
        return _enemySpawner.enemies.Count(e => e != null && e.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// Gets the player’s current position.
    /// </summary>
    public Vector2 GetPlayerPosition()
    {
        return PlayerCharacter.Instance != null ? PlayerCharacter.Instance.transform.position : Vector2.zero;
    }
    
    /// <summary>
    /// Gets the player’s current kill count.
    /// </summary>
    public float GetPlayerKill()
    {
        return _enemySpawner.CurrentKillCount;
    }

    public Vector2 GetRegionSize()
    {
        return regionSize;
    }

    public float GetMinDistanceFromPlayer()
    {
        return minDistanceFromPlayer;
    }

    public void SpawnEventEnemies(SpawnEventSO.SpawnEventData spawnData)
    {
        _enemySpawner.SpawnEventEnemies(spawnData);
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
    /// Update stage index
    /// </summary>
    public void UpdateGameController()
    {
        if (GameController.Instance == null) return;
        GameController.Instance.nextStageIndex = currentStageIndexInMap;
        GameController.Instance.selectedMapIndex = currentMapIndex;
    }
    
    /// <summary>
    /// Update stage index
    /// </summary>
    public void UpdateStageText()
    {
        if (stageText == null) return;
        stageText.text = "Stage: " + (currentStageIndexInMap + 1) + " / " + CurrentMap.stages.Count;
    }
    
    /// <summary>
    /// Update kill quota
    /// </summary>
    public void UpdateKillQuotaText()
    {
        if (killquotaText == null) return;
        killquotaText.text = "Kill: " + _enemySpawner.CurrentKillCount+ "/" + CurrentMap.stages[currentStageIndexInMap].KillQuota;
    }
    
    /// <summary>
    /// Setting up the enemy pool.
    /// </summary>
    private void EnemyPoolCreated()
    {
        _enemySpawner.Prewarm(CurrentStage, enemyParent);
    }
    
    /// <summary>
    /// Reload the stage
    /// </summary>
    private void ResetStage()
    {
        ResetQuota();
        hasTriggeredResult = false;
        Timer.Instance.SetTimer(CurrentMap.stages[currentStageIndexInMap].TimerStage);
        ClearEnemies();
        _enemySpawner = new EnemySpawner(this, CurrentStage, regionSize, minDistanceFromPlayer);
        SetBackground(CurrentMap?.background);
        UpdateStageText();
    }
    
    /// <summary>
    /// Reload the stage
    /// </summary>
    private void ResetQuota()
    {
        _enemySpawner.ResetQuota();
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
        UpdateGameController();
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
        SetStageInMap(currentStageIndexInMap);
        UpdateGameController();
        ResetStage();
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
        UpdateGameController();
        ResetStage();
    }
    
    /// <summary>
    /// Set current of stage index with delay
    /// </summary>
    /// <param name="stageIndex"></param>
    public async void SetStageInMapWithDelay(int stageIndex)
    {
        if (CurrentMap == null || stageIndex < 0 || stageIndex >= CurrentMap.stages.Count) return;
        currentStageIndexInMap = stageIndex;
        ResetStage();
        Timer.Instance.StopDelayTimer(delayNextStage);
        await UniTask.Delay(TimeSpan.FromSeconds(delayNextStage));
        StartSpawning();
    }

    /// <summary>
    /// Increase stage index by 1
    /// </summary>
    [FoldoutGroup("Stage Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void NextStageInMap()
    {
        if (CurrentMap == null || currentStageIndexInMap >= CurrentMap.stages.Count - 1) return;
        currentStageIndexInMap++;
        UpdateGameController();
        ResetStage();
    }
    
    /// <summary>
    /// Check player quota to go next stage
    /// </summary>
    /// <returns></returns>
    public async void NextStageWithDelay()
    {
        if (CurrentMap == null || currentStageIndexInMap >= CurrentMap.stages.Count - 1) return;
        currentStageIndexInMap++;
        UpdateGameController();
        ResetStage();
        Timer.Instance.StopDelayTimer(delayNextStage);
        await UniTask.Delay(TimeSpan.FromSeconds(delayNextStage));
        StartSpawning();
    }
    
    /// <summary>
    /// Start Stage
    /// </summary>
    [PropertySpace(SpaceBefore = 30)]
    [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public async void StartPlay()
    {
        UIManager.Instance.CloseAllPanels();
        await _countdown.StartCountdownAsync(delayNextStage);
        SetStageInMap(currentStageIndexInMap);
        StartSpawning();
    }
    
    /// <summary>
    /// Deletes all enemies and clears both lists.
    /// </summary>
    [Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void ClearEnemies()
    {
        foreach (var enemy in _enemySpawner.enemies)
            if (enemy != null)
                PoolManager.Instance.Despawn(enemy.gameObject);
        
        _enemySpawner.enemies.Clear();
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