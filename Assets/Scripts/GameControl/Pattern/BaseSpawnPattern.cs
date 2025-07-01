using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl.Pattern
{
    public abstract class BaseSpawnPattern : ScriptableObject
    {
        public abstract List<Vector2> CalculatePositions(Vector2 center, int enemyCount);
    
        public virtual List<List<Vector2>> CalculateRows(Vector2 center, int enemyCount)
        {
            var singleRow = CalculatePositions(center, enemyCount);
            return new List<List<Vector2>> { singleRow };
        }
    }
}