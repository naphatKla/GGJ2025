using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.HeathSystems;
using UnityEngine;

namespace GameControl
{
    public class EnemySpawnerController
    {
        private SO.MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _regionSize;

        public EnemySpawnerController(SO.MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
        }

        public void PrewarmEnemy()
        {
            foreach (var data in _mapdata.EnemyOptions)
            foreach (var enemy in PoolingSystem.Instance.Prewarm(data.id, data.EnemyObject, data.prewarmCount, _state.EnemyParent))
            {
                EnemyController enemyController = enemy.GetComponent<EnemyController>();
                enemyController.EnemyId = data.id;
                enemyController.EnemyPoint = data.EnemyPoint;
            }
        }


        public void UpgradeSpawnRatio()
        {
            //Upgrade 12.5% spawn chance
            foreach (var allEnemydata in _mapdata.EnemyOptions)
            {
                allEnemydata.Chance *= (1 + allEnemydata.enemyPointGrowthRate / 100f);
            }
        }

        public SO.MapDataSO.EnemyOption SpawnEnemy()
        {
            //Random enemy type and callback
            var randomEnemy = RandomUtility.GetWeightedRandom(_mapdata.EnemyOptions);
            PoolingSystem.Instance.Spawn(randomEnemy.EnemyId, SpawnUtility.RandomSpawnAroundRegion(_regionSize));
            
            //หักลบ Spawn Point ออก
            SpawnerStateController.Instance.CurrentEnemyPoint -= randomEnemy.EnemyPoint;
            
            return randomEnemy;
        }
 
    }
}
