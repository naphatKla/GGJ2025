using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.SO;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace GameControl.Controller
{
    public class EnemyPatternController
    {
        private readonly MapDataSO _mapdata;
        private readonly SpawnerStateController _state;
        private readonly Vector2 _regionSize;
        private readonly List<MapDataSO.PatternOption> _patternEnemy;

        private Dictionary<string, ObjectPool<EnemyController>> _storeEnemy;
        private List<MapDataSO.EnemyOption> _storeOption;
        private readonly Queue<MapDataSO.PatternOption> _patternQueue = new();

        private readonly Queue<List<MapDataSO.PatternOption>> _batchQueue = new();
        private bool _isBatchProcessing = false;
        private float _minPerPatternSlot = 0f;
        private readonly bool _isDebug;
        private float _currentTriggertime;
        private CancellationTokenSource _cts;

        public EnemyPatternController(MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
            _patternEnemy = new List<MapDataSO.PatternOption>();
            _isDebug = debug;
            _currentTriggertime = mapData.playAllPatternIn;
        }

        public void SetEnemyList(Dictionary<string, ObjectPool<EnemyController>> enemyList, List<MapDataSO.EnemyOption> enemyOptions)
        {
            _storeEnemy = enemyList;
            _storeOption = enemyOptions;
        }
        
        public void TriggerAllPatterns()
        {
            if (_patternEnemy.Count == 0) return;
            _batchQueue.Enqueue(new List<MapDataSO.PatternOption>(_patternEnemy));

            if (!_isBatchProcessing) ProcessBatchQueue().Forget();
        }

        // Update trigger time externally when appropriate
        public void UpdateTriggerTime()
        {
            if (_mapdata.triggerTimeCanDecrease && _currentTriggertime > _mapdata.patternDecreaseMinimum)
            {
                _currentTriggertime -= _mapdata.patternDecreaseInterval;
                if (_isDebug) Debug.Log($"[EnemyPatternController] Trigger time decreased to {_currentTriggertime}");
            }
        }

        // Add a random pattern (keeps list of patterns to play)
        public void AddRandomPattern()
        {
            if (_mapdata.PatternOptions == null || _mapdata.PatternOptions.Count == 0) return;

            var availablePatterns = _mapdata.PatternOptions.Where(p => !_patternEnemy.Contains(p)).ToList();

            if (availablePatterns.Count == 0)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] No available new patterns to add.");
                return;
            }

            var randomIndex = Random.Range(0, availablePatterns.Count);
            var selectedPattern = availablePatterns[randomIndex];
            _patternEnemy.Add(selectedPattern);

            if (_isDebug)
                Debug.Log($"[EnemyPatternController] Added random pattern: '{selectedPattern.pattern.name}'. Total patterns now: {_patternEnemy.Count}");
        }

        //Random Enemy Type
        public MapDataSO.EnemyOption RandomType(MapDataSO.PatternOption patternOption)
        {
            if (_storeOption == null || _storeOption.Count == 0)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] _storeOption is empty/null. Make sure SetEnemyList was called.");
                return null;
            }

            if (patternOption == null || !patternOption.enableSpecificEnemy ||
                patternOption.specificEnemyList == null || patternOption.specificEnemyList.Count == 0)
            {
                var fallback = RandomUtility.GetWeightedRandom(_storeOption);
                if (_isDebug) Debug.Log($"[EnemyPatternController] Specific disabled -> fallback chosen: {(fallback != null ? fallback.EnemyId : "null")}");
                return fallback;
            }

            var dict = patternOption.specificEnemyList
                .Where(k => !string.IsNullOrWhiteSpace(k.enemyID) && k.chance > 0f)
                .GroupBy(k => k.enemyID.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.chance));

            if (_isDebug)
            {
                Debug.Log($"[EnemyPatternController] Specific keys: {string.Join(", ", dict.Keys)}");
                Debug.Log($"[EnemyPatternController] Store ids: {string.Join(", ", _storeOption.Select(o => (o.EnemyId ?? "").Trim().ToLowerInvariant()))}");
            }

            var chosen = RandomUtility.GetWeightedRandomById(_storeOption, dict,
                o => (o.EnemyId ?? string.Empty).Trim().ToLowerInvariant());
            if (_isDebug)
                Debug.Log($"[EnemyPatternController] Chosen (specific): {(chosen != null ? chosen.EnemyId : "null")}");

            return chosen;
        }

        #region Private Method

        private (MapDataSO.EnemyOption enemy, int amount, float usedPoints) ChooseEnemyAndCalculate(
            MapDataSO.PatternOption pattern)
        {
            var enemy = RandomType(pattern);
            if (enemy == null)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] RandomType returned null");
                return (null, 0, 0f);
            }

            var pointBudget = Mathf.Min(pattern.patternPoint, SpawnerStateController.Instance.CurrentEnemyPoint);

            if (enemy.EnemyPoint <= 0f)
            {
                if (_isDebug)
                    Debug.Log(
                        $"[EnemyPatternController] Enemy '{enemy.EnemyId}' has non-positive EnemyPoint ({enemy.EnemyPoint}).");
                return (enemy, 0, 0f);
            }

            var amount = Mathf.FloorToInt(pointBudget / enemy.EnemyPoint);
            var used = amount * enemy.EnemyPoint;

            if (_isDebug)
                Debug.Log($"[EnemyPatternController] Random chosen: '{enemy.EnemyId}' (costPer={enemy.EnemyPoint}). " +
                          $"Will spawn={amount}, " +
                          $"usePoints={used}, " +
                          $"budget={pointBudget}, " +
                          $"currentPoint={SpawnerStateController.Instance.CurrentEnemyPoint}");

            return (enemy, amount, used);
        }

        private async UniTask TriggerSinglePattern(MapDataSO.PatternOption patternData)
        {
            if (!CanTriggerPattern(patternData)) return;

            var (enemyType, enemyAmount, usedPoints) = ChooseEnemyAndCalculate(patternData);
            if (enemyType == null || enemyAmount <= 0) return;
            SpawnerStateController.Instance.CurrentEnemyPoint -= usedPoints;

            var rows = CalculatePatternRows(patternData, enemyAmount);
            await SpawnEnemyRows(rows, enemyType, patternData, enemyAmount);
        }

        private async UniTask<bool> WaitForEnoughEnemyPoint(
            MapDataSO.PatternOption pattern,
            float? customTimeoutSec = null,
            CancellationToken ct = default)
        {
            if (SpawnerStateController.Instance.CurrentEnemyPoint >= pattern.patternPoint) return true;

            float timeout = customTimeoutSec ?? (_currentTriggertime + 2f);
            float timer = 0f;

            if (_isDebug) Debug.Log($"[EnemyPatternController] Waiting up to {timeout:0.00}s for enough points to play '{pattern.pattern.name}'...");

            while (SpawnerStateController.Instance.CurrentEnemyPoint < pattern.patternPoint)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                timer += Time.deltaTime;
                
                if (timer >= timeout)
                {
                    if (_isDebug) Debug.Log($"[EnemyPatternController] Timeout after {timer:0.00}s waiting for '{pattern.pattern.name}'");
                    return false;
                }
            }

            if (_isDebug) Debug.Log($"[EnemyPatternController] Got enough points after {timer:0.00}s for '{pattern.pattern.name}'");
            return true;
        }
        
        private bool CanTriggerPattern(MapDataSO.PatternOption patternData)
        {
            return patternData.pattern != null &&
                   SpawnerStateController.Instance.CurrentEnemyPoint >= patternData.patternPoint;
        }

        private List<List<Vector2>> CalculatePatternRows(MapDataSO.PatternOption patternData, int enemyAmount)
        {
            if (patternData.enablePatternCenter)
            {
                var center = patternData.patternCenter;
                return patternData.pattern.CalculateRows(center, enemyAmount);
            }
            else
            {
                var center = PlayerController.Instance.transform.position;
                return patternData.pattern.CalculateRows(center, enemyAmount);
            }
        }

        private async UniTask SpawnEnemyRows(List<List<Vector2>> rows, MapDataSO.EnemyOption enemyType, MapDataSO.PatternOption patternData, int maxEnemyAmount)
        {
            var spawnedCount = 0;

            foreach (var row in rows)
            {
                foreach (var pos in row)
                {
                    if (spawnedCount >= maxEnemyAmount) return;

                    SpawnEnemy(enemyType, pos);
                    spawnedCount++;

                    if (patternData.DelayBetweenEnemy > 0)
                        await UniTask.Delay((int)(patternData.DelayBetweenEnemy * 1000));
                }

                if (patternData.DelayBetweenRows > 0)
                    await UniTask.Delay((int)(patternData.DelayBetweenRows * 1000));
            }
        }

        private void SpawnEnemy(MapDataSO.EnemyOption enemyType, Vector2 pos)
        {
            if (!_storeEnemy.TryGetValue(enemyType.id, out var pool)) return;

            var enemyObj = pool.Get();
            enemyObj.transform.position = pos;
            enemyObj.transform.SetParent(_state.EnemyParent);
        }

        private async UniTaskVoid ProcessBatchQueue()
        {
            if (_isBatchProcessing) return;
            _isBatchProcessing = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                while (_batchQueue.Count > 0)
                {
                    var batch = _batchQueue.Dequeue();
                    if (batch == null || batch.Count == 0) continue;

                    if (_isDebug) Debug.Log($"[EnemyPatternController] === Starting batch with {batch.Count} pattern(s) ===");

                    var perPatternTime = _currentTriggertime / batch.Count;

                    for (int i = 0; i < batch.Count; i++)
                    {
                        var pattern = batch[i];

                        bool enough = await WaitForEnoughEnemyPoint(pattern, null, _cts.Token);
                        if (!enough)
                        {
                            if (_isDebug) Debug.Log($"[EnemyPatternController] Timeout waiting for points for '{pattern.pattern.name}'");
                            continue;
                        }

                        await TriggerSinglePattern(pattern);
                        if (i < batch.Count - 1)
                        {
                            if (_isDebug) Debug.Log($"[EnemyPatternController] Waiting {perPatternTime:0.00}s before next pattern...");
                            await UniTask.Delay((int)(perPatternTime * 1000), cancellationToken: _cts.Token);
                        }
                    }

                    if (_isDebug)
                        Debug.Log("[EnemyPatternController] === Finished all patterns in this batch ===");
                }
            }
            catch (OperationCanceledException)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] Processing canceled.");
            }
            finally
            {
                _isBatchProcessing = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void StopProcessing()
        {
            _cts?.Cancel();
        }

        #endregion
    }
}