using System.Collections.Generic;
using UnityEngine;

public enum EventType
{
    EnemyRaid
}

public interface ISpawnPositionStrategy
{
    void CalculatePositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer, int enemyCount,
        List<Vector2> spawnPositions);
}

public interface IWorldEvent
{
    float Chance { get; }
    float Cooldown { get; }
    List<IEnemyData> RaidEnemies { get; }

    void GetSpawnPositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer, int enemyCount,
        List<Vector2> spawnPositions);

    void OnSpawned();
    bool IsCooldownActive(float currentTime);
}

[CreateAssetMenu(fileName = "WorldEvent", menuName = "Game/WorldEvent", order = 1)]
public class WorldEventSO : ScriptableObject, IWorldEvent
{
    [SerializeField] private EventType _type;
    [SerializeField] private float _chance = 1f;
    [SerializeField] private float _cooldown = 5f;
    [SerializeField] private List<EnemyDataSO> _raidEnemies;
    [SerializeField] private int _enemyCount = 16;
    private readonly RaidSpawnStrategy _spawnStrategy = new();

    private float _lastSpawnTime = -Mathf.Infinity;

    public EventType Type => _type;
    public float Chance => _chance;
    public float EnemyWorldEventCount => _enemyCount;
    public float Cooldown => _cooldown;
    public List<IEnemyData> RaidEnemies => _raidEnemies.ConvertAll(data => (IEnemyData)data);

    public void GetSpawnPositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCount, List<Vector2> spawnPositions)
    {
        _spawnStrategy.CalculatePositions(playerPosition, regionSize, minDistanceFromPlayer,
            enemyCount > 0 ? enemyCount : _enemyCount, spawnPositions);
    }

    public void OnSpawned()
    {
        _lastSpawnTime = Time.time;
    }

    public bool IsCooldownActive(float currentTime)
    {
        return currentTime < _lastSpawnTime + _cooldown;
    }
}

public class RaidSpawnStrategy : ISpawnPositionStrategy
{
    public void CalculatePositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCount, List<Vector2> spawnPositions)
    {
        var mainCamera = Camera.main;
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;
        var screenSize = new Vector2(screenWidth, screenHeight);

        var enemiesPerSide = Mathf.CeilToInt(enemyCount / 4f);

        // Top side
        for (var i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            var x = playerPosition.x - screenWidth / 2 + (i + 0.5f) * screenWidth / enemiesPerSide;
            var y = playerPosition.y + screenHeight / 2 + minDistanceFromPlayer;
            var pos = new Vector2(x, y);
            pos = ClampPosition(pos, regionSize);
            spawnPositions.Add(pos);
        }

        // Bottom side
        for (var i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            var x = playerPosition.x - screenWidth / 2 + (i + 0.5f) * screenWidth / enemiesPerSide;
            var y = playerPosition.y - screenHeight / 2 - minDistanceFromPlayer;
            var pos = new Vector2(x, y);
            pos = ClampPosition(pos, regionSize);
            spawnPositions.Add(pos);
        }

        // Left side
        for (var i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            var x = playerPosition.x - screenWidth / 2 - minDistanceFromPlayer;
            var y = playerPosition.y - screenHeight / 2 + (i + 0.5f) * screenHeight / enemiesPerSide;
            var pos = new Vector2(x, y);
            pos = ClampPosition(pos, regionSize);
            spawnPositions.Add(pos);
        }

        // Right side
        for (var i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            var x = playerPosition.x + screenWidth / 2 + minDistanceFromPlayer;
            var y = playerPosition.y - screenHeight / 2 + (i + 0.5f) * screenHeight / enemiesPerSide;
            var pos = new Vector2(x, y);
            pos = ClampPosition(pos, regionSize);
            spawnPositions.Add(pos);
        }
    }

    private Vector2 ClampPosition(Vector2 position, Vector2 regionSize)
    {
        return new Vector2(
            Mathf.Clamp(position.x, -regionSize.x / 2, regionSize.x / 2),
            Mathf.Clamp(position.y, -regionSize.y / 2, regionSize.y / 2)
        );
    }
}