using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData", order = 2)]
public class StageDataSO : ScriptableObject
{
    [SerializeField] private EnemyDataSO[] enemies;
    [SerializeField] private SpawnTypeSO[] spawnTypes;
    [SerializeField] private float scoreQuota;
    [SerializeField] private int maxEnemySpawnCap;
    [SerializeField] private int currentMaxEnemySpawn;
    [SerializeField] private float enemySpawnInterval;
    [SerializeField] private float spawnIntervalCap = 0.5f;
    [SerializeField] private Sprite background;
    [SerializeField] private float unitScoreQuota;
    [SerializeField] private float decreaseSpawnInterval;

    public IEnemyData[] Enemies => enemies;
    public ISpawnType[] SpawnTypes => spawnTypes;
    public float ScoreQuota => scoreQuota;
    public int MaxEnemySpawnCap => maxEnemySpawnCap;
    public int CurrentMaxEnemySpawn => currentMaxEnemySpawn;
    public float EnemySpawnInterval => enemySpawnInterval;
    public float SpawnIntervalCap => spawnIntervalCap;
    public Sprite Background => background;
    public float UnitScoreQuota => unitScoreQuota;
    public float DecreaseSpawnInterval => decreaseSpawnInterval;
}