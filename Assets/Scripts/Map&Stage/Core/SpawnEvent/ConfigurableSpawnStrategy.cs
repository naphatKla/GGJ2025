using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

[System.Serializable]
public enum SpawnPattern
{
    Circle,
    Line
}

[System.Serializable]
[Flags]
public enum LineDirection
{
    None = 0,
    Top = 1 << 0,
    Bottom = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3
}

[System.Serializable]
public class ConfigurableSpawnStrategy : ISpawnPositionStrategy
{
    [SerializeField]
    private SpawnPattern pattern = SpawnPattern.Circle;

    [SerializeField]
    [Tooltip("Base minimum distance from player")]
    private float baseMinDistance = 5f;

    [SerializeField]
    [Tooltip("Adds ± randomness to spawn distance")]
    private float randomDistanceOffset = 2f;

    [SerializeField]
    [Tooltip("Spread factor for line pattern, controlling spacing between enemies (default is 1)")]
    private float spreadFactor = 1f;
    
    [SerializeField]
    [ShowIf("@pattern == SpawnPattern.Circle")]
    [Tooltip("For Circle pattern, adds ± random angle (in degrees)")]
    private float randomAngleOffset = 15f;

    [SerializeField]
    [ShowIf("@pattern == SpawnPattern.Line")]
    [Tooltip("For Line pattern, choose one or more spawn directions")]
    private LineDirection lineDirections = LineDirection.None;
    
    public void CalculatePositions(Vector2 playerPosition, Vector2 regionSize, float minDistanceFromPlayer,
        int enemyCount, List<Vector2> spawnPositions)
    {
        var cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        switch (pattern)
        {
            case SpawnPattern.Circle:
                CalculateCirclePositions(playerPosition, regionSize, enemyCount, spawnPositions);
                break;

            case SpawnPattern.Line:
                CalculateLinePositions(playerPosition, regionSize, screenWidth, screenHeight, enemyCount, spawnPositions);
                break;
        }
    }

    private void CalculateCirclePositions(Vector2 playerPosition, Vector2 regionSize, int enemyCount, List<Vector2> spawnPositions)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            float angle = (Mathf.PI * 2f / enemyCount) * i;
            angle += Mathf.Deg2Rad * Random.Range(-randomAngleOffset, randomAngleOffset);
            float distance = baseMinDistance + Random.Range(-randomDistanceOffset, randomDistanceOffset);
            Vector2 pos = playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            spawnPositions.Add(SpawnUtility.ClampToBounds(pos, regionSize));
        }
    }

    private void CalculateLinePositions(Vector2 playerPosition, Vector2 regionSize, float screenWidth,
        float screenHeight, int enemyCount, List<Vector2> spawnPositions)
    {
        var selectedDirections = GetSelectedDirections();
        if (selectedDirections.Count == 0) selectedDirections.Add((LineDirection)(1 << Random.Range(0, 4)));

        var directionsCount = selectedDirections.Count;
        var baseCount = enemyCount / directionsCount;
        var remainder = enemyCount % directionsCount;

        var enemyCounts = Enumerable.Repeat(baseCount, directionsCount).ToList();
        for (var i = 0; i < remainder; i++) enemyCounts[i]++;

        for (var d = 0; d < directionsCount; d++)
        {
            var direction = selectedDirections[d];
            var count = enemyCounts[d];

            var basePos = Vector2.zero;
            float step;

            switch (direction)
            {
                case LineDirection.Top:
                    basePos = new Vector2(playerPosition.x, playerPosition.y + screenHeight / 2);
                    step = screenWidth / count * spreadFactor;
                    for (int i = 0; i < count; i++)
                    {
                        float distance = baseMinDistance + Random.Range(-randomDistanceOffset, randomDistanceOffset);
                        float offsetX = (i - (count - 1) / 2f) * step;
                        Vector2 pos = new Vector2(basePos.x + offsetX, basePos.y + distance);
                        spawnPositions.Add(SpawnUtility.ClampToBounds(pos, regionSize));
                    }
                    break;

                case LineDirection.Bottom:
                    basePos = new Vector2(playerPosition.x, playerPosition.y - screenHeight / 2);
                    step = screenWidth / count * spreadFactor;
                    for (int i = 0; i < count; i++)
                    {
                        float distance = baseMinDistance + Random.Range(-randomDistanceOffset, randomDistanceOffset);
                        float offsetX = (i - (count - 1) / 2f) * step;
                        Vector2 pos = new Vector2(basePos.x + offsetX, basePos.y - distance);
                        spawnPositions.Add(SpawnUtility.ClampToBounds(pos, regionSize));
                    }
                    break;

                case LineDirection.Left:
                    basePos = new Vector2(playerPosition.x - screenWidth / 2, playerPosition.y);
                    step = screenHeight / count * spreadFactor;
                    for (int i = 0; i < count; i++)
                    {
                        float distance = baseMinDistance + Random.Range(-randomDistanceOffset, randomDistanceOffset);
                        float offsetY = (i - (count - 1) / 2f) * step;
                        Vector2 pos = new Vector2(basePos.x - distance, basePos.y + offsetY);
                        spawnPositions.Add(SpawnUtility.ClampToBounds(pos, regionSize));
                    }
                    break;

                case LineDirection.Right:
                    basePos = new Vector2(playerPosition.x + screenWidth / 2, playerPosition.y);
                    step = screenHeight / count * spreadFactor;
                    for (int i = 0; i < count; i++)
                    {
                        float distance = baseMinDistance + Random.Range(-randomDistanceOffset, randomDistanceOffset);
                        float offsetY = (i - (count - 1) / 2f) * step;
                        Vector2 pos = new Vector2(basePos.x + distance, basePos.y + offsetY);
                        spawnPositions.Add(SpawnUtility.ClampToBounds(pos, regionSize));
                    }
                    break;
            }
        }
    }


    private List<LineDirection> GetSelectedDirections()
    {
        List<LineDirection> directions = new List<LineDirection>();
        if ((lineDirections & LineDirection.Top) != 0) directions.Add(LineDirection.Top);
        if ((lineDirections & LineDirection.Bottom) != 0) directions.Add(LineDirection.Bottom);
        if ((lineDirections & LineDirection.Left) != 0) directions.Add(LineDirection.Left);
        if ((lineDirections & LineDirection.Right) != 0) directions.Add(LineDirection.Right);
        return directions;
    }
}
