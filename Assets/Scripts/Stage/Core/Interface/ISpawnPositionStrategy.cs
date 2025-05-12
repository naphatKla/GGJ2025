using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpawnPositionStrategy
{
    void CalculatePositions(
        Vector2 playerPosition,
        Vector2 regionSize,
        float minDistanceFromPlayer,
        int enemyCount,
        List<Vector2> spawnPositions
    );
}
