using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameControl.Controller
{
    public class EnemyPatternController
    {
        private SO.MapDataSO _mapdata;
        private SpawnerStateController _state;
        private readonly Vector2 _regionSize;
        private List<SO.MapDataSO.PatternOption> _patternEnemy;
        private List<SO.MapDataSO.EnemyOption> _storeEnemy;

        public EnemyPatternController(SO.MapDataSO mapData, SpawnerStateController state, Vector2 spawnRegion)
        {
            _mapdata = mapData;
            _state = state;
            _regionSize = spawnRegion;
            _patternEnemy = new List<SO.MapDataSO.PatternOption>();
        }
        
        public void SetEnemyList(List<SO.MapDataSO.EnemyOption> enemyList)
        {
            _storeEnemy = enemyList;
        }

        public async UniTaskVoid TriggerAllPatternsIn3Minutes()
        {
            if (_patternEnemy == null || _patternEnemy.Count == 0) return;
            var totalDuration = 180f;
            var interval = totalDuration / _patternEnemy.Count;
            for (var i = 0; i < _patternEnemy.Count; i++)
            {
                var pattern = _patternEnemy[i];

                TriggerSinglePattern(pattern).Forget();

                await UniTask.Delay((int)(interval * 1000));
            }
        }

        private async UniTaskVoid TriggerSinglePattern(SO.MapDataSO.PatternOption patternData)
        {
            if (!CanTriggerPattern(patternData)) return;

            var enemyType = RandomType();
            var enemyAmount = Mathf.FloorToInt(CalculatePoint(enemyType, patternData));
            var rows = CalculatePatternRows(patternData, enemyAmount);
            await SpawnEnemyRows(rows, enemyType, patternData);
            Debug.Log($"Triggered pattern '{patternData.pattern.name}' with {enemyAmount} enemies in {rows.Count} rows.");
        }
        
        //RandomPattern to add to list
        public void AddRandomPattern()
        {
            if (_mapdata.PatternOptions == null || _mapdata.PatternOptions.Count == 0) return;
            var availablePatterns = _mapdata.PatternOptions
                .Where(p => !_patternEnemy.Contains(p)).ToList();

            if (availablePatterns.Count == 0) return;
            var randomIndex = Random.Range(0, availablePatterns.Count);
            var selectedPattern = availablePatterns[randomIndex];
            _patternEnemy.Add(selectedPattern);
        }
        
        //RandomEnemy to spawn and Calculate point how many enemy should spawn base on patternpoint
        //var enemyIds = PoolingSystem.Instance.GetIds("Enemy");
        public SO.MapDataSO.EnemyOption RandomType()
        {
            //Random
            return RandomUtility.GetWeightedRandom(_storeEnemy);
        }
        public float CalculatePoint(SO.MapDataSO.EnemyOption enemyOption, SO.MapDataSO.PatternOption patternOption)
        {
            float enemyAmount = Mathf.Floor(patternOption.patternPoint / enemyOption.EnemyPoint);
            return enemyAmount;
        }
        
        private bool CanTriggerPattern(SO.MapDataSO.PatternOption patternData)
        {
            return patternData.pattern != null &&
                   SpawnerStateController.Instance.CurrentEnemyPoint >= patternData.patternPoint;
        }

        private List<List<Vector2>> CalculatePatternRows(SO.MapDataSO.PatternOption patternData, int enemyAmount)
        {
            var center = _mapdata.PatternCenter;
            return patternData.pattern.CalculateRows(center, enemyAmount);
        }
        
        private async UniTask SpawnEnemyRows(List<List<Vector2>> rows, SO.MapDataSO.EnemyOption enemyType, SO.MapDataSO.PatternOption patternData)
        {
            foreach (var row in rows)
            {
                foreach (var pos in row)
                {
                    PoolingSystem.Instance.Spawn(enemyType.id, pos);
                    SpawnerStateController.Instance.CurrentEnemyPoint -= enemyType.EnemyPoint;

                    if (patternData.DelayBetweenEnemy > 0)
                        await UniTask.Delay((int)(patternData.DelayBetweenEnemy * 1000));
                }

                if (patternData.DelayBetweenRows > 0)
                    await UniTask.Delay((int)(patternData.DelayBetweenRows * 1000));
            }
        }

    }

}

