using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Characters;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

[System.Serializable]
public class EnemyData
{
    public EnemyManager enemyPrefab;
    public float spawnChance;
}

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] public List<EnemyData> enemyDataList = new List<EnemyData>();
    [SerializeField] public Transform enemyParent;
    [Tooltip("Clamp Maximum number of enemies that can be spawned")]
    [BoxGroup("Global")] [SerializeField] private int globalmaximumEnemySpawn;
    [Tooltip("Current level")]
    [BoxGroup("Level")] [SerializeField] private int currentLevel;
    [Tooltip("Increase level every levelIncreaseTimer seconds")]
    [BoxGroup("Level")] [SerializeField] private float levelIncreaseTimer;
    [Tooltip("Maximum number of enemies that can be spawned at current level")]
    [BoxGroup("Enemy")] [SerializeField] private int currentmaxEnemySpawn;
    [Tooltip("Number of enemies to spawn per level")]
    [BoxGroup("Enemy")] [SerializeField] private int spawnEnemyPerLevel;
    [Tooltip("Increase maximum number of enemies that can be spawned at current level (if currentmaxEnemySpawn is 1 next level will be 1+increasemaxEnemyPerLevel)")]
    [BoxGroup("Level")] [SerializeField] private int increasemaxEnemyPerLevel;
    [Tooltip("Spawn enemy every enemySpawnTimer seconds")]
    [BoxGroup("Level")] [SerializeField] private float enemySpawnTimer;
    [Tooltip("The size of spawn region")]
    [BoxGroup("Size")] public Vector2 regionSize = Vector2.zero;
    [SerializeField] private float minDistanceFromPlayer = 20f;

    private Player player => Player.Instance;

    private void Start()
    {
        StartCoroutine(SpawnEnemies());
        StartCoroutine(IncreaseLevelOverTime());
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            int currentEnemyCount = GetCurrentEnemyCount();
            if (currentEnemyCount < currentmaxEnemySpawn)
            {
                // Spawn multiple enemies based on spawnEnemyPerLevel
                for (int i = 0; i < spawnEnemyPerLevel; i++)
                {
                    if (currentEnemyCount < currentmaxEnemySpawn)
                    {
                        SpawnEnemy();
                        currentEnemyCount++;
                    }
                }
            }
            yield return new WaitForSeconds(enemySpawnTimer);
        }
    }

    private IEnumerator IncreaseLevelOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(levelIncreaseTimer);
            IncreaseLevel();
        }
    }

    private void IncreaseLevel()
    {
        currentLevel++;
        currentmaxEnemySpawn = Mathf.Min(currentmaxEnemySpawn + increasemaxEnemyPerLevel, globalmaximumEnemySpawn);
    }

    private void SpawnEnemy()
    {
        float totalSpawnChance = 0f;
        foreach (var enemyData in enemyDataList)
        {
            totalSpawnChance += enemyData.spawnChance;
        }

        float randomValue = Random.Range(0, totalSpawnChance);
        float cumulativeChance = 0f;

        foreach (var enemyData in enemyDataList)
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
        // Loop through the enemyParent's children to count active enemies
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
}