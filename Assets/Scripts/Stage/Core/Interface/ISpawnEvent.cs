using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using UnityEngine;

public interface ISpawnEvent
{
    float Chance { get; }
    float Cooldown { get; }
    List<IEnemyData> EventEnemies { get; }
    void Trigger(IEnemySpawnerView spawnerView, HashSet<EnemyController> eventEnemies);
    bool IsCooldownActive(float currentTime);
}