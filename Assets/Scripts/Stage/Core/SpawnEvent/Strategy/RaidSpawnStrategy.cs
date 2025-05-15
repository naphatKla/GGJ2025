using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RaidSpawnStrategy : ISpawnPositionStrategy
{
    public void CalculatePositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCount, List<Vector2> spawnPositions)
    {
        var cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        int enemiesPerSide = Mathf.CeilToInt(enemyCount / 4f);

        // Top
        for (int i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            float x = playerPosition.x - screenWidth / 2 + (i + 0.5f) * screenWidth / enemiesPerSide;
            float y = playerPosition.y + screenHeight / 2 + minDistanceFromPlayer;
            spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(x, y), regionSize));
        }

        // Bottom
        for (int i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            float x = playerPosition.x - screenWidth / 2 + (i + 0.5f) * screenWidth / enemiesPerSide;
            float y = playerPosition.y - screenHeight / 2 - minDistanceFromPlayer;
            spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(x, y), regionSize));
        }

        // Left
        for (int i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            float x = playerPosition.x - screenWidth / 2 - minDistanceFromPlayer;
            float y = playerPosition.y - screenHeight / 2 + (i + 0.5f) * screenHeight / enemiesPerSide;
            spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(x, y), regionSize));
        }

        // Right
        for (int i = 0; i < enemiesPerSide && spawnPositions.Count < enemyCount; i++)
        {
            float x = playerPosition.x + screenWidth / 2 + minDistanceFromPlayer;
            float y = playerPosition.y - screenHeight / 2 + (i + 0.5f) * screenHeight / enemiesPerSide;
            spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(x, y), regionSize));
        }
    }
}
