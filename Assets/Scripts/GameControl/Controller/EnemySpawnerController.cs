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
        private SpawnOverride? _nextSpawnOverride;
        
        public event Action<EnemyController, MapDataSO.EnemyOption> OnFirstSpawned;
        private readonly HashSet<string> _firstSpawnedTypeIds = new();

        /// <summary>
        /// Enemies spawned by the spawn schedule. They never cost Enemy Point, so they must not refund it on
        /// death either - otherwise every scheduled kill would inflate the random spawner's budget.
        /// </summary>
        private readonly HashSet<EnemyController> _noPointRefund = new();

        /// <summary>
        /// Extra filter for the RANDOM spawner only (<see cref="SpawnEnemy"/>), set by the spawn schedule's
        /// Random Spawner Pool. Null = no filter (Conditions mode - behaviour unchanged). Patterns never use it.
        /// </summary>
        public Func<MapDataSO.EnemyOption, bool> RandomPoolFilter { get; set; }

        /// <summary>
        /// Extra filter for Enemy Patterns (<see cref="EnemyPatternController.RandomType"/>, also used by
        /// Wornhole), set by the spawn schedule's Pattern Enemy Pool. Null = no filter (behaviour unchanged).
        /// </summary>
        public Func<MapDataSO.EnemyOption, bool> PatternPoolFilter { get; set; }

        /// <summary>Which schedule rule spawned each live scheduled enemy - used for a rule's Max Alive.</summary>
        private readonly Dictionary<EnemyController, object> _scheduledOwner = new();

        /// <summary>Scheduled spawns still waiting on their Spawn Effects (e.g. DelaySpawn), per rule.</summary>
        private readonly Dictionary<object, int> _pendingScheduled = new();

        /// <summary>Live enemies spawned by this schedule rule, plus ones still waiting on Spawn Effects.</summary>
        public int CountScheduledAlive(object owner)
        {
            if (owner == null) return 0;
            int count = _pendingScheduled.TryGetValue(owner, out int pending) ? pending : 0;
            foreach (var kv in _scheduledOwner)
            {
                if (kv.Value != owner) continue;
                var enemy = kv.Key;
                if (enemy && enemy.gameObject.activeInHierarchy && !enemy.HealthSystem.IsDead) count++;
            }
            return count;
        }
        public int EnemyAmount
        {
            get
            {
                _activeEnemy.RemoveAll(e => e == null || e.gameObject == null);
                return _activeEnemy.Count(e => e.gameObject.activeInHierarchy);
            }
        }


        private CancellationToken _externalCt = CancellationToken.None;

        private struct SpawnOverride
        {
            public Vector2 Position;
            public bool ShowTrail;
        }
        
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
            var obj = Object.Instantiate(option.EnemyObject, _state.EnemyParent);
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
                    obj.transform.position = SpawnUtility.RandomSpawnAroundPlayerCamera(_mainCamera, 10f);
                }

                _scheduledOwner.Remove(controller);
                bool noRefund = _noPointRefund.Remove(controller);
                if (controller.CountedByMax && !noRefund)
                    SpawnerStateController.Instance.CurrentEnemyPoint += option.EnemyPoint;

                controller.CountedByMax = false;
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
            _noPointRefund.Remove(obj); // pooled instance may have been a scheduled spawn last time
            _scheduledOwner.Remove(obj);
            
            bool firstOfType = _firstSpawnedTypeIds.Add(option.id);
            if (firstOfType)
            {
                OnFirstEnemySpawn(obj, option);
            }

            var spawnOverride = _nextSpawnOverride;
            obj.transform.position = spawnOverride.HasValue
                ? spawnOverride.Value.Position
                : SpawnUtility.RandomBetweenMouseAndCamera(_mainCamera);
            obj.ResetAllDependentBehavior();
            obj.FeedbackSystem.ShowTrail(spawnOverride.HasValue ? spawnOverride.Value.ShowTrail : true);
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
            var candidates = PickEnemy(false);
            if (RandomPoolFilter != null && candidates != null)
            {
                candidates = candidates.Where(RandomPoolFilter).ToList();
                if (candidates.Count == 0) candidates = null;
            }

            var randomEnemy = RandomUtility.GetWeightedRandom(candidates);
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

        public bool TrySpawnEnemyFromPool(string enemyId, Vector2 position, out EnemyController enemy,
            bool countTowardPerEnemyMax = false, bool showTrail = true, bool requirePrewarmedInactive = true)
        {
            enemy = null;
            if (!CanSpawnEnemyFromPool(enemyId, countTowardPerEnemyMax, requirePrewarmedInactive))
                return false;

            var option = _enemyOptionsList.First(e => e.id == enemyId);
            var pool = _enemyPools[option.id];

            if (countTowardPerEnemyMax)
            {
                option.activeCount++;
            }

            _nextSpawnOverride = new SpawnOverride
            {
                Position = position,
                ShowTrail = showTrail
            };

            try
            {
                enemy = pool.Get();
            }
            finally
            {
                _nextSpawnOverride = null;
            }

            enemy.CountedByMax = countTowardPerEnemyMax;
            enemy.transform.position = position;
            enemy.FeedbackSystem.ShowTrail(showTrail);
            return true;
        }

        public bool CanSpawnEnemyFromPool(string enemyId, bool countTowardPerEnemyMax = false,
            bool requirePrewarmedInactive = true)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return false;
            if (_enemyOptionsList == null || _enemyPools == null) return false;

            var option = _enemyOptionsList.FirstOrDefault(e => e.id == enemyId);
            if (option == null) return false;
            if (!_enemyPools.TryGetValue(option.id, out var pool)) return false;
            if (requirePrewarmedInactive && pool.CountInactive <= 0) return false;

            return !countTowardPerEnemyMax || ConditionCheck(option);
        }

        public bool HasEnemyOption(string enemyId) =>
            !string.IsNullOrWhiteSpace(enemyId) && _enemyOptionsList != null && _enemyOptionsList.Any(e => e.id == enemyId);

        /// <summary>
        /// Spawns one enemy for the spawn schedule. Same pool / ActionOnGet path as a random spawn and it
        /// counts toward Per-Enemy Max, but costs no Enemy Point (and refunds none when it dies).
        /// </summary>
        /// <param name="position">Null = the random spawner's usual position.</param>
        /// <param name="failReason">Why nothing spawned (for the schedule's debug output); null on success.</param>
        /// <param name="owner">The schedule rule asking - recorded so its Max Alive can be counted.</param>
        public bool TrySpawnScheduled(string enemyId, bool respectPerEnemyMax, bool checkSpawnConditions,
            bool playSpawnEffects, Vector2? position, object owner, out string failReason)
        {
            failReason = null;
            if (!HasEnemyOption(enemyId)) { failReason = "not in Enemy Options"; return false; }
            var option = _enemyOptionsList.First(e => e.id == enemyId);
            if (!_enemyPools.TryGetValue(option.id, out var pool)) { failReason = "no pool"; return false; }

            if (respectPerEnemyMax && !ConditionCheck(option))
            {
                failReason = $"Per-Enemy Max full ({option.activeCount}/{Mathf.RoundToInt(option.maximumPerEnemy)} alive)";
                return false;
            }
            if (checkSpawnConditions && !option.IsSpawnable(_state, _mapdata))
            {
                failReason = "Spawn Conditions not met";
                return false;
            }

            option.activeCount++;

            if (playSpawnEffects && option.useSpawnEffect && option.spawnEffect != null && option.spawnEffect.Count > 0)
                SpawnScheduledAfterEffectsAsync(option, pool, position, owner).Forget();
            else
                GetScheduled(pool, position, owner);

            return true;
        }

        private async UniTaskVoid SpawnScheduledAfterEffectsAsync(MapDataSO.EnemyOption option,
            ObjectPool<EnemyController> pool, Vector2? position, object owner)
        {
            AddPending(owner, 1);
            try
            {
                await SpawnEffect(option);
            }
            catch (OperationCanceledException)
            {
                option.activeCount = Mathf.Max(0, option.activeCount - 1);
                AddPending(owner, -1);
                return;
            }

            AddPending(owner, -1);
            GetScheduled(pool, position, owner);
        }

        private void AddPending(object owner, int delta)
        {
            if (owner == null) return;
            _pendingScheduled.TryGetValue(owner, out int n);
            n += delta;
            if (n <= 0) _pendingScheduled.Remove(owner);
            else _pendingScheduled[owner] = n;
        }

        private void GetScheduled(ObjectPool<EnemyController> pool, Vector2? position, object owner)
        {
            if (position.HasValue)
                _nextSpawnOverride = new SpawnOverride { Position = position.Value, ShowTrail = true };

            EnemyController inst;
            try
            {
                inst = pool.Get();
            }
            finally
            {
                _nextSpawnOverride = null;
            }

            inst.CountedByMax = true; // so Release decrements activeCount like a random spawn
            _noPointRefund.Add(inst);
            if (owner != null) _scheduledOwner[inst] = owner;
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
