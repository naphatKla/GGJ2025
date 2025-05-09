using System.Collections.Generic;
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
    
    private readonly EnemySpawnLogic _spawnLogic;
    private readonly ISpawnerService _spawnerService = new ObjectPoolSpawnerService();

    #endregion

    #region Constructor

    public WorldEventManager(IEnemySpawnerView spawnerView, StageDataSO stageData, Vector2 regionSize,
        float minDistanceFromPlayer)
    {
        _spawnerView = spawnerView;
        _stageData = stageData;
        _regionSize = regionSize;
        _minDistanceFromPlayer = minDistanceFromPlayer;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Triggers a random world event, spawning enemies if conditions are met.
    /// </summary>
    public void TriggerWorldEvent(bool bypassCooldown = false, List<GameObject> eventEnemies = null)
    {
        var worldEvent = SelectRandomWorldEvent(bypassCooldown);
        if (worldEvent == null) return;

        _spawnPositionsPool.Clear();
        worldEvent.GetSpawnPositions(_spawnerView.GetPlayerPosition(), _regionSize, _minDistanceFromPlayer, 0,
            _spawnPositionsPool);

        foreach (var position in _spawnPositionsPool)
        {
            var enemyData = GetRandomEventEnemy(worldEvent.RaidEnemies);
            if (enemyData == null) continue;
            var enemy = _spawnerService.Spawn(enemyData.EnemyPrefab, position, Quaternion.identity);
            if (enemy != null)
            {
                enemy.transform.SetParent(_spawnerView.GetEnemyParent());
                eventEnemies?.Add(enemy);
            }
        }
        worldEvent.OnSpawned();
    }

    /// <summary>
    ///     Selects a random world event based on chance and cooldown.
    /// </summary>
    private IWorldEvent SelectRandomWorldEvent(bool bypassCooldown)
    {
        _availableEventsPool.Clear();

        foreach (var worldEvent in _stageData.WorldEvents)
        {
            var randomChance = Random.Range(0f, 100f);
            if (randomChance > worldEvent.Chance) continue;

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