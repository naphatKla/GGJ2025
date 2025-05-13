using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class WorldEventManager
{
    #region Variables

    private readonly IEnemySpawnerView _spawnerView;
    private readonly StageDataSO _stageData;
    private readonly Vector2 _regionSize;
    private readonly float _minDistanceFromPlayer;

    private readonly List<Vector2> _spawnPositionsPool = new();
    private readonly List<IWorldEvent> _availableEventsPool = new();
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();
    
    private float _currentTime => Timer.Instance.GlobalTimer;
    private readonly Dictionary<IWorldEvent, int> _eventTriggerIndices = new();

    #endregion

    #region Constructor

    public WorldEventManager(IEnemySpawnerView spawnerView, StageDataSO stageData, Vector2 regionSize,
        float minDistanceFromPlayer)
    {
        _spawnerView = spawnerView;
        _stageData = stageData;
        _regionSize = regionSize;
        _minDistanceFromPlayer = minDistanceFromPlayer;
        
        InitializeTimerTriggers();
    }

    #endregion

    #region Methods
    
    /// <summary>
    /// Initializes timerTrigger indices for each WorldEvent.
    /// </summary>
    private void InitializeTimerTriggers()
    {
        foreach (var worldEvent in _stageData.WorldEvents)
        {
            if (worldEvent is WorldEventSO eventSO && eventSO.timerTrigger != null && eventSO.timerTrigger.Count > 0)
            {
                eventSO.timerTrigger.Sort();
                eventSO.timerTrigger.Reverse();
                _eventTriggerIndices[worldEvent] = 0;
            }
        }
    }
    
    /// <summary>
    /// Updates the timer and checks for timerTrigger events.
    /// </summary>
    public void UpdateTimerTriggers(HashSet<GameObject> eventEnemies)
    {
        foreach (var worldEvent in _stageData.WorldEvents)
        {
            if (!_eventTriggerIndices.ContainsKey(worldEvent)) continue;

            var currentIndex = _eventTriggerIndices[worldEvent];
            if (worldEvent is WorldEventSO eventSO && currentIndex < eventSO.timerTrigger.Count)
                if (_currentTime <= eventSO.timerTrigger[currentIndex])
                {
                    worldEvent.Trigger(_spawnerView, eventEnemies);
                    _eventTriggerIndices[worldEvent] = currentIndex + 1;
                }
        }
    }

    /// <summary>
    ///     Triggers a random world event, spawning enemies if conditions are met.
    /// </summary>
    public void TriggerWorldEvent(bool bypassCooldown = false, HashSet<GameObject> eventEnemies = null, bool noChance = false)
    {
        var worldEvent = SelectRandomWorldEvent(bypassCooldown, noChance);
        if (worldEvent == null) return;

        worldEvent.Trigger(_spawnerView, eventEnemies);
    }

    /// <summary>
    ///     Selects a random world event based on chance and cooldown.
    /// </summary>
    private IWorldEvent SelectRandomWorldEvent(bool bypassCooldown, bool noChance = false)
    {
        _availableEventsPool.Clear();

        foreach (var worldEvent in _stageData.WorldEvents)
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

    /// <summary>
    ///     Gets a random raid enemy from a list.
    /// </summary>
    private IEnemyData GetRandomEventEnemy(List<IEnemyData> raidEnemies)
    {
        if (raidEnemies == null || raidEnemies.Count == 0)
            return null;

        return raidEnemies[Random.Range(0, raidEnemies.Count)];
    }

    #endregion
}