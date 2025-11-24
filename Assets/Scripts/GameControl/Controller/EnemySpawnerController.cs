using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.SO;
using UI;
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
        public int EnemyAmount
        {
            get
            {
                _activeEnemy.RemoveAll(e => e == null || e.gameObject == null);
                return _activeEnemy.Count(e => e.gameObject.activeInHierarchy);
            }
        }


        private CancellationToken _externalCt = CancellationToken.None;
        
        public void BindCancellationToken(CancellationToken ct)
        {
            _externalCt = ct;
        }
        
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
            
            controller.HealthSystem.OnDead += () =>
            {
                if (controller.CharacterData is EnemyDataSo enemyData)
                {
                    int totalExp = Mathf.CeilToInt(enemyData.ExpDrop * _anergyDropMultiplier);
                    _state.ItemSpawnerController.SpawnExpItem(totalExp, obj.transform.position);
                }
                SpawnerStateController.Instance.CurrentEnemyPoint += option.EnemyPoint;
            };
            
            return controller;
        }
        
        public void ActionOnDestroy(EnemyController obj, MapDataSO.EnemyOption option)
        {
            Object.Destroy(obj.gameObject);
            _activeEnemy.Remove(obj);
        }

        public void ActionOnRelease(EnemyController obj, MapDataSO.EnemyOption option)
        {
            DOTween.Kill(obj.transform, complete: true);
            
            if (obj.CountedByMax)
            {
                option.activeCount = Mathf.Max(0, option.activeCount - 1);
            }
            
            obj.gameObject.SetActive(false);
            obj.FeedbackSystem.ShowTrail(false);
            obj.transform.position = SpawnUtility.RandomSpawnAroundPlayerCamera(_mainCamera, 10f);
            _activeEnemy.Remove(obj);
        }

        public void SetAnergyDropMultiplier(float value)
        {
            _anergyDropMultiplier = Mathf.Max(0, value);
        }
        
        private void OnFirstEnemySpawn(EnemyController obj, MapDataSO.EnemyOption option)
        {
            if (GameStateController.Instance.MapState == MapState.Rush) return;
            if (!option.disableEnemyDetect) 
                NotificationManager.Instance.PlayNotification("notify_enemy", $"NEW ENEMY DETECT - {option.displayName}", 4f,NotificationType.Normal, option.displayName);
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
        
        public Dictionary<string, ObjectPool<EnemyController>> GetEnemyListFiltered(MapDataSO map)
        {
            return _enemyPools
                .Where(kv =>
                {
                    var opt = map.EnemyOptions.Find(o => o.id == kv.Key);
                    return opt == null || !opt.disableInPattern;
                })
                .ToDictionary(kv => kv.Key, kv => kv.Value);
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

        public async UniTask SpawnEffect(MapDataSO.EnemyOption option)
        {
            foreach (var effect in option.spawnEffect)
            {
                switch (effect.effectType)
                {
                    case MapDataSO.EnemyOption.SpawnEffectType.ShowPopup:
                        PopupUIManager.Instance.ShowPopup(effect.effectString, 2.0f, bypassStack: true);
                        break;
                    case MapDataSO.EnemyOption.SpawnEffectType.KillAllEnemy:
                        ClearAllEnemys();
                        break;
                    case MapDataSO.EnemyOption.SpawnEffectType.StopEnemySpawn:
                        SpawnerStateController.Instance.StopSpawning();
                        break;
                    case MapDataSO.EnemyOption.SpawnEffectType.DelaySpawn:
                        await UniTask.Delay(TimeSpan.FromSeconds(effect.effectint), DelayType.DeltaTime, cancellationToken: _externalCt);
                        break;
                }
            }
        }
        
        public MapDataSO.EnemyOption SpawnEnemy()
        {
            var randomEnemy = RandomUtility.GetWeightedRandom(PickEnemy(false));
            if (randomEnemy == null) return null;
            SpawnerStateController.Instance.CurrentEnemyPoint -= randomEnemy.EnemyPoint;
            SpawnDelayAsync(randomEnemy).Forget();
            return randomEnemy;
        }

        public async UniTask SpawnDelayAsync(MapDataSO.EnemyOption randomEnemy)
        {
            if (!ConditionCheck(randomEnemy)) return;
            randomEnemy.activeCount++;
            if (randomEnemy.useSpawnEffect)
                await SpawnEffect(randomEnemy);
            
            if (!_enemyPools.TryGetValue(randomEnemy.id, out var pool)) return;
            var inst = pool.Get();
            inst.CountedByMax = true;
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
                    disableNormalize = data.disableNormalize,
                    modifyNewData = data.modifyNewData,
                    disableInPattern = data.disableInPattern,
                    disableEnemyDetect = data.disableEnemyDetect,
                    enemyData = data.enemyData,
                    useSpawnConditions = data.useSpawnConditions,
                    conditionLogic = data.conditionLogic,
                    useMaxperEnemy = data.useMaxperEnemy,
                    maximumPerEnemy = data.maximumPerEnemy,
                    spawnConditions = data.spawnConditions != null ? new List<EnemySpawnConditionSO>(data.spawnConditions) : null,
                    useSpawnEffect = data.useSpawnEffect,
                    spawnEffect = data.spawnEffect != null ? new List<MapDataSO.EnemyOption.SpawnEffectStruct>(data.spawnEffect) : null,
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

                for (int i = 0; i < data.prewarmCount; i++)
                {
                    var objPrewarm = CreateFunc(cloned);
                    _enemyPools[cloned.id].Release(objPrewarm);
                }
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
            foreach (var e in _enemyOptionsList)
                if (e.enemyChanceCanGrowth)
                    e.Chance += e.enemyChanceGrowthRate;

            NormalizeChancesRespectingFlags(_enemyOptionsList, _debug);
        }
        
        private static void NormalizeChancesRespectingFlags(List<MapDataSO.EnemyOption> options, bool debugLog = false)
        {
            foreach (var e in options) e.Chance = Mathf.Max(0f, e.Chance);

            float fixedSum = 0f;
            float varSum   = 0f;

            foreach (var e in options)
            {
                if (e.disableNormalize) fixedSum += e.Chance;
                else                    varSum   += e.Chance;
            }

            float remaining = Mathf.Max(0f, 100f - fixedSum);
            if (varSum <= 0f)
            {
                if (debugLog) Debug.Log($"[Normalize] No variable entries. fixedSum={fixedSum:F2}. Keep as-is.");
                return;
            }
            
            if (remaining <= 0f)
            {
                if (debugLog) Debug.Log($"[Normalize] fixedSum >= 100 → skip scaling variables (soft). Keep as-is.");
                return;
            }
            
            foreach (var e in options)
            {
                if (e.disableNormalize) continue;
                e.Chance = (e.Chance / varSum) * remaining;
            }

            if (debugLog)
            {
                float sum = 0f;
                foreach (var e in options) sum += e.Chance;
                Debug.Log($"[Normalize] fixed={fixedSum:F2}, varRem={remaining:F2}, total={sum:F2})");
            }
        }

        public void ClearAllEnemysCompletely()
        {
            ReleaseAllEnemies();
            ClearAllEnemys();
        }
        public void ClearAllEnemys()
        {
            foreach (var pool in _enemyPools.Values) pool.Clear();
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
