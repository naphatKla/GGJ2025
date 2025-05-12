using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWorldEvent
{
    float Chance { get; }
    float Cooldown { get; }
    List<IEnemyData> RaidEnemies { get; }
    void Trigger(IEnemySpawnerView spawnerView, HashSet<GameObject> eventEnemies);
    bool IsCooldownActive(float currentTime);
}