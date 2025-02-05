using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Characters;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

[System.Serializable]
public class EnemyData2
{
    public EnemyCharacter enemyPrefab;
    public bool setSpawnSize;
    [ShowIf("setSpawnSize")]
    public float minSizeonSpawn;
    [ShowIf("setSpawnSize")]
    public float maxSizeonSpawn;
}

public class RandomEnemyRespawn : MMSingleton<RandomEnemyRespawn>
{
    [SerializeField] private List<EnemyData2> enemyDataList = new List<EnemyData2>();
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

    [ShowInInspector] private List<EnemyCharacter> enemyList = new List<EnemyCharacter>();
    [ShowInInspector] public int enemyCount = 0;

    private void Start()
    {
        SpawnToMaxOnStart();
        StartCoroutine(RandomEnemySpawn());
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        enemyCount = enemyParent.childCount;
        UpdateEnemyList();
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
                    EnemyCharacter enemyCharacter = obj.GetComponent<EnemyCharacter>();
                    if (enemyCharacter != null)
                    {
                        enemyList.Add(enemyCharacter);
                    }

                    if (enemy.setSpawnSize)
                    {
                        //SET SIZE
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
                        if (playerTransform != null)
                        {
                            while (Vector3.Distance(spawnPosition, playerTransform.position) < minDistanceFromPlayer)
                            {
                                spawnPosition = GetRegionPosition();
                            }
                        }
                        else
                        {
                            while (Vector3.Distance(spawnPosition, Vector3.zero) < minDistanceFromPlayer)
                            {
                                spawnPosition = GetRegionPosition();
                            }
                        }
                        GameObject obj = Instantiate(enemy.enemyPrefab.gameObject, spawnPosition, Quaternion.identity);
                        obj.transform.SetParent(enemyParent);
                        EnemyCharacter enemyCharacter = obj.GetComponent<EnemyCharacter>();
                        if (enemyCharacter != null)
                        {
                            enemyList.Add(enemyCharacter);
                        }

                        if (enemy.setSpawnSize)
                        {
                            //SET SIZE
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

    private void UpdateEnemyList()
    {
        for (int i = enemyList.Count - 1; i >= 0; i--)
        {
            if (enemyList[i] == null || enemyList[i].gameObject == null)
            {
                enemyList.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(regionSize.x, regionSize.y, 0));
    }
}