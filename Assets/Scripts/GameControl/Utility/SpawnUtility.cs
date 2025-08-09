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

    public static Vector2 RandomSpawnAroundPlayerCamera(Camera playerCamera, float offset = 0.5f)
    {
        // หามุมซ้ายล่างและขวาบนของกล้องใน World Space
        Vector3 bottomLeft = playerCamera.ViewportToWorldPoint(new Vector3(0, 0, playerCamera.nearClipPlane));
        Vector3 topRight = playerCamera.ViewportToWorldPoint(new Vector3(1, 1, playerCamera.nearClipPlane));

        float minX = bottomLeft.x;
        float maxX = topRight.x;
        float minY = bottomLeft.y;
        float maxY = topRight.y;

        // 0 = ซ้าย, 1 = ขวา, 2 = บน, 3 = ล่าง
        int side = Random.Range(0, 4);
        Vector2 spawnPos = Vector2.zero;

        switch (side)
        {
            case 0: // ซ้าย (นอกกล้องทางซ้าย)
                spawnPos = new Vector2(minX - offset, Random.Range(minY, maxY));
                break;
            case 1: // ขวา
                spawnPos = new Vector2(maxX + offset, Random.Range(minY, maxY));
                break;
            case 2: // บน
                spawnPos = new Vector2(Random.Range(minX, maxX), maxY + offset);
                break;
            case 3: // ล่าง
                spawnPos = new Vector2(Random.Range(minX, maxX), minY - offset);
                break;
        }

        return spawnPos;
    }


    public static Vector2 RandomInsideRegion(Vector2 regionSize)
    {
        float x = Random.Range(-regionSize.x / 2f, regionSize.x / 2f);
        float y = Random.Range(-regionSize.y / 2f, regionSize.y / 2f);
        return new Vector2(x, y);
    }

}
