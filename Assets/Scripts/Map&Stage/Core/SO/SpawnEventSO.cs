using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using Sirenix.OdinInspector;
using UnityEngine;

#region Data Classes

[Serializable]
public class SpawnEnemyProperties : IWeightedEnemy
{
    [HideInInspector]
    public EnemyDataSO EnemyData;
    
    [ValueDropdown("@GetEnemyTypesFromDefault()")]
    public EnemyController EnemyType;

    [Tooltip("Chance to spawn this enemy")]
    public float SpawnChance;
    
    private IEnumerable<EnemyController> GetEnemyTypesFromDefault()
    {
        if (EnemyData == null || EnemyData.enemyData == null)
            return Enumerable.Empty<EnemyController>();

        return EnemyData.enemyData;
    }

    public EnemyController GetCharacterData() => EnemyType;
    public float GetSpawnChance() => SpawnChance;
}

#endregion

[CreateAssetMenu(fileName = "SpawnEventSO", menuName = "Game/Spawn Event")]
public class SpawnEventSO : ScriptableObject, ISpawnEvent
{
    #region Serialized Fields
    [SerializeField] private EnemyDataSO defaultEnemyData;

    [Title("Event Properties")]
    [Tooltip("Chance of this spawn event being selected (0–100%)")]
    [Range(0, 100)]
    [SerializeField] private float _chance = 1f;

    [Tooltip("Cooldown time (in seconds) before this event can be triggered again")]
    [SerializeField] private float _cooldown = 5f;

    [Tooltip("Enemy count when spawning event")]
    [SerializeField] private int _enemyCount = 16;

    [Tooltip("Toggle to spawn delay per enemy when spawning event")]
    [SerializeField] private float _spawnDelayAll = 0.2f;

    [FoldoutGroup("Delay Per Enemy Setting")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [SerializeField] private bool spawnDelayPerEnemy;

    [FoldoutGroup("Delay Per Enemy Setting")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [SerializeField] private float minDelay = 0.05f;

    [FoldoutGroup("Delay Per Enemy Setting")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [SerializeField] private float maxDelay = 0.5f;

    [FoldoutGroup("Delay Per Enemy Setting")]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [SerializeField] private AnimationCurve perEnemyDelayCurve;

    [Tooltip("Enemies and their spawn chances")]
    [SerializeField] private SpawnEnemyProperties[] _spawnEnemies;

    [SerializeField] private GameObject _spawnEffectPrefab;
    [SerializeReference] private ISpawnPositionStrategy _spawnStrategy = new ConfigurableSpawnStrategy();
    [SerializeReference] public List<IEventCondition> _customConditions;

    [Tooltip("Trigger times (seconds) for timed spawn events")]
    [SerializeField] public List<float> timerTrigger;

    [Tooltip("Trigger kill counts for kill-based spawn events")]
    [SerializeField] public List<float> killTrigger;

    #endregion

    #region Runtime State

    private float _lastSpawnTime = -Mathf.Infinity;

    #endregion

    #region Public Properties

    public float Chance => _chance;
    public float Cooldown => _cooldown;
    public int EnemyCount => _enemyCount;
    public SpawnEnemyProperties[] EventEnemies => _spawnEnemies;
    #endregion

    #region Public Methods

    public bool IsCooldownActive(float currentTime) =>
        currentTime < _lastSpawnTime + _cooldown;

    public bool CanTrigger() =>
        _customConditions == null || _customConditions.All(c => c.CanTrigger());

    public void LastTriggered() =>
        _lastSpawnTime = Time.time;

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
            EnemiesWithChance = _spawnEnemies?.ToList(),
            SpawnDelayAll = _spawnDelayAll,
            SpawnDelayPerEnemy = spawnDelayPerEnemy,
            MinDelay = minDelay,
            MaxDelay = maxDelay,
            DelayCurve = perEnemyDelayCurve,
            SpawnEffectPrefab = _spawnEffectPrefab
        };
    }
    
    public void ResetState()
    {
        _lastSpawnTime = -Mathf.Infinity;
    }

    
    private void OnValidate()
    {
        foreach (var enemy in _spawnEnemies)
            enemy.EnemyData = defaultEnemyData;
    }

    #endregion

    #region Nested Data Class

    [Serializable]
    public class SpawnEventData
    {
        public List<Vector2> Positions;
        public int EnemyCount;
        public List<SpawnEnemyProperties> EnemiesWithChance;
        public float SpawnDelayAll;
        public bool SpawnDelayPerEnemy;
        public float MinDelay;
        public float MaxDelay;
        public AnimationCurve DelayCurve;
        public GameObject SpawnEffectPrefab;
    }

    #endregion

    #region Editor Preview (Draw Button)

#if UNITY_EDITOR
    [Button("Preview Spawn Event", ButtonSizes.Large)]
    [GUIColor(0, 1, 0)]
    private void PreviewSpawnArea()
    {
        var playerPos = Vector2.zero;
        var regionSize = new Vector2(20, 20);
        var minDistance = 3f;
        var count = _enemyCount;

        var strategy = _spawnStrategy ?? new ConfigurableSpawnStrategy();
        var spawnPositions = new List<Vector2>();

        strategy.CalculatePositions(playerPos, regionSize, minDistance, count, spawnPositions);

        foreach (var pos in spawnPositions)
            DrawDebugBox(pos, 0.6f, Color.red, 5f);
    }

    private void DrawDebugBox(Vector2 center, float size, Color color, float duration)
    {
        var half = size / 2f;

        var topLeft = new Vector3(center.x - half, center.y + half, 0);
        var topRight = new Vector3(center.x + half, center.y + half, 0);
        var bottomLeft = new Vector3(center.x - half, center.y - half, 0);
        var bottomRight = new Vector3(center.x + half, center.y - half, 0);

        Debug.DrawLine(topLeft, topRight, color, duration);
        Debug.DrawLine(topRight, bottomRight, color, duration);
        Debug.DrawLine(bottomRight, bottomLeft, color, duration);
        Debug.DrawLine(bottomLeft, topLeft, color, duration);
    }
#endif

    #endregion
}
