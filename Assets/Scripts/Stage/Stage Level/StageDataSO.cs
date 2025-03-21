using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class EnemyProperties
{
    public EnemyDataSO EnemyData;
    public float SpawnChance;
}

[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData", order = 2)]
public class StageDataSO : ScriptableObject
{
    #region Inspector & Variable
    [Title("Enemy Data")] [Tooltip("Data of the enemies scriptable object (Random by chance)")]
    [SerializeField] private EnemyProperties[] enemies;
    [Space][Title("Spawn Type")] [Tooltip("Data of the spawn type scriptable object (Random)")]
    [SerializeField] private SpawnTypeSO[] spawnTypes;
    [Space][Title("Stage Properties")]
    [Tooltip("Quota of the score to end the game")]
    [SerializeField] private float scoreQuota;
    
    [Space][Title("Max Enemy Spawn")]
    [Tooltip("Start max enemy")]
    [SerializeField] private int startMaxEnemySpawn;
    [Tooltip("Max enemy spawn cap")]
    [SerializeField] private int maxEnemySpawnCap;
    [Tooltip("Max enemy will increase by this quota")]
    [SerializeField] private float unitScoreQuota;
    
    [Space][Title("Enemy spawn Timer")]
    [Tooltip("Start enemy spawn interval")]
    [SerializeField] private float startSpawnInterval;
    [Tooltip("Enemy spawn interval Cap")]
    [SerializeField] private float spawnIntervalCap = 0.5f;
    [Tooltip("Enemy spawn interval will increase by this quota")]
    [SerializeField] private float decreaseSpawnInterval;
    
    [Space]
    [SerializeField] private Sprite background;
    #endregion

    #region Properties
    
    public EnemyProperties[] Enemies => enemies;
    public ISpawnType[] SpawnTypes => spawnTypes;
    public float ScoreQuota => scoreQuota;
    public int MaxEnemySpawnCap => maxEnemySpawnCap;
    public int CurrentMaxEnemySpawn => startMaxEnemySpawn;
    public float EnemySpawnInterval => startSpawnInterval;
    public float SpawnIntervalCap => spawnIntervalCap;
    public Sprite Background => background;
    public float UnitScoreQuota => unitScoreQuota;
    public float DecreaseSpawnInterval => decreaseSpawnInterval;
    
    #endregion
}