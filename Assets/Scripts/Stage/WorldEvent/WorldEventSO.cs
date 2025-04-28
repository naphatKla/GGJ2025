using System.Collections.Generic;
using UnityEngine;

public enum EventType
{
    EnemyRaid
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
    [SerializeField] private EventType type;
    [SerializeField] private float chance = 1f;
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private List<EnemyDataSO> raidEnemies;
    [SerializeField] private int enemyCount = 16;

    private float lastSpawnTime = -Mathf.Infinity;

    public EventType Type => type;
    public float Chance => chance;
    public float EnemyWorldEventCount => enemyCount;
    public float Cooldown => cooldown;
    public List<IEnemyData> RaidEnemies => raidEnemies.ConvertAll(data => (IEnemyData)data);

    public void GetSpawnPositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCountOverride, List<Vector2> spawnPositions)
    {
        var mainCamera = Camera.main;
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;
        var screenSize = new Vector2(screenWidth, screenHeight);

        if (type == EventType.EnemyRaid)
        {
            var totalEnemies = enemyCountOverride > 0 ? enemyCountOverride : enemyCount;

            var enemiesPerSide = Mathf.CeilToInt(totalEnemies / 4f);


            for (var i = 0; i < enemiesPerSide && spawnPositions.Count < totalEnemies; i++)
            {
                var x = playerPosition.x - screenWidth / 2 + (i + 0.5f) * screenWidth / enemiesPerSide;
                var y = playerPosition.y + screenHeight / 2 + minDistanceFromPlayer;
                var pos = new Vector2(x, y);
                pos.x = Mathf.Clamp(pos.x, -regionSize.x / 2, regionSize.x / 2);
                pos.y = Mathf.Clamp(pos.y, -regionSize.y / 2, regionSize.y / 2);
                spawnPositions.Add(pos);
            }


            for (var i = 0; i < enemiesPerSide && spawnPositions.Count < totalEnemies; i++)
            {
                var x = playerPosition.x - screenWidth / 2 + (i + 0.5f) * screenWidth / enemiesPerSide;
                var y = playerPosition.y - screenHeight / 2 - minDistanceFromPlayer;
                var pos = new Vector2(x, y);
                pos.x = Mathf.Clamp(pos.x, -regionSize.x / 2, regionSize.x / 2);
                pos.y = Mathf.Clamp(pos.y, -regionSize.y / 2, regionSize.y / 2);
                spawnPositions.Add(pos);
            }


            for (var i = 0; i < enemiesPerSide && spawnPositions.Count < totalEnemies; i++)
            {
                var x = playerPosition.x - screenWidth / 2 - minDistanceFromPlayer;
                var y = playerPosition.y - screenHeight / 2 + (i + 0.5f) * screenHeight / enemiesPerSide;
                var pos = new Vector2(x, y);
                pos.x = Mathf.Clamp(pos.x, -regionSize.x / 2, regionSize.x / 2);
                pos.y = Mathf.Clamp(pos.y, -regionSize.y / 2, regionSize.y / 2);
                spawnPositions.Add(pos);
            }


            for (var i = 0; i < enemiesPerSide && spawnPositions.Count < totalEnemies; i++)
            {
                var x = playerPosition.x + screenWidth / 2 + minDistanceFromPlayer;
                var y = playerPosition.y - screenHeight / 2 + (i + 0.5f) * screenHeight / enemiesPerSide;
                var pos = new Vector2(x, y);
                pos.x = Mathf.Clamp(pos.x, -regionSize.x / 2, regionSize.x / 2);
                pos.y = Mathf.Clamp(pos.y, -regionSize.y / 2, regionSize.y / 2);
                spawnPositions.Add(pos);
            }
        }
    }

    public void OnSpawned()
    {
        lastSpawnTime = Time.time;
    }

    public bool IsCooldownActive(float currentTime)
    {
        return currentTime < lastSpawnTime + cooldown;
    }
}