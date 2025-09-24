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
    
    
     /// <summary>
    /// สุ่มตำแหน่ง "แถว ๆ เมาส์" ในวงแหวนรัศมี [minRadius, maxRadius] บนระนาบ worldZ
    /// ใช้ได้ทั้ง Ortho/Perspective (คำนวณด้วย ScreenToWorldPoint)
    /// </summary>
    public static Vector2 RandomNearMouse(Camera cam, float minRadius, float maxRadius, float worldZ = 0f)
    {
        if (maxRadius < minRadius) (minRadius, maxRadius) = (maxRadius, minRadius);
        minRadius = Mathf.Max(0f, minRadius);

        // ตำแหน่งเมาส์ในโลก
        Vector3 sp = Input.mousePosition;
        sp.z = cam.orthographic ? 0f : Mathf.Abs(worldZ - cam.transform.position.z);
        Vector3 mw = cam.ScreenToWorldPoint(sp);
        mw.z = worldZ;

        // สุ่มมุม + รัศมี (แบบ sqrt ให้กระจายสม่ำเสมอ)
        float ang = Random.Range(0f, Mathf.PI * 2f);
        float r   = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));

        return (Vector2)mw + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
    }

    /// <summary>
    /// สุ่ม "เกิดที่ขอบจอ" อิงตำแหน่งเมาส์:
    /// - เลือกขอบที่ใกล้เมาส์ที่สุด
    /// - สุ่มเลื่อนไปตามแนวขอบเล็กน้อย (alongJitter)
    /// - ดันออกไปนอกจอด้วย margin
    /// </summary>
    public static Vector2 RandomAtScreenEdgeNearMouse(Camera cam, float worldZ = 0f, float alongJitter = 1f, float margin = 0.5f)
    {
        // หา rect ของมุมมองที่ระนาบ worldZ
        float minX, maxX, minY, maxY;

        if (cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            minX = c.x - halfW; maxX = c.x + halfW;
            minY = c.y - halfH; maxY = c.y + halfH;
        }
        else
        {
            float d = Mathf.Abs(worldZ - cam.transform.position.z);
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, d));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, d));
            minX = bl.x; maxX = tr.x;
            minY = bl.y; maxY = tr.y;
        }

        // เมาส์โลก
        Vector3 sp = Input.mousePosition;
        sp.z = cam.orthographic ? 0f : Mathf.Abs(worldZ - cam.transform.position.z);
        Vector3 mw3 = cam.ScreenToWorldPoint(sp); mw3.z = worldZ;
        Vector2 mw  = mw3;

        // เลือกขอบที่ใกล้เมาส์ที่สุด
        float dL = Mathf.Abs(mw.x - minX);
        float dR = Mathf.Abs(maxX - mw.x);
        float dB = Mathf.Abs(mw.y - minY);
        float dT = Mathf.Abs(maxY - mw.y);

        float dMin = Mathf.Min(Mathf.Min(dL, dR), Mathf.Min(dB, dT));

        if (dMin == dL)
        {
            float y = Mathf.Clamp(mw.y + Random.Range(-alongJitter, alongJitter), minY, maxY);
            return new Vector2(minX - margin, y);
        }
        if (dMin == dR)
        {
            float y = Mathf.Clamp(mw.y + Random.Range(-alongJitter, alongJitter), minY, maxY);
            return new Vector2(maxX + margin, y);
        }
        if (dMin == dT)
        {
            float x = Mathf.Clamp(mw.x + Random.Range(-alongJitter, alongJitter), minX, maxX);
            return new Vector2(x, maxY + margin);
        }
        else
        {
            float x = Mathf.Clamp(mw.x + Random.Range(-alongJitter, alongJitter), minX, maxX);
            return new Vector2(x, minY - margin);
        }
    }
}
