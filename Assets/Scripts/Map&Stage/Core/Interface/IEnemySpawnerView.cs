using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using UnityEngine;

public interface IEnemySpawnerView
{
    /// <summary>
    /// Gets the parent transform for spawned enemies.
    /// </summary>
    Transform GetEnemyParent();

    /// <summary>
    /// Gets the current number of active enemies.
    /// </summary>
    int GetCurrentEnemyCount();

    /// <summary>
    /// Gets the player's current position.
    /// </summary>
    Vector2 GetPlayerPosition();

    /// <summary>
    /// Sets the background sprite.
    /// </summary>
    void SetBackground(Sprite sprite);

    /// <summary>
    /// Gets the player's current kill count.
    /// </summary>
    float GetPlayerKill();
    
    /// <summary>
    /// Get region size
    /// </summary>
    /// <returns></returns>
    Vector2 GetRegionSize();
    
    /// <summary>
    /// Get Distance from player
    /// </summary>
    /// <returns></returns>
    float GetMinDistanceFromPlayer();
    
    /// <summary>
    /// Spawns event enemies
    /// </summary>
    /// <param name="spawnData"></param>
    /// <param name="onEnemySpawned"></param>
    void SpawnEventEnemies(SpawnEventSO.SpawnEventData spawnData);
}
