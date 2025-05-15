using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CircleSpawnStrategy : ISpawnPositionStrategy
{
    public void CalculatePositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCount, List<Vector2> spawnPositions)
    {
        float radius = minDistanceFromPlayer;
        for (int i = 0; i < enemyCount; i++)
        {
            float angle = (Mathf.PI * 2f / enemyCount) * i;
            Vector2 pos = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            spawnPositions.Add(SpawnUtility.ClampToBounds(pos, regionSize));
        }
    }
}