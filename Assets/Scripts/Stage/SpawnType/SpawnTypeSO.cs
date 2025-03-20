using UnityEngine;

public enum SpawnType
{
    RandomSpawn,
    CircleAroundPlayer,
    RandomSideScreen
}

public interface ISpawnType
{
    float Chance { get; }
    float Cooldown { get; }
    Vector2 GetSpawnPosition(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer);
    void OnSpawned();
    bool IsCooldownActive(float currentTime);
}

[CreateAssetMenu(fileName = "SpawnType", menuName = "Game/SpawnType", order = 1)]
public class SpawnTypeSO : ScriptableObject, ISpawnType
{
    [SerializeField] private SpawnType type;
    [SerializeField] private float chance = 1f;
    [SerializeField] private float cooldown = 1f;

    private float lastSpawnTime = -Mathf.Infinity;

    public SpawnType Type => type;
    public float Chance => chance;
    public float Cooldown => cooldown;

    public Vector2 GetSpawnPosition(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer)
    {
        var mainCamera = Camera.main;
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;
        var screenSize = new Vector2(screenWidth, screenHeight);

        switch (type)
        {
            case SpawnType.RandomSpawn:
                Vector2 spawnPosition;
                var attempts = 0;
                const int maxAttempts = 10;

                do
                {
                    spawnPosition = new Vector2(
                        Random.Range(-regionSize.x / 2, regionSize.x / 2),
                        Random.Range(-regionSize.y / 2, regionSize.y / 2)
                    );
                    attempts++;
                } while (IsPositionOnScreen(spawnPosition, playerPosition, screenSize) ||
                         (Vector2.Distance(spawnPosition, playerPosition) < minDistanceFromPlayer &&
                          attempts < maxAttempts));

                return spawnPosition;

            case SpawnType.CircleAroundPlayer:
                var radius = Mathf.Max(screenWidth, screenHeight) + minDistanceFromPlayer;
                var angle = Random.Range(0f, 2f * Mathf.PI);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                return playerPosition + offset;

            case SpawnType.RandomSideScreen:
                var side = Random.Range(0, 4);
                switch (side)
                {
                    case 0: // Left
                        return new Vector2(-regionSize.x / 2, Random.Range(-regionSize.y / 2, regionSize.y / 2));
                    case 1: // Right
                        return new Vector2(regionSize.x / 2, Random.Range(-regionSize.y / 2, regionSize.y / 2));
                    case 2: // Top
                        return new Vector2(Random.Range(-regionSize.x / 2, regionSize.x / 2), regionSize.y / 2);
                    case 3: // Bottom
                        return new Vector2(Random.Range(-regionSize.x / 2, regionSize.x / 2), -regionSize.y / 2);
                    default:
                        return Vector2.zero;
                }

            default:
                return Vector2.zero;
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

    private bool IsPositionOnScreen(Vector2 position, Vector2 playerPosition, Vector2 screenSize)
    {
        var screenMin = playerPosition - screenSize / 2;
        var screenMax = playerPosition + screenSize / 2;
        return position.x >= screenMin.x && position.x <= screenMax.x &&
               position.y >= screenMin.y && position.y <= screenMax.y;
    }
}