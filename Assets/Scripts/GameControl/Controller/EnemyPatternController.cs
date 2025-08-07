using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.SO;
using UnityEngine;
using UnityEngine.Pool;

namespace GameControl.Controller
{
    public class EnemyPatternController
    {
        private readonly MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _regionSize;
        private readonly List<MapDataSO.PatternOption> _patternEnemy;
        private Dictionary<string, ObjectPool<EnemyController>> _storeEnemy;
        private List<MapDataSO.EnemyOption> _storeOption;
        private readonly bool _isDebug;
        private float _currentTriggertime;

        public EnemyPatternController(MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
            _patternEnemy = new List<MapDataSO.PatternOption>();
            _isDebug = debug;
            _currentTriggertime = mapData.triggerTime;
        }

        public void SetEnemyList(Dictionary<string, ObjectPool<EnemyController>> enemyList, List<MapDataSO.EnemyOption> enemyOptions)
        {
            _storeEnemy = enemyList;
            _storeOption = enemyOptions;
        }

        private async UniTask WaitForEnoughEnemyPoint(MapDataSO.PatternOption pattern)
        {
            while (SpawnerStateController.Instance.CurrentEnemyPoint < pattern.patternPoint)
            {
                if (_isDebug) Debug.Log($"[EnemyPatternController] Waiting for enough CurrentEnemyPoint to play pattern '{pattern.pattern.name}'. Current: {SpawnerStateController.Instance.CurrentEnemyPoint}, Required: {pattern.patternPoint}");
                await UniTask.Delay(500);
            }
        }

        private async UniTask<float> PlayPatternWithTiming(int index, MapDataSO.PatternOption pattern, float interval)
        {
            if (_isDebug) Debug.Log($"[EnemyPatternController] Start playing pattern {index + 1}: '{pattern.pattern.name}'");
            var startTime = Time.time;

            await TriggerSinglePattern(pattern);

            var elapsed = Time.time - startTime;
            if (_isDebug) Debug.Log($"[EnemyPatternController] Finished pattern '{pattern.pattern.name}' in {elapsed:F2} seconds");

            var remaining = interval - elapsed;
            if (remaining > 0)
            {
                if (_isDebug) Debug.Log($"[EnemyPatternController] Waiting {remaining:F2} seconds before next pattern...");
                await UniTask.Delay((int)(remaining * 1000));
            }

            return elapsed + Mathf.Max(remaining, 0);
        }

        public void UpdateTriggerTime()
        {
            if (_mapdata.triggerTimeCanDecrease && _mapdata.triggerTime < _mapdata.patternDecreaseMinimum)
            {
                _currentTriggertime -= _mapdata.patternDecreaseInterval;
            }
        }

        public async UniTaskVoid TriggerAllPatterns()
        {
            if (_patternEnemy == null || _patternEnemy.Count == 0) return;
            
            var interval = _currentTriggertime / _patternEnemy.Count;

            var totalElapsed = 0f;

            for (var i = 0; i < _patternEnemy.Count; i++)
            {
                var pattern = _patternEnemy[i];
                if (_isDebug) Debug.Log($"[EnemyPatternController] Ready to play pattern {i + 1}/{_patternEnemy.Count}: '{pattern.pattern.name}'");

                await WaitForEnoughEnemyPoint(pattern);

                var elapsed = await PlayPatternWithTiming(i, pattern, interval);
                totalElapsed += elapsed;
            }

            if (_isDebug) Debug.Log($"[EnemyPatternController] All patterns triggered in total {totalElapsed:F2} seconds");
        }


        private async UniTask TriggerSinglePattern(MapDataSO.PatternOption patternData)
        {
            if (!CanTriggerPattern(patternData)) return;

            var enemyType = RandomType();
            var enemyAmount = Mathf.FloorToInt(CalculatePoint(enemyType, patternData));
            var rows = CalculatePatternRows(patternData, enemyAmount);
            await SpawnEnemyRows(rows, enemyType, patternData);
        }

        //RandomPattern to add to list
        public void AddRandomPattern()
        {
            if (_mapdata.PatternOptions == null || _mapdata.PatternOptions.Count == 0) return;

            var availablePatterns = _mapdata.PatternOptions
                .Where(p => !_patternEnemy.Contains(p)).ToList();

            if (availablePatterns.Count == 0)
            {
                if (_isDebug) Debug.Log("[EnemyPatternController] No available new patterns to add.");
                return;
            }

            var randomIndex = Random.Range(0, availablePatterns.Count);
            var selectedPattern = availablePatterns[randomIndex];
            _patternEnemy.Add(selectedPattern);

            if (_isDebug)
                Debug.Log(
                    $"[EnemyPatternController] Added random pattern: '{selectedPattern.pattern.name}'. Total patterns now: {_patternEnemy.Count}");
        }
        
        public MapDataSO.EnemyOption RandomType()
        {
            //Random
            return RandomUtility.GetWeightedRandom(_storeOption);
        }

        public float CalculatePoint(MapDataSO.EnemyOption enemyOption, MapDataSO.PatternOption patternOption)
        {
            var enemyAmount = Mathf.Floor(patternOption.patternPoint / enemyOption.EnemyPoint);
            return enemyAmount;
        }

        private bool CanTriggerPattern(MapDataSO.PatternOption patternData)
        {
            return patternData.pattern != null &&
                   SpawnerStateController.Instance.CurrentEnemyPoint > patternData.patternPoint;
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
            MapDataSO.PatternOption patternData)
        {
            foreach (var row in rows)
            {
                foreach (var pos in row)
                {
                    SpawnEnemy(enemyType, pos);
                    
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
                
            SpawnerStateController.Instance.CurrentEnemyPoint -= enemyType.EnemyPoint;
        }
    }
}