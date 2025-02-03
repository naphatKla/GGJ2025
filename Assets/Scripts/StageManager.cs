using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

#region Class
[Serializable]
public struct EnemyStruct
{
    public GameObject enemyPrefab;
    public int spawnChance;
}

public class StageClass
{
    public EnemyStruct[] enemyPrefab;
    public MonoScript[] challenge;
    public MonoScript[] unlock;
    public float scoreQuota;
    public int maxEnemySpawnCap;
    public float enemySpawnInterval;
    public float unitScoreQuota;
    public int decreaseSpawnInterval;
    public float spawnIntervalCap = 0.5f;
}
#endregion

public class StageManager : SerializedMonoBehaviour
{
    #region Inspector
    [DictionaryDrawerSettings(KeyLabel = "Stage", ValueLabel = "Stage Properties")]
    public Dictionary<int, StageClass> stageLabels = new Dictionary<int, StageClass>();
    
    [BoxGroup("Current Data Stage")]
    [SerializeField] public int currentStage = 1;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private float currentSpawnInterval = 1;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private float currentScoreQuota;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private int currentMaxEnemySpawn;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private int currentEnemySpawn;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private float nextunitScoreQuota;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private float intervalunitScoreQuota;
    [BoxGroup("Current Data Stage")]
    [SerializeField] private float minDistanceFromPlayer = 20f;
    [BoxGroup("Size")] public Vector2 regionSize = Vector2.zero;
    [SerializeField] public Transform enemyParent;

    [ShowInInspector] private bool isStarting = true;
    private bool nextQuotaReached = false;
    private bool intervalQuotaReached = false;
    
    [Header("Stage Events")] [BoxGroup("Events")]
    public StageEvent stageEvent;
    
    public class StageEvent
    {
        public UnityEvent onStageStart;
        public UnityEvent onStageEnemySpawn;
        public UnityEvent onStageReached;
        public UnityEvent onEnemyQuotaUnitReached;
        public UnityEvent onEnemyQuotaIntervalReached;
    }

    #endregion
    
    
    #region Properties
    private float _score => Player.Instance.Score;
    private Player player => Player.Instance;
    #endregion

    #region Method
    private void Start()
    {
        if (!isStarting) return;
        SetStage();
        StartCoroutine(EnemyQuotaCoroutine());
        StartCoroutine(CheckSpawnEnemies());
    }

    private void Update()
    {
        if (!isStarting) return;
        CheckScoreStageQuota();
    }

    private IEnumerator CheckSpawnEnemies()
    {
        while (true)
        {
            int currentEnemyCount = GetCurrentEnemyCount();
            if (currentEnemyCount < currentMaxEnemySpawn)
            {
                for (int i = 0; i < currentEnemySpawn; i++)
                {
                    if (currentEnemyCount < currentMaxEnemySpawn)
                    {
                        stageEvent.onStageEnemySpawn?.Invoke();
                        SpawnEnemy();
                        currentEnemyCount++;
                    }
                }
            }
            yield return new WaitForSeconds(currentSpawnInterval);
        }
    }
    
    private void SetStage()
    {
        stageEvent.onStageStart?.Invoke();
        currentMaxEnemySpawn = stageLabels[currentStage].maxEnemySpawnCap;
        currentSpawnInterval = stageLabels[currentStage].enemySpawnInterval;
        currentScoreQuota = stageLabels[currentStage].scoreQuota;
        intervalunitScoreQuota = stageLabels[currentStage].decreaseSpawnInterval;
        nextunitScoreQuota = stageLabels[currentStage].unitScoreQuota;
    }
    
    private void CheckScoreStageQuota()
    {
        if (_score >= currentScoreQuota)
        {
            Debug.Log("Score Quota Reached");
            stageEvent.onStageReached?.Invoke();
        }
    }
    
    private IEnumerator EnemyQuotaCoroutine()
    {
        while (true)
        {
            if (_score >= nextunitScoreQuota)
            {
                stageEvent.onEnemyQuotaUnitReached?.Invoke();
                currentEnemySpawn = Mathf.Clamp(currentEnemySpawn + 1, 1, stageLabels[currentStage].maxEnemySpawnCap);
                nextunitScoreQuota += stageLabels[currentStage].unitScoreQuota;
            }
            if (_score >= intervalunitScoreQuota)
            {
                stageEvent.onEnemyQuotaIntervalReached?.Invoke();
                float newInterval = currentSpawnInterval - 0.1f;
                currentSpawnInterval = Mathf.Clamp(newInterval, stageLabels[currentStage].spawnIntervalCap, currentSpawnInterval);
                intervalunitScoreQuota += stageLabels[currentStage].decreaseSpawnInterval;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }


    private void SpawnEnemy()
    {
        float totalSpawnChance = 0f;
        foreach (var enemyData in stageLabels[currentStage].enemyPrefab)
        {
            totalSpawnChance += enemyData.spawnChance;
        }

        float randomValue = Random.Range(0, totalSpawnChance);
        float cumulativeChance = 0f;

        foreach (var enemyData in stageLabels[currentStage].enemyPrefab)
        {
            cumulativeChance += enemyData.spawnChance;
            if (randomValue <= cumulativeChance)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();
                Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, enemyParent);
                break;
            }
        }
    }
    
    private Vector2 GetRandomSpawnPosition()
    {
        if (player == null) { return Vector2.zero; }
        Vector2 spawnPosition;
        do
        {
            spawnPosition = new Vector2(
                Random.Range(-regionSize.x / 2, regionSize.x / 2),
                Random.Range(-regionSize.y / 2, regionSize.y / 2)
            );
        } while (Vector2.Distance(spawnPosition, player.transform.position) < minDistanceFromPlayer);

        return spawnPosition;
    }
    
    private int GetCurrentEnemyCount()
    {
        int count = 0;
        foreach (Transform child in enemyParent)
        {
            if (child.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, regionSize);
    }
    
    #endregion
}
