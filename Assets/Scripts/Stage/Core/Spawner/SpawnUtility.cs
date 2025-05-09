using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpawnUtility
{
    /// <summary>
    /// Clamp the position
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="regionSize"></param>
    /// <returns></returns>
    public static Vector2 ClampToBounds(Vector2 pos, Vector2 regionSize)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, -regionSize.x / 2, regionSize.x / 2),
            Mathf.Clamp(pos.y, -regionSize.y / 2, regionSize.y / 2)
        );
    }

    /// <summary>
    /// Is on player screen
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="player"></param>
    /// <param name="screen"></param>
    /// <returns></returns>
    public static bool IsOnScreen(Vector2 pos, Vector2 player, Vector2 screen)
    {
        var min = player - screen / 2;
        var max = player + screen / 2;
        return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
    }
}
