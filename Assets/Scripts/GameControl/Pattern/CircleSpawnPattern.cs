using System.Collections;
using System.Collections.Generic;
using GameControl.Pattern;
using UnityEngine;

using UnityEngine;

namespace GameControl.Pattern
{
    [CreateAssetMenu(menuName = "EnemySpawnPatterns/Circle")]
    public class CircleSpawnPattern : BaseSpawnPattern
    {
        [Header("Circle Settings")]
        public float baseMinDistance = 5f;
        public float randomDistanceOffset = 2f;
        public float randomAngleOffset = 10f;
        public bool evenlySpaced = true;
        
        [Header("Row Settings")]
        public int rows = 2;
        public float rowSpacing = 2f;
        public bool evenDistributionAcrossRows = true;

        public override List<Vector2> CalculatePositions(Vector2 center, int enemyCount)
        {
            List<Vector2> positions = new();

            if (rows <= 0) rows = 1;
            int baseEnemiesPerRow = Mathf.Max(1, enemyCount / rows);
            int extra = enemyCount % rows;

            for (int row = 0; row < rows; row++)
            {
                int enemiesThisRow = baseEnemiesPerRow + (row < extra ? 1 : 0);
                float radius = baseMinDistance + row * rowSpacing;

                for (int i = 0; i < enemiesThisRow; i++)
                {
                    float angle = (evenlySpaced ? (Mathf.PI * 2f / enemiesThisRow) * i : Random.Range(0, Mathf.PI * 2f));
                    angle += Mathf.Deg2Rad * Random.Range(-randomAngleOffset, randomAngleOffset);

                    float actualDistance = radius + Random.Range(0f, randomDistanceOffset);
                    Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * actualDistance;
                    positions.Add(pos);
                }
            }
            return positions;
        }

        public virtual List<List<Vector2>> CalculateRows(Vector2 center, int totalEnemyCount)
        {
            List<List<Vector2>> result = new();

            int rowCount = Mathf.Max(1, rows);
            int baseEnemiesPerRow = Mathf.Max(1, totalEnemyCount / rowCount);
            int extra = totalEnemyCount % rowCount;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                int enemiesThisRow = baseEnemiesPerRow + (rowIndex < extra ? 1 : 0);
                float radius = baseMinDistance + rowIndex * rowSpacing;
                List<Vector2> rowPositions = new();

                for (int i = 0; i < enemiesThisRow; i++)
                {
                    float angle = (evenlySpaced ? (Mathf.PI * 2f / enemiesThisRow) * i : Random.Range(0, Mathf.PI * 2f));
                    angle += Mathf.Deg2Rad * Random.Range(-randomAngleOffset, randomAngleOffset);

                    float actualDistance = radius + Random.Range(0f, randomDistanceOffset);
                    Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * actualDistance;
                    rowPositions.Add(pos);
                }

                result.Add(rowPositions);
            }

            return result;
        }
    }
}

