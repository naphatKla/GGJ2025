using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameControl.Pattern
{
    [CreateAssetMenu(menuName = "EnemySpawnPatterns/DirectionalCircleBlob")]
    public class DirectionalCircleBlobPattern : BaseSpawnPattern
    {
        public SpawnDirectionFlag directions = SpawnDirectionFlag.Left;
        public bool canRandom;
        public float spawnDistanceFromCenter = 8f;
        public float blobRadius = 2f;
        public float enemySpacing = 0.8f;

        public override List<Vector2> CalculatePositions(Vector2 center, int enemyCount)
        {
            List<Vector2> finalPositions = new();
            var selectedDirs = GetSelectedDirections();

            if (selectedDirs.Count == 0 || enemyCount <= 0)
                return finalPositions;

            bool isRandomSingleDirection = canRandom && selectedDirs.Count == 1;

            int countPerDirection = isRandomSingleDirection ? enemyCount : enemyCount;

            for (int i = 0; i < selectedDirs.Count; i++)
            {
                var dir = selectedDirs[i];
                Vector2 mainDir = GetDirectionVector(dir);
                Vector2 spawnCenter = center + mainDir * spawnDistanceFromCenter;
                
                int count = isRandomSingleDirection ? enemyCount : countPerDirection;

                var blobPositions = GenerateCircleBlob(spawnCenter, blobRadius, enemySpacing, count);
                finalPositions.AddRange(blobPositions);
            }

            return finalPositions;
        }


        private List<SpawnDirectionFlag> GetSelectedDirections()
        {
            var list = new List<SpawnDirectionFlag>();

            foreach (SpawnDirectionFlag dir in Enum.GetValues(typeof(SpawnDirectionFlag)))
            {
                if (dir == SpawnDirectionFlag.None) continue;
                if (!IsPowerOfTwo((int)dir)) continue;

                if (directions.HasFlag(dir))
                    list.Add(dir);
            }

            if (canRandom && list.Count > 0)
            {
                var randomIndex = Random.Range(0, list.Count);
                return new List<SpawnDirectionFlag> { list[randomIndex] };
            }

            return list;
        }

        private bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        private Vector2 GetDirectionVector(SpawnDirectionFlag dir)
        {
            return dir switch
            {
                SpawnDirectionFlag.Top => Vector2.up,
                SpawnDirectionFlag.Bottom => Vector2.down,
                SpawnDirectionFlag.Left => Vector2.left,
                SpawnDirectionFlag.Right => Vector2.right,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), $"Unsupported direction: {dir}")
            };
        }

        private List<Vector2> GenerateCircleBlob(Vector2 center, float radius, float spacing, int maxCount)
        {
            List<Vector2> positions = new();
            int range = Mathf.CeilToInt(radius / spacing);

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    Vector2 offset = new Vector2(x * spacing, y * spacing);
                    if (offset.magnitude <= radius)
                        positions.Add(center + offset);

                    if (positions.Count >= maxCount)
                        return positions;
                }
            }

            return positions;
        }
    }
}
