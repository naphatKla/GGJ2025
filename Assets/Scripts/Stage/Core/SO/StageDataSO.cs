using System;
using System.Collections.Generic;
using System.Linq;
using Characters.SO.CharacterDataSO;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class EnemyProperties : IWeightedEnemy
{
    [HideInInspector] public EnemyDataSO EnemyData;

    [ValueDropdown("@GetEnemyTypesFromDefault()")]
    public CharacterDataSo EnemyType;

    [Tooltip("Chance to spawn this enemy")]
    public float SpawnChance;
    
    [FoldoutGroup("Advanced Spawn Control")]
    public bool UseTimer;

    [ShowIf("UseTimer")] [Tooltip("Time after stage start to begin spawning this enemy")]
    public float StartTimer;

    [ShowIf("UseTimer")] [Tooltip("Time after stage start to stop spawning this enemy")]
    public float EndTimer;

    [FoldoutGroup("Advanced Spawn Control")]
    public bool UseDynamicSpawnChance;

    [ShowIf("UseDynamicSpawnChance")] [Tooltip("Chance to spawn this enemy with animation curved (Default: 0 Second = 1% / 300 Second = 10%)")]
    public AnimationCurve SpawnChanceOverTime = AnimationCurve.Linear(0, 1, 300, 10);

    /// <summary>
    ///     Calculate SpawnChance
    /// </summary>
    public float GetCurrentSpawnChance(float currentTime)
    {
        return UseDynamicSpawnChance ? SpawnChanceOverTime.Evaluate(currentTime) : SpawnChance;
    }


    private IEnumerable<CharacterDataSo> GetEnemyTypesFromDefault()
    {
        if (EnemyData == null || EnemyData.enemyData == null)
            return Enumerable.Empty<CharacterDataSo>();

        return EnemyData.enemyData;
    }

    public CharacterDataSo GetCharacterData()
    {
        return EnemyType;
    }

    public float GetSpawnChance()
    {
        return SpawnChance;
    }

    public bool IsAvailableAtTime(float currentTime)
    {
        if (!UseTimer) return true;
        return currentTime >= StartTimer && currentTime <= EndTimer;
    }
}


[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData", order = 2)]
public class StageDataSO : ScriptableObject
{
    #region Inspector & Variable

    [SerializeField] private EnemyDataSO defaultEnemyData;

    [Tooltip("How many enemy should created on enemyparent to use with pooling")]
    public float PreObjectSpawn;

    [Title("Enemy Data")] [Tooltip("Data of the enemies scriptable object (Random by chance)")] [SerializeField]
    private EnemyProperties[] enemies;

    [Space] [Title("Spawn Event")] [Tooltip("Data of world event scriptable object")] [SerializeField]
    private SpawnEventSO[] spawnEvent;

    [Space]
    [Title("Stage Properties")]
    [Tooltip("Quota of the kill to end the game or go to next stage")]
    [SerializeField]
    private float killQuota;

    [Space] [Title("Stage Timer")] [Tooltip("Timer limit for this stage to end")] [SerializeField]
    private float timerLimit;

    [Space] [Title("Max Enemy Spawn")] [Tooltip("Enemy spawn max will increase by this quota")] [SerializeField]
    private float spawnQuota;

    [Tooltip("Start max enemy")] [SerializeField]
    private int startMaxEnemySpawn;

    [Tooltip("Max enemy spawn cap")] [SerializeField]
    private int maxEnemySpawnCap;

    [Tooltip("How many should enemy add to startMaxEnemyspawn (default 1)")] [SerializeField]
    private int addMaxEnemySpawn = 1;

    [Space] [Title("Enemy Spawn Timer")] [Tooltip("Enemy spawn interval will increase by this quota")] [SerializeField]
    private float intervalQuota;

    [Tooltip("Start enemy spawn interval")] [SerializeField]
    private float startSpawnInterval;

    [Tooltip("Enemy spawn interval Cap")] [SerializeField]
    private float spawnIntervalCap = 0.5f;

    [Tooltip("How much interval should decrease to startSpawnInterval (default 0.1f)")] [SerializeField]
    private float removeIntervalEnemySpawn = 0.1f;

    [Space] [Title("Spawn Event Timer")] [Tooltip("Interval to check for spawn event triggers")] [SerializeField]
    private float eventIntervalCheck = 1f;

    [Space] [SerializeField] private Sprite background;

    #endregion

    #region Properties

    public EnemyProperties[] Enemies => enemies;
    public ISpawnEvent[] SpawnEvents => spawnEvent;
    public float KillQuota => killQuota;
    public float TimerStage => timerLimit;
    public int MaxEnemySpawnCap => maxEnemySpawnCap;
    public int CurrentMaxEnemySpawn => startMaxEnemySpawn;
    public int AddMaxEnemySpawn => addMaxEnemySpawn;
    public float EnemySpawnInterval => startSpawnInterval;
    public float SpawnIntervalCap => spawnIntervalCap;
    public float RemoveIntervalEnemySpawn => removeIntervalEnemySpawn;
    public Sprite Background => background;
    public float SpawnKillQuota => spawnQuota;
    public float IntervalKillQuota => intervalQuota;
    public float EventIntervalCheck => eventIntervalCheck;

    #endregion

    private void OnValidate()
    {
        foreach (var enemy in enemies) enemy.EnemyData = defaultEnemyData;
    }
}