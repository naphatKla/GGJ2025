using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl
{
    public class EnemySpawnerController
    {
        [Tooltip("start spawn point of every enemy to spawn (Default 30)")]
        private float _startEnemyPoint;
        [Tooltip("Max spawn point of every enemy to spawn (Default 500)")]
        private float _maxEnemyPoint;
        [Tooltip("Max spawn point of every enemy to spawn (Default 20)")]
        private float _increaseRateEnemyPoint;

        public EnemySpawnerController(SO.MapDataSO mapData, SpawnerStateController state)
        {
            _startEnemyPoint = mapData.startEnemyPoint;
            _maxEnemyPoint = mapData.maxEnemyPoint;
            _increaseRateEnemyPoint = mapData.increaseRateEnemyPoint;
        }
    }
}
