using System.Collections;
using System.Collections.Generic;
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
    /// Gets the player's current score.
    /// </summary>
    float GetPlayerScore();
}
