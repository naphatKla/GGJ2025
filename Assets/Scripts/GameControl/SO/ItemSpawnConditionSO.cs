using GameControl.Controller;
using UnityEngine;

namespace GameControl.SO
{
    public abstract class ItemSpawnConditionSO : ScriptableObject
    {
        public abstract bool IsSatisfied(SpawnerStateController state, MapDataSO mapData, MapDataSO.ItemOption option);
    }
}

