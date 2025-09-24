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
        
        private EnemySpawnerController _enemySpawner;
        private Dictionary<string, ObjectPool<EnemyController>> _storeEnemy;
        private List<MapDataSO.EnemyOption> _storeOption;
        private readonly Queue<MapDataSO.PatternOption> _patternQueue = new();

        private readonly Queue<List<MapDataSO.PatternOption>> _batchQueue = new();
        private bool _isBatchProcessing = false;
        private float _minPerPatternSlot = 0f;
        private readonly bool _isDebug;
        private float _currentTriggertime;
        private CancellationTokenSource _cts;
        private int PatternMax => Mathf.Max(1, Mathf.RoundToInt(_mapdata.patternMax));
        private bool CanDuplicateAfterHaveAllPattern => _mapdata.canDuplicateAfterHaveAllPattern;

        public EnemyPatternController(MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
            _patternEnemy = new List<MapDataSO.PatternOption>();
            _isDebug = debug;
            _currentTriggertime = mapData.triggerAllPatternIn;
        }
        
        #region Public Method

        public void SetEnemySpawner(EnemySpawnerController spawner)
        {
            _enemySpawner = spawner;
            _storeEnemy = spawner.GetEnemyList();
            _storeOption = spawner.GetEnemyOption();
        }

        public void ReloadPatterns(List<MapDataSO.PatternOption> newPatterns)
        {
            StopProcessing();
            if (newPatterns != null) _mapdata.PatternOptions = newPatterns;
            _currentTriggertime = Mathf.Max(0.01f, _mapdata.triggerAllPatternIn);
            _batchQueue.Clear();
            _minPerPatternSlot = 0f;
            
            int targetCount = Mathf.Min(PatternMax, _patternEnemy.Count);

            _patternEnemy.Clear();

            if (_enemySpawner != null)
            {
                _storeEnemy = _enemySpawner.GetEnemyList();
                _storeOption = _enemySpawner.GetEnemyOption();
            }

            AddRandomPatternsForce(targetCount);
            RebindSpawner(_enemySpawner);
            if (_isDebug) Debug.Log($"[EnemyPatternController] Reloaded patterns and restored count to {targetCount}/{PatternMax}");
        }
        
        public void RebindSpawner(EnemySpawnerController spawner)
        {
            SetEnemySpawner(spawner);
            _storeEnemy = spawner.GetEnemyList();
            _storeOption = spawner.GetEnemyOption();
        }
        
        private void AddRandomPatternsForce(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var enabled = GetEnabledPatterns();
                if (enabled == null || enabled.Count == 0) break;

                EnsurePatternCapacity();
                var notIn = enabled.Where(p => !_patternEnemy.Contains(p)).ToList();
                MapDataSO.PatternOption pick = null;

                if (notIn.Count > 0)
                    pick = notIn[Random.Range(0, notIn.Count)];
                else
                    pick = enabled[Random.Range(0, enabled.Count)];
                _patternEnemy.Add(pick);
            }
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
        
        public void AddRandomPatterns(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AddRandomPattern();
            }
        }

        public void AddRandomPattern()
        {
            if (_mapdata.PatternOptions == null || _mapdata.PatternOptions.Count == 0) return;
            EnsurePatternCapacity();

            var enabledPatterns = GetEnabledPatterns();
            var availablePatterns = enabledPatterns
                .Where(p => !_patternEnemy.Contains(p))
                .ToList();

            MapDataSO.PatternOption selectedPattern = null;

            if (availablePatterns.Count > 0)
            {
                var idx = Random.Range(0, availablePatterns.Count);
                selectedPattern = availablePatterns[idx];
            }
            else
            {
                if (CanDuplicateAfterHaveAllPattern && enabledPatterns.Count > 0)
                {
                    var idx = Random.Range(0, enabledPatterns.Count);
                    selectedPattern = enabledPatterns[idx];
                }
                else
                {
                    if (_isDebug) Debug.Log("[EnemyPatternController] No available patterns to add (duplicates disabled or none enabled).");
                    return;
                }
            }

            _patternEnemy.Add(selectedPattern);
            if (_isDebug) Debug.Log($"[EnemyPatternController] Added pattern: '{selectedPattern.pattern.name}'. Total={_patternEnemy.Count}/{PatternMax}");
        }

        //Random Enemy Type
        public MapDataSO.EnemyOption RandomType(MapDataSO.PatternOption patternOption)
        {
            if (_enemySpawner == null)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] EnemySpawner not set.");
                return null;
            }
            
            var candidates = _enemySpawner.ConditionEnemy(patternOption != null && patternOption.bypassSpawnCondition);
            if (candidates == null || candidates.Count == 0)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] No candidates after ConditionEnemy()");
                return null;
            }

            if (patternOption == null || !patternOption.enableSpecificEnemy ||
                patternOption.specificEnemyList == null || patternOption.specificEnemyList.Count == 0)
            {
                return RandomUtility.GetWeightedRandom(candidates);
            }

            var dict = patternOption.specificEnemyList
                .Where(k => !string.IsNullOrWhiteSpace(k.enemyID) && k.chance > 0f)
                .GroupBy(k => k.enemyID.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.chance));

            return RandomUtility.GetWeightedRandomById(candidates, dict,
                o => (o.EnemyId ?? string.Empty).Trim().ToLowerInvariant());
        }
        #endregion

        #region Private Method

        public void TriggerAllPatterns()
        {
            if (_patternEnemy.Count == 0) return;
            _batchQueue.Enqueue(new List<MapDataSO.PatternOption>(_patternEnemy));

            if (!_isBatchProcessing) ProcessBatchQueue().Forget();
        }
        
        private List<MapDataSO.PatternOption> GetEnabledPatterns()
        {
            return _mapdata.PatternOptions?
                .Where(p => p != null && p.enableThisPattern)
                .ToList() ?? new List<MapDataSO.PatternOption>();
        }

        private void EnsurePatternCapacity()
        {
            if (_patternEnemy.Count >= PatternMax)
                _patternEnemy.RemoveAt(0);
        }
        
        private (MapDataSO.EnemyOption enemy, int amount, float usedPoints) ChooseEnemyAndCalculate(MapDataSO.PatternOption pattern)
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
                if (_isDebug) Debug.Log($"[EnemyPatternController] Enemy '{enemy.EnemyId}' has non-positive EnemyPoint ({enemy.EnemyPoint}).");
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

        private async UniTask WaitUntilEnoughEnemyPoint(MapDataSO.PatternOption pattern, CancellationToken ct = default)
        {
            if (_isDebug)
                Debug.Log($"[EnemyPatternController] Waiting until enough points for '{pattern.pattern.name}'...");

            if (SpawnerStateController.Instance.CurrentEnemyPoint >= pattern.patternPoint)
                return;

            while (SpawnerStateController.Instance.CurrentEnemyPoint < pattern.patternPoint)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (_isDebug)
                Debug.Log($"[EnemyPatternController] Got enough points for '{pattern.pattern.name}'");
        }
        
        private bool CanTriggerPattern(MapDataSO.PatternOption patternData)
        {
            return patternData.pattern != null && SpawnerStateController.Instance.CurrentEnemyPoint >= patternData.patternPoint;
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

                    SpawnEnemy(enemyType,patternData, pos);
                    spawnedCount++;

                    if (patternData.DelayBetweenEnemy > 0)
                        await UniTask.Delay((int)(patternData.DelayBetweenEnemy * 1000));
                }

                if (patternData.DelayBetweenRows > 0)
                    await UniTask.Delay((int)(patternData.DelayBetweenRows * 1000));
            }
        }

        private void SpawnEnemy(MapDataSO.EnemyOption enemyType,MapDataSO.PatternOption patternData, Vector2 pos)
        {
            if (!_storeEnemy.TryGetValue(enemyType.id, out var pool)) return;

            var enemyObj = pool.Get();
            enemyObj.transform.position = pos;
            enemyObj.transform.SetParent(_state.EnemyParent);
            StopEnemyMovement(enemyObj,patternData.enableMovementAfter).Forget();
        }
        
        private async UniTaskVoid StopEnemyMovement(EnemyController enemy,float time)
        {
            try
            {
                enemy.InputSystem.Enable = false;
                enemy.MovementSystem.StopAllMovementAndTween();
                await UniTask.Delay((int)(time * 1000));
                enemy.InputSystem.Enable = true;
                enemy.MovementSystem.ResetMovementSystem();
            }
            catch (Exception a)
            {
                Console.WriteLine(a);
                throw;
            }
            finally
            {
                enemy.MovementSystem.ResetMovementSystem();
                enemy.InputSystem.Enable = true;
            }
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

                        await WaitUntilEnoughEnemyPoint(pattern, _cts.Token);
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