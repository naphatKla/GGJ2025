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
    private float normalSpawnTimer;
    private float eventCheckTimer;
    private float normalSpawnInterval;

    public void Enter(EnemySpawner spawner)
    {
        normalSpawnInterval = spawner.CurrentSpawnInterval;
        normalSpawnTimer = 0f;
        eventCheckTimer = 0f;
    }

    public void Update(EnemySpawner spawner)
    {
        // Normal enemy spawning
        normalSpawnTimer += Time.deltaTime;
        if (normalSpawnTimer >= normalSpawnInterval && spawner.CanSpawn())
        {
            spawner.SpawnEnemy();
            normalSpawnTimer = 0f;
            normalSpawnInterval = spawner.CurrentSpawnInterval;
        }

        // World event triggering
        eventCheckTimer += Time.deltaTime;
        if (eventCheckTimer >= spawner.EventIntervalCheck)
        {
            spawner.TriggerWorldEvent();
            eventCheckTimer = 0f;
        }
        
        spawner.UpdateTimerTriggers();
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