using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using DG.Tweening;
using GameControl.SO;
using UI.Manager;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace GameControl.Controller
{
    public class EnemySpawnerController
    {
        private MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _regionSize;
        
        private List<EnemyController> _activeEnemy;
        private Dictionary<string, ObjectPool<EnemyController>> _enemyPools;
        private List<MapDataSO.EnemyOption> _enemyOptionsList;
        private bool _debug;
        private Camera _mainCamera;
        private float _anergyDropMultiplier = 1;
        
        public event Action<EnemyController, MapDataSO.EnemyOption> OnFirstSpawned;
        private readonly HashSet<string> _firstSpawnedTypeIds = new();
        
        public EnemySpawnerController(MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion, bool debug, Camera mainCamera)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
            _debug = debug;
            _mainCamera = mainCamera;

            PrewarmEnemy();
        }

        #region Pooling
        private EnemyController CreateFunc(MapDataSO.EnemyOption option)
        {
            var obj = Object.Instantiate(option.EnemyObject);
            var controller = obj.GetComponent<EnemyController>();
            controller.HealthSystem.OnDead = () => _enemyPools[option.id].Release(controller);
            if (option.modifyNewData) 
                controller.AssignCharacterData(option.enemyData);
            else 
                controller.AssignCharacterData(controller.CharacterData);
            return controller;
        }
        
        public void ActionOnDestroy(EnemyController obj, MapDataSO.EnemyOption option)
        {
            Object.Destroy(obj.gameObject);
        }

        public void ActionOnRelease(EnemyController obj, MapDataSO.EnemyOption option)
        {
            DOTween.Kill(obj.transform, complete: true);
            if (obj.CharacterData is EnemyDataSo enemyData)
            {
                int totalExp = Mathf.CeilToInt(enemyData.ExpDrop * _anergyDropMultiplier);
                _state.ItemSpawnerController.SpawnExpItem(totalExp, obj.transform.position);
            }

            if (obj.CountedByMax)
            {
                option.activeCount = Mathf.Max(0, option.activeCount - 1);
            }
            
            obj.gameObject.SetActive(false);
            obj.FeedbackSystem.ShowTrail(false);
            obj.transform.position = SpawnUtility.RandomSpawnAroundPlayerCamera(_mainCamera, 10f);
            SpawnerStateController.Instance.CurrentEnemyPoint += option.EnemyPoint;
            _activeEnemy.Remove(obj);
        }

        public void SetAnergyDropMultiplier(float value)
        {
            _anergyDropMultiplier = Mathf.Max(0, value);
        }
        
        private void OnFirstEnemySpawn(EnemyController obj, MapDataSO.EnemyOption option)
        {
            if (GameStateController.Instance.MapState == MapState.Rush) return;
            NotificationManager.Instance.PlayNotification("notify_enemy", $"NEW ENEMY DETECT - {option.displayName}", 4f, option.displayName);
            OnFirstSpawned?.Invoke(obj, option);
        }
        
        private void ActionOnGet(EnemyController obj, MapDataSO.EnemyOption option)
        {
            DOTween.Kill(obj.transform, complete: true);
            
            bool firstOfType = _firstSpawnedTypeIds.Add(option.id);
            if (firstOfType)
            {
                OnFirstEnemySpawn(obj, option);
            }
            obj.transform.position = SpawnUtility.RandomBetweenMouseAndCamera(_mainCamera);
            obj.transform.SetParent(_state.EnemyParent);
            obj.FeedbackSystem.ShowTrail(true);
            obj.ResetAllDependentBehavior();
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
        
        public List<MapDataSO.EnemyOption> PickEnemy(bool bypass)
        {
            List<MapDataSO.EnemyOption> candidates;

            if (bypass)
                candidates = _enemyOptionsList.ToList();
            else
                candidates = _enemyOptionsList
                    .Where(e => e.IsSpawnable(_state, _mapdata))
                    .ToList();

            return candidates.Count == 0 ? null : candidates;
        }
        
        public bool ConditionCheck(MapDataSO.EnemyOption opt)
        {
            if (!opt.IsBelowPerEnemyMax()) return false;
            return true;
        }
        
        public MapDataSO.EnemyOption SpawnEnemy()
        {
            var randomEnemy = RandomUtility.GetWeightedRandom(PickEnemy(false));
            if (randomEnemy == null) return null;
            if (!ConditionCheck(randomEnemy)) return null;
            
            if (!_enemyPools.TryGetValue(randomEnemy.id, out var pool)) return null;
            var inst = pool.Get();
            inst.CountedByMax = true;
            
            randomEnemy.activeCount++;
            SpawnerStateController.Instance.CurrentEnemyPoint -= randomEnemy.EnemyPoint;
            return randomEnemy;
        }
        
        #endregion
        
        #region Public Method
        public void PrewarmEnemy()
        {
            _firstSpawnedTypeIds.Clear();
            _activeEnemy = new List<EnemyController>();
            _enemyOptionsList = new List<MapDataSO.EnemyOption>();
            _enemyPools = new Dictionary<string, ObjectPool<EnemyController>>();
            
            foreach (var data in _mapdata.EnemyOptions)
            {
                var cloned = new MapDataSO.EnemyOption
                {
                    id = data.id,
                    displayName = data.displayName,
                    enemyController = data.enemyController,
                    EnemyPoint = data.EnemyPoint,
                    enemyPointGrowthRate = data.enemyPointGrowthRate,
                    enemyPointCanGrowth = data.enemyPointCanGrowth,
                    enemyChanceGrowthRate = data.enemyChanceGrowthRate,
                    enemyChanceCanGrowth = data.enemyChanceCanGrowth,
                    useCustomInterval = data.useCustomInterval,
                    customInterval = data.customInterval,
                    EnemyObject = data.EnemyObject,
                    Chance = data.Chance,
                    modifyNewData = data.modifyNewData,
                    enemyData = data.enemyData,
                    useSpawnConditions = data.useSpawnConditions,
                    conditionLogic = data.conditionLogic,
                    useMaxperEnemy = data.useMaxperEnemy,
                    maximumPerEnemy = data.maximumPerEnemy,
                    spawnConditions = data.spawnConditions != null ? new List<EnemySpawnConditionSO>(data.spawnConditions) : null
                };
                cloned.InitRuntime();
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
        public void ReloadOptions(List<MapDataSO.EnemyOption> newOptions)
        {
            if (newOptions == null) return;
            _mapdata.EnemyOptions = new List<MapDataSO.EnemyOption>(newOptions);
            ReloadFromMap(_mapdata);
        }
        public void ReloadFromMap(MapDataSO newMap)
        {
            if (newMap == null) return;
            _mapdata = newMap;
            PrewarmEnemy();
        }
        public void UpgradePointRatio()
        {
            foreach (var data in _enemyOptionsList)
            {
                if (data.enemyPointCanGrowth)
                    data.EnemyPoint *= (1 + data.enemyPointGrowthRate / 100f);
            }
        }
        public void UpgradeEnemyChance()
        {
            float totalChanceBefore = 0;
            foreach (var data in _enemyOptionsList)
                totalChanceBefore += data.Chance;

            foreach (var data in _enemyOptionsList)
            {
                if (data.enemyChanceCanGrowth)
                    data.Chance += data.enemyChanceGrowthRate;
            }

            float totalChanceAfter = 0;
            foreach (var data in _enemyOptionsList)
                totalChanceAfter += data.Chance;

            foreach (var data in _enemyOptionsList)
                data.Chance = (data.Chance / totalChanceAfter) * 100f;

            if (!_debug) return; Debug.Log("---- Enemy Spawn Chance After Normalize ----");
            foreach (var data in _enemyOptionsList)
            {
                Debug.Log($"ID: {data.id} | Chance: {data.Chance:F2}%");
            }
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
                enemy.HealthSystem.ForceDead();
            }
        }
        
        #endregion
    }
}
