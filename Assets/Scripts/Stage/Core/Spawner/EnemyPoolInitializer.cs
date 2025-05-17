using UnityEngine;

public static class EnemyPoolInitializer
{
    /// <summary>
    /// Setting up the enemy pool.
    /// </summary>
    public static void Prewarm(StageDataSO stageData, Transform enemyParent)
    {
        foreach (var enemyData in stageData.Enemies)
            for (var i = 0; i < enemyData.PreObjectSpawn; i++)
            {
                var enemy = PoolManager.Instance.Spawn(enemyData.EnemyData.EnemyPrefab, Vector3.zero,
                    Quaternion.identity, enemyParent, true);
                PoolManager.Instance.Despawn(enemy);
            }

        foreach (var spawnEvent in stageData.SpawnEvents)
        foreach (var enemyData in spawnEvent.RaidEnemies)
            for (var i = 0; i < ((SpawnEventSO)spawnEvent).EnemySpawnEventCount; i++)
            {
                var enemy = PoolManager.Instance.Spawn(enemyData.EnemyPrefab, Vector3.zero, Quaternion.identity,
                    enemyParent, true);
                PoolManager.Instance.Despawn(enemy);
            }
    }
}