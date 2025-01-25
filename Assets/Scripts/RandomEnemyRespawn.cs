using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class EnemyData
{
    public EnemyManager enemyPrefab;
    public bool setSpawnSize;
    [ShowIf("setSpawnSize")]
    public float minSizeonSpawn;
    [ShowIf("setSpawnSize")]
    public float maxSizeonSpawn;
}

public class RandomEnemyRespawn : MMSingleton<RandomSpawnExp>
{
    [SerializeField] private List<EnemyData> enemyDataList = new List<EnemyData>();
    [SerializeField] public Transform enemyParent;
    [SerializeField] private int maxSpawn;
    [SerializeField] private int enemyPerSpawn;
    [SerializeField] private float enemySpawnTimer;
    [Tooltip("The size of spawn region")]
    [BoxGroup("Size")] public Vector2 regionSize = Vector2.zero;
    [Tooltip("Player transform to avoid spawning near the player")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Minimum distance from the player to spawn enemies")]
    [SerializeField] private float minDistanceFromPlayer = 5f;
    
    [ShowInInspector]
    private List<EnemyManager> enemyList = new List<EnemyManager>();
    
    private void Start()
    {
        SpawnToMaxOnStart();
        StartCoroutine(RandomEnemySpawn());
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    private void SpawnToMaxOnStart()
    {
        while (enemyParent.childCount < maxSpawn)
        {
            foreach (var enemy in enemyDataList)
            {
                for (int i = 0; i < enemyPerSpawn; i++)
                {
                    if (enemyParent.childCount >= maxSpawn) break;

                    Vector3 spawnPosition = GetRegionPosition();
                    while (Vector3.Distance(spawnPosition, playerTransform.position) < minDistanceFromPlayer)
                    {
                        spawnPosition = GetRegionPosition();
                    }

                    GameObject obj = Instantiate(enemy.enemyPrefab.gameObject, spawnPosition, Quaternion.identity);
                    obj.transform.SetParent(enemyParent);
                    enemyList.Add(obj.GetComponent<EnemyManager>());
                    if (enemy.setSpawnSize)
                    {
                        float randomSize = Random.Range(enemy.minSizeonSpawn, enemy.maxSizeonSpawn);
                        obj.transform.localScale = new Vector3(randomSize, randomSize, 1);
                    }
                }
            }
        }
    }
    
    private IEnumerator RandomEnemySpawn()
    {
        while (true)
        {
            foreach (var enemy in enemyDataList)
            {
                float elapsedTime = 0f;

                while (elapsedTime < enemySpawnTimer)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (enemyParent.childCount < maxSpawn)
                {
                    for (int i = 0; i < enemyPerSpawn; i++)
                    {
                        Vector3 spawnPosition = GetRegionPosition();
                        while (Vector3.Distance(spawnPosition, playerTransform.position) < minDistanceFromPlayer)
                        {
                            spawnPosition = GetRegionPosition();
                        }

                        GameObject obj = Instantiate(enemy.enemyPrefab.gameObject, spawnPosition, Quaternion.identity);
                        obj.transform.SetParent(enemyParent);
                        enemyList.Add(obj.GetComponent<EnemyManager>());
                        if (enemy.setSpawnSize)
                        {
                            //Random Set Size
                        }
                    }
                }
            }
        }
    }
    
    private Vector3 GetRegionPosition()
    {
        return new Vector3(
            Random.Range(-regionSize.x / 2, regionSize.x / 2),
            Random.Range(-regionSize.y / 2, regionSize.y / 2),
            0
        );
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(regionSize.x, regionSize.y, 0));
    }
}
