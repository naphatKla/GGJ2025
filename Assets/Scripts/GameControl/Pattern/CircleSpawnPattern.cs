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

            for (int row = 0; row < rows; row++)
            {
                float radius = baseMinDistance + row * rowSpacing;

                for (int i = 0; i < enemyCount; i++)
                {
                    float angle;
                    if (evenlySpaced)
                        angle = (Mathf.PI * 2f / enemyCount) * i;
                    else
                        angle = Random.Range(0f, Mathf.PI * 2f);

                    angle += Mathf.Deg2Rad * Random.Range(-randomAngleOffset, randomAngleOffset);

                    float actualDistance = radius + Random.Range(0f, randomDistanceOffset);
                    Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * actualDistance;
                    positions.Add(pos);
                }
            }

            return positions;
        }

        public override List<List<Vector2>> CalculateRows(Vector2 center, int enemiesPerRow)
        {
            List<List<Vector2>> result = new();

            int rowCount = Mathf.Max(1, rows);

            for (int rowIndex = 0; rowCount > 0 && rowIndex < rowCount; rowIndex++)
            {
                float radius = baseMinDistance + rowIndex * rowSpacing;
                List<Vector2> rowPositions = new();

                for (int i = 0; i < enemiesPerRow; i++)
                {
                    float angle = (evenlySpaced ? (Mathf.PI * 2f / enemiesPerRow) * i : Random.Range(0, Mathf.PI * 2f));
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

