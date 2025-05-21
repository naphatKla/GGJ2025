using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using UnityEngine;
using UnityEngine.Events;


public class SpawnEventManager
{
    #region Variables

    private readonly IEnemySpawnerView _spawnerView;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;

    private readonly List<Vector2> _spawnPositionsPool = new();
    private readonly List<ISpawnEvent> _availableEventsPool = new();
    private readonly ISpawnerService _spawnerService;
    
    private float _currentTime => Timer.Instance.GlobalTimer;
    private float _killTrigger => _spawnerView.GetPlayerKill();
    private readonly Dictionary<ISpawnEvent, int> _timerTriggerIndices = new();
    private readonly Dictionary<ISpawnEvent, int> _killTriggerIndices = new();

    public UnityEvent onDespawntrigger;
    
    #endregion

    #region Constructor

    public SpawnEventManager(IEnemySpawnerView spawnerView, StageDataSO stageData, Vector2 regionSize,
        float minDistanceFromPlayer, ISpawnerService spawnerService)
    {
        _spawnerView = spawnerView;
        _stageData = stageData;
        _regionSize = regionSize;
        _minDistanceFromPlayer = minDistanceFromPlayer;
        _spawnerService = spawnerService;
        InitializeEventTriggerIndices();
    }

    #endregion

    #region Methods
    
    /// <summary>
    /// Initializes timerTrigger indices for each WorldEvent.
    /// </summary>
    private void InitializeEventTriggerIndices()
    {
        foreach (var spawnEvent in _stageData.SpawnEvents)
        {
            if (!_timerTriggerIndices.ContainsKey(spawnEvent))
                _timerTriggerIndices[spawnEvent] = 0;

            if (!_killTriggerIndices.ContainsKey(spawnEvent))
                _killTriggerIndices[spawnEvent] = 0;

            if (spawnEvent is SpawnEventSO eventSO)
            {
                eventSO.timerTrigger?.Sort();
                eventSO.killTrigger?.Sort();
            }
        }
    }


    
    /// <summary>
    /// Updates the timer and checks for timerTrigger events.
    /// </summary>
    public void UpdateTimerTriggers(HashSet<EnemyController> eventEnemies)
    {
        foreach (var spawnEvent in _stageData.SpawnEvents)
        {
            if (!_timerTriggerIndices.ContainsKey(spawnEvent)) continue;

            var currentIndex = _timerTriggerIndices[spawnEvent];

            if (spawnEvent is SpawnEventSO eventSO && currentIndex < eventSO.timerTrigger.Count)
                if (_currentTime <= eventSO.timerTrigger[currentIndex] && eventSO.CanTrigger())
                {
                    eventSO.LastTriggered();
                    var data = eventSO.CreateSpawnData(_spawnerView);
                    _spawnerView.SpawnEventEnemies(data);
                    _timerTriggerIndices[spawnEvent] = currentIndex + 1;
                }
        }
    }


    
    /// <summary>
    /// Updates the kill and checks for killTrigger events.
    /// </summary>
    public void UpdateKillTriggers(HashSet<EnemyController> eventEnemies)
    {
        foreach (var spawnEvent in _stageData.SpawnEvents)
        {
            if (!_killTriggerIndices.ContainsKey(spawnEvent)) continue;

            var currentIndex = _killTriggerIndices[spawnEvent];

            if (spawnEvent is SpawnEventSO eventSO && currentIndex < eventSO.killTrigger.Count)
                if (_killTrigger >= eventSO.killTrigger[currentIndex] && eventSO.CanTrigger())
                {
                    eventSO.LastTriggered();
                    var data = eventSO.CreateSpawnData(_spawnerView);
                    _spawnerView.SpawnEventEnemies(data);
                    _killTriggerIndices[spawnEvent] = currentIndex + 1;
                }
        }
    }


    /// <summary>
    ///     Triggers a random world event, spawning enemies if conditions are met.
    /// </summary>
    public void TriggerSpawnEvent(bool bypassCooldown = false, bool noChance = false)
    {
        var spawnEvent = SelectRandomWorldEvent(bypassCooldown, noChance);
        if (spawnEvent == null) return;

        if (spawnEvent is not SpawnEventSO eventSO) return;

        if (!eventSO.CanTrigger()) return;
        
        int availableCount = CountAvailableEnemiesInPool(eventSO);
        if (availableCount < eventSO.EnemyCount) { return; }
        
        eventSO.LastTriggered();
        var data = eventSO.CreateSpawnData(_spawnerView);
        _spawnerView.SpawnEventEnemies(data);
    }
    
    /// <summary>
    /// Count Enemies
    /// </summary>
    /// <param name="eventSO"></param>
    /// <returns></returns>
    private int CountAvailableEnemiesInPool(SpawnEventSO eventSO)
    {
        int count = 0;
        foreach (var enemyData in eventSO.EventEnemies)
        {
            GameObject prefab = enemyData.EnemyController.gameObject;
            count += _spawnerService.CountAvailable(prefab);
        }
        return count;
    }
    
    /// <summary>
    ///     Selects a random world event based on chance and cooldown.
    /// </summary>
    private ISpawnEvent SelectRandomWorldEvent(bool bypassCooldown, bool noChance = false)
    {
        _availableEventsPool.Clear();

        foreach (var worldEvent in _stageData.SpawnEvents)
        {
            if (!noChance)
            {
                var randomChance = Random.Range(0f, 100f);
                if (randomChance > worldEvent.Chance) continue;
            }

            if (bypassCooldown || !worldEvent.IsCooldownActive(Time.time))
                _availableEventsPool.Add(worldEvent);
        }

        if (_availableEventsPool.Count == 0) return null;
        return _availableEventsPool[Random.Range(0, _availableEventsPool.Count)];
    }
    #endregion
}