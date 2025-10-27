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
        public enum TriggerOutcomeKind
        {
            SpawnedSome,
            NoPoints,
            Skipped
        }

        private readonly MapDataSO _mapdata;
        private readonly SpawnerStateController _state;
        private readonly List<MapDataSO.PatternOption> _patternEnemy;

        private EnemySpawnerController _enemySpawner;
        private Dictionary<string, ObjectPool<EnemyController>> _storeEnemy;
        private readonly Queue<MapDataSO.PatternOption> _patternQueue = new();

        private readonly Queue<List<MapDataSO.PatternOption>> _batchQueue = new();
        private bool _isBatchProcessing;

        private readonly bool _isDebug;
        private float _currentTriggertime;
        
        private CancellationToken _externalCt = CancellationToken.None;
        private CancellationTokenSource _internalStopCts = new CancellationTokenSource();
        private CancellationTokenSource _linkedCts;
        private CancellationToken _ct = CancellationToken.None;

        public void BindCancellationToken(CancellationToken ct)
        {
            _externalCt = ct;
            RebuildLinkedToken();
        }
        
        private void RebuildLinkedToken()
        {
            _linkedCts?.Dispose();
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_externalCt, _internalStopCts.Token);
            _ct = _linkedCts.Token;
        }

        private int PatternMax => Mathf.Max(1, Mathf.RoundToInt(_mapdata.patternMax));
        private bool CanDuplicateAfterHaveAllPattern => _mapdata.canDuplicateAfterHaveAllPattern;

        public EnemyPatternController(MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _patternEnemy = new List<MapDataSO.PatternOption>();
            _isDebug = debug;
            _currentTriggertime = mapData.triggerAllPatternIn;
        }

        #region Public Method

        public void SetEnemySpawner(EnemySpawnerController spawner)
        {
            _enemySpawner = spawner;
            _storeEnemy = spawner.GetEnemyListFiltered(_mapdata);
        }

        public void ReloadPatterns(List<MapDataSO.PatternOption> newPatterns)
        {
            StopProcessing();
            if (newPatterns != null) _mapdata.PatternOptions = newPatterns;
            _currentTriggertime = Mathf.Max(0.01f, _mapdata.triggerAllPatternIn);
            _batchQueue.Clear();

            var targetCount = Mathf.Min(PatternMax, _patternEnemy.Count);

            _patternEnemy.Clear();

            if (_enemySpawner != null) SetEnemySpawner(_enemySpawner);

            AddRandomPatternsForce(targetCount);
            if (_isDebug) Debug.Log($"[EnemyPatternController] Reloaded patterns and restored count to {targetCount}/{PatternMax}");
        }

        private void AddRandomPatternsForce(int count)
        {
            for (var i = 0; i < count; i++)
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
        
        public void TriggerAllPatterns()
        {
            if (_patternEnemy.Count == 0) return;
            _batchQueue.Enqueue(new List<MapDataSO.PatternOption>(_patternEnemy));
            if (!_isBatchProcessing) ProcessBatchQueue().Forget();
        }

        public void AddRandomPatterns(int count)
        {
            for (var i = 0; i < count; i++) AddRandomPattern();
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
                    if (_isDebug)
                        Debug.Log(
                            "[EnemyPatternController] No available patterns to add (duplicates disabled or none enabled).");
                    return;
                }
            }

            _patternEnemy.Add(selectedPattern);
            if (_isDebug)
                Debug.Log(
                    $"[EnemyPatternController] Added pattern: '{selectedPattern.pattern.name}'. Total={_patternEnemy.Count}/{PatternMax}");
        }

        //Random Enemy Type
        public MapDataSO.EnemyOption RandomType(MapDataSO.PatternOption patternOption)
        {
            if (_enemySpawner == null)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] EnemySpawner not set.");
                return null;
            }

            var candidates = _enemySpawner.PickEnemy(patternOption != null && patternOption.bypassSpawnCondition);
            if (candidates == null || candidates.Count == 0)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] No candidates after ConditionEnemy()");
                return null;
            }

            if (patternOption == null || !patternOption.enableSpecificEnemy ||
                patternOption.specificEnemyList == null || patternOption.specificEnemyList.Count == 0)
                return RandomUtility.GetWeightedRandom(candidates);

            var dict = patternOption.specificEnemyList
                .Where(k => !string.IsNullOrWhiteSpace(k.enemyID) && k.chance > 0f)
                .GroupBy(k => k.enemyID.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.chance));

            return RandomUtility.GetWeightedRandomById(candidates, dict,
                o => (o.EnemyId ?? string.Empty).Trim().ToLowerInvariant());
        }

        #endregion

        #region Private Method

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

        private TriggerOutcomeKind TriggerSinglePattern(MapDataSO.PatternOption patternData)
        {
            if (!CanTriggerPattern(patternData)) return TriggerOutcomeKind.Skipped;

            var (enemyType, enemyAmountPlan, usedPointsPlan) = ChooseEnemyAndCalculate(patternData);
            if (enemyType == null || enemyAmountPlan <= 0)
            {
                if (_isDebug) Debug.Log($"[Pattern] '{patternData.pattern.name}' skip: no enemy or planAmount=0");
                return TriggerOutcomeKind.Skipped;
            }

            var costPer = enemyType.EnemyPoint;
            var points = SpawnerStateController.Instance.EnemyPoint;

            if (points < costPer)
            {
                if (_isDebug) Debug.Log($"[Pattern] '{patternData.pattern.name}' no min points. need≥{costPer:0.##}, have={points:0.##}");
                return TriggerOutcomeKind.NoPoints;
            }

            var maxByPoints = Mathf.FloorToInt(points / costPer);
            var finalAmount = Mathf.Min(enemyAmountPlan, maxByPoints);
            if (finalAmount <= 0) return TriggerOutcomeKind.NoPoints;

            var used = finalAmount * costPer;
            SpawnerStateController.Instance.CurrentEnemyPoint = points - used;

            if (_isDebug)
            {
                var partial = finalAmount < enemyAmountPlan ? "PARTIAL" : "FULL";
                Debug.Log($"[Pattern] '{patternData.pattern.name}' {partial} spawn " +
                          $"{finalAmount}/{enemyAmountPlan}  costPer={costPer:0.##}  used={used:0.##}  left={points - used:0.##}");
            }

            var rows = CalculatePatternRows(patternData, finalAmount);
            _ = SpawnEnemyRows(rows, enemyType, patternData, finalAmount)
                .AttachExternalCancellation(_ct);
            return TriggerOutcomeKind.SpawnedSome;
        }


        private bool CanTriggerPattern(MapDataSO.PatternOption patternData)
        {
            return patternData.pattern != null;
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

        private async UniTask SpawnEnemyRows(List<List<Vector2>> rows, MapDataSO.EnemyOption enemyType,
            MapDataSO.PatternOption patternData, int maxEnemyAmount)
        {
            var spawnedCount = 0;

            foreach (var row in rows)
            {
                _ct.ThrowIfCancellationRequested();
                foreach (var pos in row)
                {
                    _ct.ThrowIfCancellationRequested();
                    if (spawnedCount >= maxEnemyAmount) return;

                    SpawnEnemy(enemyType, patternData, pos);
                    spawnedCount++;

                    if (patternData.DelayBetweenEnemy > 0)
                        await UniTask.Delay((int)(patternData.DelayBetweenEnemy * 1000),
                            DelayType.DeltaTime, PlayerLoopTiming.Update, _ct);
                }

                if (patternData.DelayBetweenRows > 0)
                    await UniTask.Delay((int)(patternData.DelayBetweenRows * 1000),
                        DelayType.DeltaTime, PlayerLoopTiming.Update, _ct);
            }
        }

        private void SpawnEnemy(MapDataSO.EnemyOption enemyType, MapDataSO.PatternOption patternData, Vector2 pos)
        {
            if (!_storeEnemy.TryGetValue(enemyType.id, out var pool)) return;

            var enemyObj = pool.Get();
            enemyObj.transform.position = pos;
            enemyObj.CountedByMax = false;
            enemyObj.transform.SetParent(_state.EnemyParent);
            StopEnemyMovement(enemyObj, patternData.enableMovementAfter, _ct).Forget();
        }

        private async UniTask StopEnemyMovement(EnemyController enemy, float time,
            CancellationToken token = default)
        {
            if (enemy == null) return;

            var input = enemy.InputSystem;
            var move = enemy.MovementSystem;
            time = Mathf.Max(0f, time);
            var prevEnable = input != null && input.Enable;

            try
            {
                if (input != null) input.Enable = false;
                move?.StopAllMovementAndTween();

                await UniTask.Delay(TimeSpan.FromSeconds(time),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (enemy != null)
                {
                    if (input != null) input.Enable = prevEnable;
                    move?.ResetMovementSystem();
                }
            }
        }

        private MapDataSO.PatternOption PickRandomEnabledPattern()
        {
            var enabled = GetEnabledPatterns();
            if (enabled == null || enabled.Count == 0) return null;
            return enabled[Random.Range(0, enabled.Count)];
        }

        private async UniTaskVoid ProcessBatchQueue()
        {
            if (_isBatchProcessing) return;
            _isBatchProcessing = true;

            try
            {
                while (_batchQueue.Count > 0)
                {
                    _ct.ThrowIfCancellationRequested();

                    var batch = _batchQueue.Dequeue();
                    if (batch == null || batch.Count == 0) continue;
                    if (_isDebug)
                        Debug.Log($"[EnemyPatternController] === Starting batch with {batch.Count} pattern(s) ===");

                    var targetSuccess = batch.Count;
                    var successCount = 0;
                    var work = new List<MapDataSO.PatternOption>(batch);

                    var perPatternTime = _currentTriggertime / Mathf.Max(1, targetSuccess);
                    var nextAt = Time.deltaTime;

                    var maxAttempts = targetSuccess + 8;
                    for (var i = 0; successCount < targetSuccess && i < work.Count && i < maxAttempts; i++)
                    {
                        _ct.ThrowIfCancellationRequested();

                        var pattern = work[i];
                        var outcome = TriggerSinglePattern(pattern);

                        if (outcome == TriggerOutcomeKind.SpawnedSome)
                        {
                            successCount++;
                        }
                        else if (outcome == TriggerOutcomeKind.NoPoints)
                        {
                            var replacement = PickRandomEnabledPattern();
                            if (replacement != null) work.Add(replacement);
                        }

                        nextAt += perPatternTime;
                        var wait = Mathf.Max(0f, nextAt - Time.deltaTime);
                        if (_isDebug) Debug.Log($"[EnemyPatternController] Waiting {wait:0.00}s");
                        if (wait > 0f)
                            await UniTask.Delay((int)(wait * 1000),
                                DelayType.DeltaTime, PlayerLoopTiming.Update, _ct);
                    }

                    if (_isDebug)
                        Debug.Log(
                            $"[EnemyPatternController] === Finished batch success={successCount}/{targetSuccess} tried={work.Count} ===");
                }
            }
            catch (OperationCanceledException)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] Processing canceled.");
            }
            finally
            {
                _isBatchProcessing = false;
            }
        }


        public void StopProcessing()
        {
            if (!_internalStopCts.IsCancellationRequested)
                _internalStopCts.Cancel();
            _patternQueue.Clear();
            _batchQueue.Clear();
            _isBatchProcessing = false;
            _internalStopCts.Dispose();
            _internalStopCts = new CancellationTokenSource();
            RebuildLinkedToken();
        }


        #endregion
    }
}