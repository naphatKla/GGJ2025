using UnityEngine;

public interface ISpawnState
{
    void Enter(EnemySpawner spawner, SpawnEventManager eventManager);
    void Update(EnemySpawner spawner, SpawnEventManager eventManager);
    void Exit(EnemySpawner spawner, SpawnEventManager eventManager);
}


public class StopState : ISpawnState
{
    public void Enter(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }

    public void Update(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }

    public void Exit(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }
}

public class SpawningState : ISpawnState
{
    private float normalSpawnTimer;
    private float eventCheckTimer;
    private float normalSpawnInterval;

    public void Enter(EnemySpawner spawner, SpawnEventManager eventManager)
    {
        normalSpawnInterval = spawner.CurrentSpawnInterval;
        normalSpawnTimer = 0f;
        eventCheckTimer = 0f;
    }

    public void Update(EnemySpawner spawner, SpawnEventManager eventManager)
    {
        // Normal enemy spawning
        normalSpawnTimer += Time.deltaTime;
        if (normalSpawnTimer >= normalSpawnInterval && spawner.CanSpawn())
        {
            spawner.SpawnEnemy();
            normalSpawnTimer = 0f;
            normalSpawnInterval = spawner.CurrentSpawnInterval;
        }

        // Spawn event triggering
        eventCheckTimer += Time.deltaTime;
        if (eventCheckTimer >= spawner.EventIntervalCheck)
        {
            eventManager.TriggerSpawnEvent();
            eventCheckTimer = 0f;
        }
        
        eventManager.UpdateTimerTriggers();
        eventManager.UpdateKillTriggers();
        spawner.UpdateQuota();
    }

    public void Exit(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }
}

public class PausedState : ISpawnState
{
    public void Enter(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }

    public void Update(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }

    public void Exit(EnemySpawner spawner, SpawnEventManager eventManager)
    {
    }
}