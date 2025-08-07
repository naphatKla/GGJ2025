using System;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using GameControl.SO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace GameControl.Controller
{
    public class EnemySpawnerController
    {
        private SO.MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _regionSize;
        
        private List<EnemyController> _activeEnemy;
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
            _activeEnemy = new List<EnemyController>();
            _enemyOptionsList = new List<MapDataSO.EnemyOption>();
            _enemyPools = new Dictionary<string, ObjectPool<EnemyController>>();
            
            foreach (var data in _mapdata.EnemyOptions)
            {
                var cloned = new MapDataSO.EnemyOption
                {
                    id = data.id,
                    enemyController = data.enemyController,
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
            controller.HealthSystem.OnDead = () => _enemyPools[option.id].Release(controller); ;
            return controller;
        }
        
        public void ActionOnDestroy(EnemyController obj, MapDataSO.EnemyOption option)
        {
            Object.Destroy(obj.gameObject);
        }

        public void ActionOnRelease(EnemyController obj, MapDataSO.EnemyOption option)
        {
            if (obj.CharacterData is EnemyDataSo enemyData)
            {
                _state.ItemSpawnerController.SpawnExpItem(enemyData.ExpDrop, obj.transform.position);
            }
            
            obj.gameObject.SetActive(false);
            obj.FeedbackSystem.ShowTrail(false);
            obj.transform.position = SpawnUtility.RandomSpawnAroundRegion(_regionSize);
            SpawnerStateController.Instance.CurrentEnemyPoint += option.EnemyPoint;
            _activeEnemy.Remove(obj);
        }
        
        private void ActionOnGet(EnemyController obj, MapDataSO.EnemyOption option)
        {
            obj.ResetAllDependentBehavior();
            obj.transform.position = SpawnUtility.RandomSpawnAroundRegion(_regionSize);
            obj.transform.SetParent(_state.EnemyParent);
            obj.FeedbackSystem.ShowTrail(true);
            obj.gameObject.SetActive(true);

            _activeEnemy.Add(obj);
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
            pool.Get();
            
            // Spawn Point Decrease
            SpawnerStateController.Instance.CurrentEnemyPoint -= randomEnemy.EnemyPoint;

            return randomEnemy;
        }
        
        public void ClearAllEnemysCompletely()
        {
            ReleaseAllEnemies();
            ClearAllEnemys();
        }
        
        public void ClearAllEnemys()
        {
            foreach (var pool in _enemyPools.Values)
            {
                pool.Clear();
            }
            _activeEnemy.Clear();
        }

        public void ReleaseAllEnemies()
        {
            foreach (var enemy in _activeEnemy.ToArray())
            {
                enemy.HealthSystem.TakeDamage(enemy.HealthSystem.MaxHealth);
            }
        }
    }
}
