using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace GameControl.SO
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [CreateAssetMenu(fileName = "RushData", menuName = "GameControl/SO/Rush Data")]
    public class RushDataSO : ScriptableObject
    {
        #region Rush Setting

        [FoldoutGroup("Rush Setting")] [OnValueChanged(nameof(ImportMapData))]
        public MapDataSO mapData;

        [Button("Import From MapData")]
        private void ImportMapData()
        {
            ImportEnemySetting(mapData);
            ImportPatternSetting(mapData);
            ImportEventMapSetting(mapData);
        }

        #endregion

        #region Enemy Setting

        [FoldoutGroup("Enemy Setting")] public List<MapDataSO.EnemyOption> enemyOptions;
        [FoldoutGroup("Enemy Setting")] public float intervalEnemyChanceUpgrade;
        [FoldoutGroup("Enemy Setting")] public float intervalEnemyPointRatioUpgrade;
        
        [FoldoutGroup("Enemy Setting")] [Title("Default Enemy Timer Setting")]
        [Tooltip("Enemy spawn interval (Default 1)")]
        public float defaultEnemySpawnTimer;

        public void ImportEnemySetting(MapDataSO src)
        {
            if (!src) return;

            intervalEnemyChanceUpgrade = src.intervalEnemyChanceUpgrade;
            intervalEnemyPointRatioUpgrade = src.intervalEnemyPointRatioUpgrade;
            defaultEnemySpawnTimer = src.defaultEnemySpawnTimer;

            enemyOptions = src.EnemyOptions?.ConvertAll(e => e == null
                ? null
                : new MapDataSO.EnemyOption
                {
                    id = e.id,
                    displayName = e.displayName,
                    enemyController = e.enemyController,
                    spawnPoint = e.spawnPoint,
                    enemyPointCanGrowth = e.enemyPointCanGrowth,
                    enemyPointGrowthRate = e.enemyPointGrowthRate,
                    chance = e.chance,
                    enemyChanceCanGrowth = e.enemyChanceCanGrowth,
                    enemyChanceGrowthRate = e.enemyChanceGrowthRate,
                    useCustomInterval = e.useCustomInterval,
                    customInterval = e.customInterval,
                    modifyNewData = e.modifyNewData,
                    enemyData = e.enemyData,
                    useSpawnConditions = e.useSpawnConditions,
                    conditionLogic = e.conditionLogic,
                    spawnConditions = e.spawnConditions != null ? new List<SpawnConditionSO>(e.spawnConditions) : null
                });

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #endregion

        #region Pattern Setting

        [FoldoutGroup("Pattern Setting")] [Tooltip("Data of each pattern")]
        public List<MapDataSO.PatternOption> patternOptions;

        [FoldoutGroup("Pattern Setting")]
        [Title("Time to play")]
        [InfoBox("Pattern จะถูกเล่นทุกค่านี้ ตามเวลาเกม เช่นเล่น Pattern ทั้งหมดทุกๆ 180 วิ")]
        [Tooltip(
            "The time of the pattern to play in trigger time (default player all pattern in 3 minute / 180 seconds)")]
        public float playAllPatternIn = 180f;

        [FoldoutGroup("Pattern Setting")]
        [Title("Pattern Setting")]
        [InfoBox(
            "เช่น มีทั้งหมด 3 Pattern ใน Pool มันจะเล่น 3 Pattern ภายในเวลานี้ (all pattern in 3 minute / 180 seconds)")]
        [Tooltip("interval of event will be trigger (Default 180 seconds)")]
        public float triggerAllPatternIn = 180f;

        [FoldoutGroup("Pattern Setting")] [Tooltip("if this enable trigger time will decrease")]
        public bool triggerTimeCanDecrease;

        [FoldoutGroup("Pattern Setting")]
        [Tooltip("interval of pattern trigger time to decrease (default 30) (enable on start only not in runtime)")]
        [InfoBox("จะลด TriggerTime ทุกๆเท่าไหร่เช่น ค่า triggerAllPatternIn จะถูกลดทุกๆ 25 วิ")]
        [ShowIf("$triggerTimeCanDecrease")]
        public float patternDecreaseInterval;

        [FoldoutGroup("Pattern Setting")]
        [Tooltip("triggerTime will decrease by this float")]
        [InfoBox("TriggerTime จากถูกลดกี่วิจากค่านี้เช่น 180 วิถูกลดลง 10 วิเป็น เล่น Pattern ทั้งหมดภายใน 170 วิ")]
        [ShowIf("$triggerTimeCanDecrease")]
        public float patternDecreaseRate;

        [FoldoutGroup("Pattern Setting")]
        [Tooltip("Minimum of triggertime")]
        [InfoBox("ค่าที่ต่ำที่สุดของ triggerAllPatternIn ที่จะต่ำได้")]
        [ShowIf("$triggerTimeCanDecrease")]
        public float patternDecreaseMinimum;

        [FoldoutGroup("Pattern Setting")]
        [Title("Add Pattern Setting")]
        [Tooltip("Add pattern every this variable default (30s)")]
        public float addPatternInterval = 30f;

        [FoldoutGroup("Pattern Setting")] public int amountToAdd = 1;
        [FoldoutGroup("Pattern Setting")] public float patternMax;
        [FoldoutGroup("Pattern Setting")] public bool canDuplicateAfterHaveAllPattern;

        public void ImportPatternSetting(MapDataSO src)
        {
            if (!src) return;
            playAllPatternIn = src.playAllPatternIn;
            triggerAllPatternIn = src.triggerAllPatternIn;
            triggerTimeCanDecrease = src.triggerTimeCanDecrease;
            patternDecreaseInterval = src.patternDecreaseInterval;
            patternDecreaseRate = src.patternDecreaseRate;
            patternDecreaseMinimum = src.patternDecreaseMinimum;
            addPatternInterval = src.addPatternInterval;
            amountToAdd = src.amountToAdd;
            patternMax = src.patternMax;
            canDuplicateAfterHaveAllPattern = src.canDuplicateAfterHaveAllPattern;

            if (src.PatternOptions != null)
            {
                patternOptions = new List<MapDataSO.PatternOption>(src.PatternOptions.Count);
                foreach (var p in src.PatternOptions)
                {
                    var copy = new MapDataSO.PatternOption
                    {
                        enableThisPattern = p.enableThisPattern,
                        bypassSpawnCondition = p.bypassSpawnCondition,
                        enableSpecificEnemy = p.enableSpecificEnemy,
                        specificEnemyList = p.specificEnemyList != null
                            ? new List<MapDataSO.PatternOption.EnemyKv>(p.specificEnemyList)
                            : null,
                        pattern = p.pattern,
                        delayBetweenEnemy = p.delayBetweenEnemy,
                        delayBetweenRows = p.delayBetweenRows,
                        patternPoint = p.patternPoint,
                        enableMovementAfter = p.enableMovementAfter,
                        enablePatternCenter = p.enablePatternCenter,
                        patternCenter = p.patternCenter
                    };
                    patternOptions.Add(copy);
                }
            }
            else
            {
                patternOptions = null;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #endregion

        #region Event Map Setting

        [FoldoutGroup("Event Map Setting")] public List<MapDataSO.EventMapOption> eventmapOptions;

        public void ImportEventMapSetting(MapDataSO src)
        {
            if (!src) return;

            if (src.eventmapOptions != null)
            {
                eventmapOptions = new List<MapDataSO.EventMapOption>(src.eventmapOptions.Count);
                foreach (var ev in src.eventmapOptions)
                {
                    var copy = new MapDataSO.EventMapOption
                    {
                        enableThisMapEvent = ev.enableThisMapEvent,
                        catagolyMapEvent = ev.catagolyMapEvent,
                        eventMapChance = ev.eventMapChance,
                        playInterval = ev.playInterval,
                        intervalCanModify = ev.intervalCanModify,
                        rateModify = ev.rateModify,
                        intervalModify = ev.intervalModify,
                        minPlayInterval = ev.minPlayInterval,
                        allMapEventID = ev.allMapEventID != null
                            ? ev.allMapEventID.ConvertAll(k => new MapDataSO.EventMapOption.MapEventKv
                            {
                                mapEventID = k.mapEventID,
                                chance = k.chance,
                                useWeightRandom = k.useWeightRandom
                            })
                            : null
                    };
                    eventmapOptions.Add(copy);
                }
            }
            else
            {
                eventmapOptions = null;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #endregion
        
        
        public void ApplyInto(MapDataSO target)
        {
            if (!target) return;

            // Enemy scalars
            target.intervalEnemyChanceUpgrade     = intervalEnemyChanceUpgrade;
            target.intervalEnemyPointRatioUpgrade = intervalEnemyPointRatioUpgrade;
            target.defaultEnemySpawnTimer = defaultEnemySpawnTimer;
            // Enemy list
            target.EnemyOptions = enemyOptions != null ? new List<MapDataSO.EnemyOption>(enemyOptions) : null;

            // Pattern scalars
            target.playAllPatternIn         = playAllPatternIn;
            target.triggerAllPatternIn      = triggerAllPatternIn;
            target.triggerTimeCanDecrease   = triggerTimeCanDecrease;
            target.patternDecreaseInterval  = patternDecreaseInterval;
            target.patternDecreaseRate      = patternDecreaseRate;
            target.patternDecreaseMinimum   = patternDecreaseMinimum;
            target.addPatternInterval       = addPatternInterval;
            target.amountToAdd              = amountToAdd;
            target.patternMax               = patternMax;
            target.canDuplicateAfterHaveAllPattern = canDuplicateAfterHaveAllPattern;
            // Pattern list
            target.PatternOptions = patternOptions != null ? new List<MapDataSO.PatternOption>(patternOptions) : null;

            // Event map list
            target.eventmapOptions = eventmapOptions != null ? new List<MapDataSO.EventMapOption>(eventmapOptions) : null;
        }
    }
}