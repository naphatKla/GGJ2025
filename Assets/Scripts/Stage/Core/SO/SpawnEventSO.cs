using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;

public class SpawnEventSO : ScriptableObject, ISpawnEvent
{
    [Title("Event Properties")]
    [Tooltip("Chance of this speed event being selected (0–100%)")]
    [Range(0, 100)] [SerializeField] private float _chance = 1f;
    
    [Tooltip("Cooldown time (in seconds) before this event can be triggered again")]
    [SerializeField] private float _cooldown = 5f;
    
    [Tooltip("Enemy count when spawning event")]
    [SerializeField] private int _enemyCount = 16;
    
    [Tooltip("Toggle to spawn delay per enemy when spawning event")]
    [SerializeField] private float _spawnDelayAll = 0.2f;
    
    [Tooltip("Minimum Delay between spawning per enemy when spawning event")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")] [SerializeField] 
    private bool spawnDelayPerEnemy;
    
    [Tooltip("Minimum Delay between spawning per enemy when spawning event")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")]
    [SerializeField] private float minDelay = 0.05f;
    
    [Tooltip("Maximum Delay between spawning per enemy when spawning event")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")]
    [SerializeField] private float maxDelay = 0.5f;
    
    [Tooltip("Delay Curve between spawning per enemy when spawning event")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")]
    [SerializeField] private AnimationCurve perEnemyDelayCurve;
    
    [SerializeField] private List<EnemyDataSO> _spawnEnemies;
    [SerializeField] private GameObject _spawnEffectPrefab;
    [SerializeReference] private ISpawnPositionStrategy _spawnStrategy = new ConfigurableSpawnStrategy();
    [SerializeReference] private List<IEventCondition> _customConditions;
    [SerializeField] public List<float> timerTrigger;
    [SerializeField] public List<float> killTrigger;

    private float _lastSpawnTime = -Mathf.Infinity;

    public float Chance => _chance;
    public float Cooldown => _cooldown;
    public int EnemyCount => _enemyCount;
    public List<IEnemyData> EventEnemies => _spawnEnemies?.ConvertAll(e => (IEnemyData)e);

    public bool IsCooldownActive(float currentTime) => currentTime < _lastSpawnTime + _cooldown;

    public bool CanTrigger() => _customConditions == null || _customConditions.All(c => c.CanTrigger());

    public void MarkTriggered() => _lastSpawnTime = Time.time;

    public SpawnEventData CreateSpawnData(IEnemySpawnerView spawnerView)
    {
        var spawnPositions = new List<Vector2>();
        (_spawnStrategy ?? new ConfigurableSpawnStrategy()).CalculatePositions(
            spawnerView.GetPlayerPosition(),
            spawnerView.GetRegionSize(),
            spawnerView.GetMinDistanceFromPlayer(),
            _enemyCount,
            spawnPositions
        );

        return new SpawnEventData
        {
            Positions = spawnPositions,
            EnemyCount = _enemyCount,
            Enemies = EventEnemies,
            SpawnDelayAll = _spawnDelayAll,
            SpawnDelayPerEnemy = spawnDelayPerEnemy,
            MinDelay = minDelay,
            MaxDelay = maxDelay,
            DelayCurve = perEnemyDelayCurve,
            SpawnEffectPrefab = _spawnEffectPrefab
        };
    }

    [Serializable]
    public class SpawnEventData
    {
        public List<Vector2> Positions;
        public int EnemyCount;
        public List<IEnemyData> Enemies;
        public float SpawnDelayAll;
        public bool SpawnDelayPerEnemy;
        public float MinDelay;
        public float MaxDelay;
        public AnimationCurve DelayCurve;
        public GameObject SpawnEffectPrefab;
    }
}
