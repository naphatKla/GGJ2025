using UnityEngine;

public interface ISpawnState
{
    void Enter(EnemySpawner spawner);
    void Update(EnemySpawner spawner);
    void Exit(EnemySpawner spawner);
}

public class StopState : ISpawnState
{
    public void Enter(EnemySpawner spawner)
    {
    }

    public void Update(EnemySpawner spawner)
    {
    }

    public void Exit(EnemySpawner spawner)
    {
    }
}

public class SpawningState : ISpawnState
{
    private float spawnTimer;
    private float interval;

    public void Enter(EnemySpawner spawner)
    {
        interval = spawner.CurrentSpawnInterval;
        spawnTimer = 0f;
    }

    public void Update(EnemySpawner spawner)
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= interval && spawner.CanSpawn())
        {
            spawner.Spawn();
            spawnTimer = 0f;
        }

        spawner.UpdateQuota();
    }

    public void Exit(EnemySpawner spawner)
    {
    }
}

public class PausedState : ISpawnState
{
    public void Enter(EnemySpawner spawner)
    {
    }

    public void Update(EnemySpawner spawner)
    {
    }

    public void Exit(EnemySpawner spawner)
    {
    }
}