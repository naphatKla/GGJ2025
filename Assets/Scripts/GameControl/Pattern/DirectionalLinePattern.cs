using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameControl.Pattern
{
    [Flags]
    public enum SpawnDirectionFlag
    {
        None = 0,
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        All = Top | Bottom | Left | Right
    }

    [CreateAssetMenu(menuName = "EnemySpawnPatterns/DirectionalLine")]
    public class DirectionalLinePattern : BaseSpawnPattern
    {
        public SpawnDirectionFlag directions = SpawnDirectionFlag.Left;
        public bool canRandom;
        public int rowsPerDirection = 2;
        public float spacingBetweenEnemies = 1.5f;
        public float spacingBetweenRows = 1.0f;
        public float spawnDistanceFromCenter = 8f;

        public override List<Vector2> CalculatePositions(Vector2 center, int dummyEnemyCount)
        {
            List<Vector2> positions = new();

            var selectedDirections = GetSelectedDirections();

            foreach (var direction in selectedDirections)
            {
                var mainDir = GetMainDirection(direction);
                var sideDir = GetSideDirection(direction);

                for (var row = 0; row < rowsPerDirection; row++)
                {
                    var rowOffset = mainDir * (spawnDistanceFromCenter + row * spacingBetweenRows);
                    var rowCenter = center + rowOffset;

                    for (var i = 0; i < dummyEnemyCount; i++)
                    {
                        var half = (dummyEnemyCount - 1) * spacingBetweenEnemies / 2f;
                        var sideOffset = sideDir * (i * spacingBetweenEnemies - half);
                        var pos = rowCenter + sideOffset;
                        positions.Add(pos);
                    }
                }
            }

            return positions;
        }

        public override List<List<Vector2>> CalculateRows(Vector2 center, int enemiesPerRow)
        {
            List<List<Vector2>> allRows = new();

            var selectedDirections = GetSelectedDirections();

            foreach (var direction in selectedDirections)
            {
                var mainDir = GetMainDirection(direction);
                var sideDir = GetSideDirection(direction);

                for (var row = 0; row < rowsPerDirection; row++)
                {
                    List<Vector2> rowPositions = new();

                    var rowOffset = mainDir * (spawnDistanceFromCenter + row * spacingBetweenRows);
                    var rowCenter = center + rowOffset;
                    var half = (enemiesPerRow - 1) * spacingBetweenEnemies / 2f;

                    for (var i = 0; i < enemiesPerRow; i++)
                    {
                        var sideOffset = sideDir * (i * spacingBetweenEnemies - half);
                        var pos = rowCenter + sideOffset;
                        rowPositions.Add(pos);
                    }

                    allRows.Add(rowPositions);
                }
            }

            return allRows;
        }

        private List<SpawnDirectionFlag> GetSelectedDirections()
        {
            var list = new List<SpawnDirectionFlag>();

            foreach (SpawnDirectionFlag dir in Enum.GetValues(typeof(SpawnDirectionFlag)))
            {
                if (dir == SpawnDirectionFlag.None || !IsPowerOfTwo((int)dir)) continue;

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


        private Vector2 GetMainDirection(SpawnDirectionFlag dir)
        {
            return dir switch
            {
                SpawnDirectionFlag.Top => Vector2.up,
                SpawnDirectionFlag.Bottom => Vector2.down,
                SpawnDirectionFlag.Left => Vector2.left,
                SpawnDirectionFlag.Right => Vector2.right,
                _ => Vector2.zero
            };
        }

        private Vector2 GetSideDirection(SpawnDirectionFlag dir)
        {
            return dir switch
            {
                SpawnDirectionFlag.Top => Vector2.right,
                SpawnDirectionFlag.Bottom => Vector2.right,
                SpawnDirectionFlag.Left => Vector2.up,
                SpawnDirectionFlag.Right => Vector2.up,
                _ => Vector2.zero
            };
        }
    }
}