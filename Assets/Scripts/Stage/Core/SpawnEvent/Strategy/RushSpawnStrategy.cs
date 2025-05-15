using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RushSpawnStrategy : ISpawnPositionStrategy
{
    public void CalculatePositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCount, List<Vector2> spawnPositions)
    {
        var cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        int direction = Random.Range(0, 4);
        Vector2 basePos = Vector2.zero;
        float step;

        switch (direction)
        {
            case 0: // Top
                basePos = new Vector2(playerPosition.x - screenWidth / 2, playerPosition.y + screenHeight / 2 + minDistanceFromPlayer);
                step = screenWidth / enemyCount;
                for (int i = 0; i < enemyCount; i++)
                    spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(basePos.x + step * i, basePos.y), regionSize));
                break;

            case 1: // Bottom
                basePos = new Vector2(playerPosition.x - screenWidth / 2, playerPosition.y - screenHeight / 2 - minDistanceFromPlayer);
                step = screenWidth / enemyCount;
                for (int i = 0; i < enemyCount; i++)
                    spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(basePos.x + step * i, basePos.y), regionSize));
                break;

            case 2: // Left
                basePos = new Vector2(playerPosition.x - screenWidth / 2 - minDistanceFromPlayer, playerPosition.y - screenHeight / 2);
                step = screenHeight / enemyCount;
                for (int i = 0; i < enemyCount; i++)
                    spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(basePos.x, basePos.y + step * i), regionSize));
                break;

            case 3: // Right
                basePos = new Vector2(playerPosition.x + screenWidth / 2 + minDistanceFromPlayer, playerPosition.y - screenHeight / 2);
                step = screenHeight / enemyCount;
                for (int i = 0; i < enemyCount; i++)
                    spawnPositions.Add(SpawnUtility.ClampToBounds(new Vector2(basePos.x, basePos.y + step * i), regionSize));
                break;
        }
    }
}
