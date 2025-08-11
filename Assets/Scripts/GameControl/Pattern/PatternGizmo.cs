using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl.Pattern
{
    public class PatternGizmo : MonoBehaviour
    {
        public BaseSpawnPattern spawnPattern;

        [Header("Point System")]
        public int currentPoint = 40;
        public int enemyPoint = 5;

        public Vector2 offset;
        public Color gizmoColor = Color.red;

        private void OnDrawGizmos()
        {
            if (spawnPattern == null || currentPoint <= 0 || enemyPoint <= 0)
                return;
            
            int enemyCount = Mathf.Max(1, currentPoint / enemyPoint);

            Gizmos.color = gizmoColor;
            Vector2 center = (Vector2)transform.position + offset;
            var positions = spawnPattern.CalculatePositions(center, enemyCount);

            foreach (var pos in positions)
            {
                Gizmos.DrawWireSphere(pos, 1f);
            }
        }
    }
}