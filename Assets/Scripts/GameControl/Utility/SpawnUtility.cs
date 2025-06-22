using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpawnUtility
{
    public static Vector2 ClampToBounds(Vector2 pos, Vector2 regionSize)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, -regionSize.x / 2, regionSize.x / 2),
            Mathf.Clamp(pos.y, -regionSize.y / 2, regionSize.y / 2)
        );
    }
    
    public static bool IsOnScreen(Vector2 pos, Vector2 player, Vector2 screen)
    {
        var min = player - screen / 2;
        var max = player + screen / 2;
        return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
    }
    
    public static Vector2 RandomSpawnAroundRegion(Vector2 regionSize, float offset = 0.5f)
    {
        int edge = Random.Range(0, 4); // 0 = top, 1 = bottom, 2 = left, 3 = right
        float x, y;

        switch (edge)
        {
            case 0: // Top
                x = Random.Range(-regionSize.x / 2f, regionSize.x / 2f);
                y = regionSize.y / 2f + offset;
                break;
            case 1: // Bottom
                x = Random.Range(-regionSize.x / 2f, regionSize.x / 2f);
                y = -regionSize.y / 2f - offset;
                break;
            case 2: // Left
                x = -regionSize.x / 2f - offset;
                y = Random.Range(-regionSize.y / 2f, regionSize.y / 2f);
                break;
            case 3: // Right
                x = regionSize.x / 2f + offset;
                y = Random.Range(-regionSize.y / 2f, regionSize.y / 2f);
                break;
            default:
                x = y = 0f;
                break;
        }
        return new Vector2(x, y);
    }
}
