using GameControl.Controller;
using GameControl.SO;
using UnityEngine;

namespace GameControl.SO
{
    public abstract class SpawnConditionSO : ScriptableObject
    {
        public abstract bool IsSatisfied(SpawnerStateController state, EnemySpawnerController spawner, MapDataSO mapData, MapDataSO.EnemyOption option);
    }
}
