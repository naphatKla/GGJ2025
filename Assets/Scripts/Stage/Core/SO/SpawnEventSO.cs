using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "[SE] Stage-Level-x", menuName = "Game/SpawnEventData", order = 1)]
public class SpawnEventSO : ScriptableObject, ISpawnEvent
{
    #region Inspector: Event Properties

    [Title("Event Properties")]
    [Tooltip("Chance of this speed event being selected (0–100%)")]
    [Range(0, 100)]
    [SerializeField]
    private float _chance = 1f;

    [Tooltip("Cooldown time (in seconds) before this event can be triggered again")] [SerializeField]
    private float _cooldown = 5f;

    [Tooltip("Delay (in seconds) between spawning all enemy")] [SerializeField]
    private float _spawnDelayAll = 0.2f;

    [Tooltip("Toggle to spawn delay per enemy when spawning event")] [SerializeField]
    private bool spawnDelayPerEnemy;

    [Tooltip("Minimum Delay between spawning per enemy when spawning event")]
    [SerializeField]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")]
    private float minDelay = 0.05f;

    [Tooltip("Maximum Delay between spawning per enemy when spawning event")]
    [SerializeField]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")]
    private float maxDelay = 0.5f;

    [Tooltip("Delay between spawning per enemy when spawning event")]
    [SerializeField]
    [ShowIf("@spawnDelayPerEnemy == true")]
    [FoldoutGroup("Delay Per Enemy Setting")]
    private AnimationCurve perEnemyDelayCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Number of enemies to spawn")] [SerializeField]
    private int _enemyCount = 16;

    [Tooltip("Enemies that can be spawned in this event")] [SerializeField]
    private List<EnemyDataSO> _raidEnemies;

    [Tooltip("The strategy that controls how enemy positions are calculated")]
    [FoldoutGroup("Strategy Setting")]
    [SerializeReference]
    private ISpawnPositionStrategy _spawnStrategy = new ConfigurableSpawnStrategy();

    #endregion

    #region Inspector: Visual & Feedback

    [Title("Visual & Feedback")]
    [Tooltip("List of Feel feedbacks to play when this event is triggered")]
    [SerializeField]
    private List<MMFeedbacks> _eventFeedback;

    [Tooltip("Optional visual effect spawned at each enemy spawn position")] [SerializeField]
    private GameObject _spawnEffectPrefab;

    #endregion

    #region Inspector: Custom Logic

    [Title("Custom Logic")]
    [Tooltip("Optional list of conditions that must be met before this event can be triggered")]
    [SerializeReference]
    private List<IEventCondition> _customConditions;

    [Title("Timer Trigger")] [Tooltip("Event can be triggered by global timer (start at index 0)")] [SerializeField]
    public List<float> timerTrigger;

    [Title("Kill Trigger")] [Tooltip("Event can be triggered by global kill (start at index 0)")] [SerializeField]
    public List<float> killTrigger;

    #endregion

    #region Runtime Fields

    private ISpawnerService _spawnerService;
    private float _lastSpawnTime = -Mathf.Infinity;

    #endregion

    #region Properties

    public float Chance => _chance;
    public float Cooldown => _cooldown;
    public float EnemySpawnEventCount => _enemyCount;
    public List<IEnemyData> EventEnemies => _raidEnemies?.ConvertAll(e => (IEnemyData)e);
    public event Action<EnemyController> OnEnemySpawnedInEvent;

    #endregion

    #region Public API
    
    /// <summary>
    ///     Checks if this event is still in cooldown.
    /// </summary>
    public bool IsCooldownActive(float currentTime)
    {
        return currentTime < _lastSpawnTime + _cooldown;
    }
    
    /// <summary>
    /// Inialize for spawner service
    /// </summary>
    /// <param name="spawnerService"></param>
    public void SetSpawnerService(ISpawnerService spawnerService)
    {
        _spawnerService = spawnerService;
    }

    /// <summary>
    ///     Triggers the event: evaluates conditions, spawns enemies, plays feedbacks, and runs effects.
    /// </summary>
    public void Trigger(IEnemySpawnerView spawnerView, HashSet<EnemyController> eventEnemies)
    {
        _lastSpawnTime = Time.time;

        if (!PassCustomConditions()) return;
      
        var spawnPositions = CalculateSpawnPositions(spawnerView);
        TriggerJuicyEffects();

        var spawnData = new SpawnEventData
        {
            Positions = spawnPositions,
            EnemyCount = _enemyCount,
            Enemies = _raidEnemies?.ConvertAll(e => (IEnemyData)e),
            SpawnDelayAll = _spawnDelayAll,
            SpawnDelayPerEnemy = spawnDelayPerEnemy,
            MinDelay = minDelay,
            MaxDelay = maxDelay,
            DelayCurve = perEnemyDelayCurve,
            SpawnEffectPrefab = _spawnEffectPrefab
        };
        
        spawnerView.SpawnEventEnemies(spawnData, OnEnemySpawnedInEvent);
    }

    #endregion

    #region Private Logic

    /// <summary>
    ///     Returns true if all conditions are met.
    /// </summary>
    private bool PassCustomConditions()
    {
        foreach (var condition in _customConditions ?? Enumerable.Empty<IEventCondition>())
            if (!condition.CanTrigger())
                return false;
        return true;
    }

    /// <summary>
    ///     Plays assigned feedbacks when event is triggered.
    /// </summary>
    private void TriggerJuicyEffects()
    {
        if (_eventFeedback != null)
        {
            /*foreach (var feedback in _eventFeedback)
                feedback.PlayFeedbacks();*/
        }
    }

    /// <summary>
    ///     Calculates where enemies should be spawned based on the selected strategy.
    /// </summary>
    private List<Vector2> CalculateSpawnPositions(IEnemySpawnerView spawnerView)
    {
        var spawnPositions = new List<Vector2>();
        var strategy = _spawnStrategy ?? new ConfigurableSpawnStrategy();

        strategy.CalculatePositions(
            spawnerView.GetPlayerPosition(),
            spawnerView.GetRegionSize(),
            spawnerView.GetMinDistanceFromPlayer(),
            _enemyCount,
            spawnPositions
        );
        return spawnPositions;
    }
    
    /// <summary>
    ///     Returns a random enemy from the list of available enemies.
    /// </summary>
    private IEnemyData GetRandomEventEnemy()
    {
        if (_raidEnemies == null || _raidEnemies.Count == 0)
            return null;

        return _raidEnemies[Random.Range(0, _raidEnemies.Count)];
    }
    #endregion

    #region Drawbutton

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

        foreach (var pos in spawnPositions) DrawDebugBox(pos, 0.6f, Color.red, 5f);
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