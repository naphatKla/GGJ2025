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
        private List<SO.MapDataSO.EnemyOption> _storeEnemy;

        public EnemySpawnerController(SO.MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
        }

        public void PrewarmEnemy()
        {
            _storeEnemy = new List<SO.MapDataSO.EnemyOption>();

            foreach (var data in _mapdata.EnemyOptions)
            {
                var cloned = new SO.MapDataSO.EnemyOption
                {
                    id = data.id,
                    enemyController = data.enemyController,
                    prewarmCount = data.prewarmCount,
                    EnemyPoint = data.EnemyPoint,
                    enemyPointGrowthRate = data.enemyPointGrowthRate,
                    enemyCanGrowth = data.enemyCanGrowth,
                    useCustomInterval = data.useCustomInterval,
                    customInterval = data.customInterval
                };

                _storeEnemy.Add(cloned);

                foreach (var enemy in PoolingSystem.Instance.Prewarm(data.id, data.EnemyObject, data.prewarmCount,
                             _state.EnemyParent))
                {
                    var enemyController = enemy.GetComponent<EnemyController>();
                    enemyController.EnemyId = data.id;
                    enemyController.EnemyPoint = data.EnemyPoint;
                }
            }
        }

        
        public void UpgradePointRatio(string id)
        {
            //Upgrade enemy point ratio
            foreach (var data in _storeEnemy)
            {
                if (data.enemyCanGrowth)
                    data.EnemyPoint *= (1 + data.enemyPointGrowthRate / 100f);
            }
        }

        public SO.MapDataSO.EnemyOption SpawnEnemy()
        {
            //Random
            var randomEnemy = RandomUtility.GetWeightedRandom(_storeEnemy);
            PoolingSystem.Instance.Spawn(randomEnemy.id, SpawnUtility.RandomSpawnAroundRegion(_regionSize));

            //Reduce enemy point
            SpawnerStateController.Instance.CurrentEnemyPoint -= randomEnemy.EnemyPoint;

            return randomEnemy;
        }

 
    }
}
