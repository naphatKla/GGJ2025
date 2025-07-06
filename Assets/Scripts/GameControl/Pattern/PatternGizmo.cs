using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl.Pattern
{
    public class PatternGizmo : MonoBehaviour
    {
        public BaseSpawnPattern spawnPattern;
        public int enemyCount = 8;
        public Vector2 offset;
        public Color gizmoColor = Color.red;

        private void OnDrawGizmos()
        {
            if (spawnPattern == null || enemyCount <= 0)
                return;

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