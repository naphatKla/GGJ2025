using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private float _chance = 1f;

    [Tooltip("Cooldown time (in seconds) before this event can be triggered again")]
    [SerializeField] private float _cooldown = 5f;

    [Tooltip("Delay (in seconds) between spawning each enemy")]
    [SerializeField] private float _spawnDelay = 0.2f;

    [Tooltip("Number of enemies to spawn")]
    [SerializeField] private int _enemyCount = 16;

    [Tooltip("Enemies that can be spawned in this event")]
    [SerializeField] private List<EnemyDataSO> _raidEnemies;

    [Tooltip("The strategy that controls how enemy positions are calculated")]
    [SerializeReference] private ISpawnPositionStrategy _spawnStrategy;

    #endregion

    #region Inspector: Visual & Feedback

    [Title("Visual & Feedback")]
    [Tooltip("List of Feel feedbacks to play when this event is triggered")]
    [SerializeField] private List<MMFeedbacks> _eventFeedback;

    [Tooltip("Optional visual effect spawned at each enemy spawn position")]
    [SerializeField] private GameObject _spawnEffectPrefab;

    #endregion

    #region Inspector: Custom Logic

    [Title("Custom Logic")]
    [Tooltip("Optional list of conditions that must be met before this event can be triggered")]
    [SerializeReference] private List<IEventCondition> _customConditions;
    
    [Title("Timer Trigger")]
    [Tooltip("Event can be triggered by global timer (start at index 0)")]
    [SerializeField] public List<float> timerTrigger;

    #endregion

    #region Runtime Fields

    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();
    private float _lastSpawnTime = -Mathf.Infinity;

    #endregion

    #region Properties

    public float Chance => _chance;
    public float Cooldown => _cooldown;
    public float EnemySpawnEventCount => _enemyCount;
    public List<IEnemyData> RaidEnemies => _raidEnemies?.ConvertAll(e => (IEnemyData)e);

    #endregion

    #region Public API

    /// <summary>
    /// Checks if this event is still in cooldown.
    /// </summary>
    public bool IsCooldownActive(float currentTime)
    {
        return currentTime < _lastSpawnTime + _cooldown;
    }

    /// <summary>
    /// Triggers the event: evaluates conditions, spawns enemies, plays feedbacks, and runs effects.
    /// </summary>
    public void Trigger(IEnemySpawnerView spawnerView, HashSet<GameObject> eventEnemies)
    {
        _lastSpawnTime = Time.time;

        if (!PassCustomConditions()) return;

        var spawnPositions = CalculateSpawnPositions(spawnerView);
        TriggerJuicyEffects();

        if (_spawnDelay > 0)
            SpawnEnemiesWithDelayAsync(spawnerView, spawnPositions, eventEnemies).Forget();
        else
            SpawnEnemies(spawnerView, spawnPositions, eventEnemies);
    }

    #endregion

    #region Private Logic

    /// <summary>
    /// Returns true if all conditions are met.
    /// </summary>
    private bool PassCustomConditions()
    {
        foreach (var condition in _customConditions ?? Enumerable.Empty<IEventCondition>())
            if (!condition.CanTrigger())
                return false;
        return true;
    }

    /// <summary>
    /// Plays assigned feedbacks when event is triggered.
    /// </summary>
    private void TriggerJuicyEffects()
    {
        if (_eventFeedback != null)
        {
            foreach (var feedback in _eventFeedback)
                feedback.PlayFeedbacks();
        }
    }

    /// <summary>
    /// Calculates where enemies should be spawned based on the selected strategy.
    /// </summary>
    private List<Vector2> CalculateSpawnPositions(IEnemySpawnerView spawnerView)
    {
        var spawnPositions = new List<Vector2>();
        var strategy = _spawnStrategy ?? new RaidSpawnStrategy(); // fallback if not assigned

        strategy.CalculatePositions(
            spawnerView.GetPlayerPosition(),
            spawnerView.GetRegionSize(),
            spawnerView.GetMinDistanceFromPlayer(),
            _enemyCount,
            spawnPositions
        );

        // Trim positions if not enough enemies are available in pool
        var available = _raidEnemies.Sum(e => PoolManager.Instance.GetPoolCount(e.EnemyPrefab.name));
        if (available < spawnPositions.Count)
            spawnPositions = spawnPositions.Take(available).ToList();

        return spawnPositions;
    }

    /// <summary>
    /// Spawns enemies at given positions immediately.
    /// </summary>
    private void SpawnEnemies(IEnemySpawnerView spawnerView, List<Vector2> positions, HashSet<GameObject> eventEnemies)
    {
        foreach (var pos in positions)
        {
            var enemyData = GetRandomEventEnemy();
            if (_spawnEffectPrefab != null)
                _spawnerService.Spawn(_spawnEffectPrefab, pos, Quaternion.identity);

            var enemy = _spawnerService.Spawn(
                enemyData.EnemyPrefab,
                pos,
                Quaternion.identity,
                spawnerView.GetEnemyParent()
            );

            if (enemy != null) eventEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Returns a random enemy from the list of available enemies.
    /// </summary>
    private IEnemyData GetRandomEventEnemy()
    {
        if (_raidEnemies == null || _raidEnemies.Count == 0)
            return null;

        return _raidEnemies[Random.Range(0, _raidEnemies.Count)];
    }

    /// <summary>
    /// Spawns enemies one by one with a delay between each spawn.
    /// </summary>
    private async UniTaskVoid SpawnEnemiesWithDelayAsync(
        IEnemySpawnerView spawnerView,
        List<Vector2> spawnPositions,
        HashSet<GameObject> eventEnemies
    )
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_spawnDelay));
        foreach (var position in spawnPositions)
        {
            var enemyData = GetRandomEventEnemy();
            if (enemyData == null) continue;

            if (_spawnEffectPrefab != null)
                _spawnerService.Spawn(_spawnEffectPrefab, position, Quaternion.identity);

            var enemy = _spawnerService.Spawn(
                enemyData.EnemyPrefab,
                position,
                Quaternion.identity,
                spawnerView.GetEnemyParent()
            );

            if (enemy != null) eventEnemies.Add(enemy);
        }
    }

    #endregion
}
