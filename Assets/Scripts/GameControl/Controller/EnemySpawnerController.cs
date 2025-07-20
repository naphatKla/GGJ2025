using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.HeathSystems;
using Cinemachine;
using GameControl.Interface;
using GameControl.SO;
using UnityEngine;
using UnityEngine.Pool;
using VHierarchy.Libs;

namespace GameControl.Controller
{
    public class EnemySpawnerController
    {
        private SO.MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _regionSize;
        
        private Dictionary<string, ObjectPool<EnemyController>> _enemyPools;
        private List<MapDataSO.EnemyOption> _enemyOptionsList;

        public EnemySpawnerController(MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;

            PrewarmEnemy();
        }

        public void PrewarmEnemy()
        {
            _enemyOptionsList = new List<MapDataSO.EnemyOption>();
            _enemyPools = new Dictionary<string, ObjectPool<EnemyController>>();
            
            foreach (var data in _mapdata.EnemyOptions)
            {
                var cloned = new MapDataSO.EnemyOption
                {
                    id = data.id,
                    enemyController = data.enemyController,
                    prewarmCount = data.prewarmCount,
                    EnemyPoint = data.EnemyPoint,
                    enemyPointGrowthRate = data.enemyPointGrowthRate,
                    enemyCanGrowth = data.enemyCanGrowth,
                    useCustomInterval = data.useCustomInterval,
                    customInterval = data.customInterval,
                    EnemyObject = data.EnemyObject
                };
                _enemyOptionsList.Add(cloned);

                _enemyPools[cloned.id] = new ObjectPool<EnemyController>(
                    () => CreateFunc(cloned),
                    obj => ActionOnGet(obj, cloned),
                    obj => ActionOnRelease(obj, cloned),
                    obj => ActionOnDestroy(obj, cloned),
                    false
                );
            }
        }

        private EnemyController CreateFunc(MapDataSO.EnemyOption option)
        {
            var obj = Object.Instantiate(option.EnemyObject);
            var controller = obj.GetComponent<EnemyController>();
            controller.HealthSystem.OnDead = () => _enemyPools[option.id].Release(controller);
            return controller;
        }
        
        public void ActionOnDestroy(EnemyController obj, MapDataSO.EnemyOption option)
        {
            Object.Destroy(obj.gameObject);
        }

        public void ActionOnRelease(EnemyController obj, MapDataSO.EnemyOption option)
        {
            obj.gameObject.SetActive(false);
            SpawnerStateController.Instance.CurrentEnemyPoint += option.EnemyPoint;
        }
        
        private void ActionOnGet(EnemyController obj, MapDataSO.EnemyOption option)
        {
            obj.ResetAllDependentBehavior();
            obj.gameObject.SetActive(true);
        }

        public Dictionary<string, ObjectPool<EnemyController>> GetEnemyList()
        {
            return _enemyPools;
        }
        
        public List<MapDataSO.EnemyOption> GetEnemyOption()
        {
            return _enemyOptionsList;
        }
        
        public void UpgradePointRatio()
        {
            foreach (var data in _enemyOptionsList)
            {
                if (data.enemyCanGrowth)
                    data.EnemyPoint *= (1 + data.enemyPointGrowthRate / 100f);
            }
        }

        public MapDataSO.EnemyOption SpawnEnemy()
        {
            // Random Enemy
            var randomEnemy = RandomUtility.GetWeightedRandom(_enemyOptionsList);
            if (randomEnemy == null) return null;

            // Get GameObject
            if (!_enemyPools.TryGetValue(randomEnemy.id, out var pool)) return null;
            
            var enemyObj = pool.Get();
            enemyObj.transform.position = SpawnUtility.RandomSpawnAroundRegion(_regionSize);
            enemyObj.transform.SetParent(_state.EnemyParent);
            
            // Spawn Point Decrease
            SpawnerStateController.Instance.CurrentEnemyPoint -= randomEnemy.EnemyPoint;

            return randomEnemy;
        }

    }
}
