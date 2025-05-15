using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpawnEvent
{
    float Chance { get; }
    float Cooldown { get; }
    List<IEnemyData> RaidEnemies { get; }
    void Trigger(IEnemySpawnerView spawnerView, HashSet<GameObject> eventEnemies);
    bool IsCooldownActive(float currentTime);
}