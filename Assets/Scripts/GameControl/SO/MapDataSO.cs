using System;
using System.Collections;
using System.Collections.Generic;
using Characters.CollectItemSystems.CollectableItems;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using GameControl.Controller;
using GameControl.Interface;
using GameControl.Pattern;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameControl.SO
{
    [CreateAssetMenu(fileName = "MapData", menuName = "GameControl/SO/Map Data", order = 0)]
    public class MapDataSO : ScriptableObject
    {
        
        [Serializable]
        public class EnemyOption : IRandomable
        {
            [FoldoutGroup("$id")][Title("Setting")]
            public string id;
            [FoldoutGroup("$id")]
            public string displayName;
            [FoldoutGroup("$id")]
            public EnemyController enemyController;
            
            [FoldoutGroup("$id")] [Title("Spawn Point")]
            [Tooltip("Start of spawn point")]
            public float spawnPoint;
            [FoldoutGroup("$id")]
            [GUIColor("@this.enemyPointCanGrowth ? Color.green : Color.red")]
            [Tooltip("if this enable this enemy point will increase every 30 seconds")]
            public bool enemyPointCanGrowth;
            [FoldoutGroup("$id")] [ShowIf("$enemyPointCanGrowth")]
            [Tooltip("Growth rate of spawn point (Default 12.5%)")]
            public float enemyPointGrowthRate = 12.5f;
            
            [FoldoutGroup("$id")][Title("Chance Setting")]
            public float chance = 100;
            [GUIColor("@this.enemyChanceCanGrowth ? Color.green : Color.red")]
            [FoldoutGroup("$id")][Tooltip("if this enable this enemy chance will increase and auto weight every 30 seconds")]
            public bool enemyChanceCanGrowth;
            [FoldoutGroup("$id")] [ShowIf("$enemyChanceCanGrowth")]
            [Tooltip("Growth rate of chance")]
            public float enemyChanceGrowthRate = 0f;
            [FoldoutGroup("$id")] [GUIColor("@this.disableNormalize ? Color.green : Color.red")]
            public bool disableNormalize;
            
            [FoldoutGroup("$id")][Title("Enemy Interval")][GUIColor("@this.useCustomInterval ? Color.green : Color.red")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            
            [FoldoutGroup("$id")][Title("Per-Enemy Max")] [GUIColor("@this.useMaxperEnemy ? Color.green : Color.red")]
            [Tooltip("Enable to limit maximum concurrent active enemy")]
            public bool useMaxperEnemy;
            [FoldoutGroup("$id")][ShowIf("$useMaxperEnemy")]
            [Tooltip("Maximum ACTIVE instances for this enemy type (<=0 = unlimited).")]
            public float maximumPerEnemy = 0f;
            
            [FoldoutGroup("$id")][Title("Enemy Status")] [GUIColor("@this.modifyNewData ? Color.green : Color.red")]
            public bool modifyNewData;
            [FoldoutGroup("$id")] [ShowIf("$modifyNewData")]
            public EnemyDataSo enemyData;
            [FoldoutGroup("$id")] [GUIColor("@this.disableInPattern ? Color.green : Color.red")]
            public bool disableInPattern;
            [FoldoutGroup("$id")] [GUIColor("@this.disableEnemyDetect ? Color.green : Color.red")]
            public bool disableEnemyDetect;
            
            [FoldoutGroup("$id")][Title("Spawn Conditions")] [GUIColor("@this.useSpawnConditions ? Color.green : Color.red")]
            public bool useSpawnConditions = false;
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnConditions")]
            public ConditionLogic conditionLogic = ConditionLogic.All; // All = AND, Any = OR
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnConditions")]
            public List<EnemySpawnConditionSO> spawnConditions;
            
            [FoldoutGroup("$id")][Title("Spawn Effect")] [GUIColor("@this.useSpawnEffect ? Color.green : Color.red")]
            public bool useSpawnEffect = false;
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnEffect")]
            public List<SpawnEffectStruct> spawnEffect;

            public enum SpawnEffectType
            {
                ShowPopup,
                KillAllEnemy,
                StopEnemySpawn,
                DelaySpawn
            }
            
            [Serializable]
            public class SpawnEffectStruct
            {
                public SpawnEffectType effectType;
                [ShowIf("@effectType == SpawnEffectType.ShowPopup")]
                public string effectString;
                [ShowIf("@effectType == SpawnEffectType.DelaySpawn")]
                public int effectint;
            }

            public enum ConditionLogic { All, Any }
            [NonSerialized] public int activeCount;
            
            public void InitRuntime()
            {
                activeCount = 0;
                if (useMaxperEnemy)
                {
                    int max = Mathf.RoundToInt(maximumPerEnemy); // <=0 = unlimited
                    maximumPerEnemy = max;
                }
            }

            public bool IsBelowPerEnemyMax()
            {
                if (!useMaxperEnemy) return true;
                int max = Mathf.RoundToInt(maximumPerEnemy);
                return (max <= 0) || (activeCount < max);
            }

            public bool IsSpawnable(SpawnerStateController state, MapDataSO mapData)
            {
                if (!useSpawnConditions || spawnConditions == null || spawnConditions.Count == 0) return true;

                if (conditionLogic == ConditionLogic.All)
                {
                    foreach (var c in spawnConditions)
                    {
                        if (c == null) continue;
                        if (!c.IsSatisfied(state, mapData, this))
                        {
                            //Debug.Log($"[Spawn] {id} blocked by condition {c.name} (All)");
                            return false;
                        }
                    }
                    return true;
                }

                // Any
                foreach (var c in spawnConditions)
                {
                    if (c == null) continue;
                    if (c.IsSatisfied(state, mapData, this))
                    {
                        return true;
                    }
                }
                return false;
            }
            
            public float Chance { get => chance; set => chance = value; }
            public float EnemyPoint { get => spawnPoint; set => spawnPoint = value; }
            public string EnemyId => id;
            public float EnemyCooldown => customInterval;
            public GameObject EnemyObject
            {
                get => enemyController.gameObject;
                set => enemyController = value?.GetComponent<EnemyController>();
            }
            public bool TryPassChance() => Random.Range(0, 100) < chance;
        }
        
        [Serializable]
        public class PatternOption
        {
            [FoldoutGroup("$pattern")] [GUIColor("@this.enableThisPattern ? Color.green : Color.red")]
            public bool enableThisPattern;
            
            [FoldoutGroup("$pattern")] [GUIColor("@this.bypassSpawnCondition ? Color.green : Color.red")]
            public bool bypassSpawnCondition;
            
            [FoldoutGroup("$pattern")] [GUIColor("@this.enableSpecificEnemy ? Color.green : Color.red")]
            public bool enableSpecificEnemy;

            [Serializable]
            public struct EnemyKv
            {
                public string enemyID; 
                public float chance;
            }
            [FoldoutGroup("$pattern")] [Title("Enemy Specific")] [ShowIf("enableSpecificEnemy")] 
            public List<EnemyKv> specificEnemyList;
            
            [FoldoutGroup("$pattern")] [Title("Setting")]
            public BaseSpawnPattern pattern;
            [FoldoutGroup("$pattern")]
            public float delayBetweenEnemy = 0.1f;
            [FoldoutGroup("$pattern")]
            public float delayBetweenRows = 1f;
            [FoldoutGroup("$pattern")]
            [Tooltip("the amount of point to use calcalate how many enemy should spawn base on point")]
            public float patternPoint;
            [FoldoutGroup("$pattern")]
            public float enableMovementAfter = 1f;
            [FoldoutGroup("$pattern")] [GUIColor("@this.enablePatternCenter ? Color.green : Color.red")]
            [Tooltip("if enable you can set custom center of the pattern")]
            public bool enablePatternCenter;
            [FoldoutGroup("$pattern")] [ShowIf("$enablePatternCenter")]
            public Vector2 patternCenter;
            
            [FoldoutGroup("$pattern")] [GUIColor("@this.useCondition ? Color.green : Color.red")]
            public bool useCondition;
            //Condition
            [FoldoutGroup("$pattern")][BoxGroup("$pattern/Condition")][ShowIf("useCondition")]
            public List<PatternConditionStruct> patternCondition;
                
            public enum PatternConditionType
            {
                TimeCondition
            }
            
            [Serializable]
            public class PatternConditionStruct
            {
                public PatternConditionType conditionType;
                    
                // ----- TimeCondition -----
                [ShowIf("@conditionType == PatternConditionType.TimeCondition")]
                [Tooltip("เวลาตั้งแต่เริ่มเกม (วินาที)")]
                public float startAfter = 0f;

                [ShowIf("@conditionType == PatternConditionType.TimeCondition")]
                [Tooltip("เวลาที่สิ้นสุด (วินาที) (ถ้า < 0 = ไม่มีขีดจำกัด)")]
                public float endAt = -1f;
            }
            
            public float DelayBetweenRows => delayBetweenRows;
            public float DelayBetweenEnemy => delayBetweenEnemy;
        }
        
        [Serializable]
        public class ItemOption : IRandomable
        {
            [FoldoutGroup("$id")][Title("Setting")]
            public string id;
            [FoldoutGroup("$id")]
            public BaseCollectableItem itemObj;
          
            [FoldoutGroup("$id")][Title("Chance Setting")]
            [Range(0, 100)] public float chance = 100;
            
            [FoldoutGroup("$id")][Title("Item Generate Interval")] [GUIColor("@this.useCustomInterval ? Color.green : Color.red")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            
            [FoldoutGroup("$id")][Title("Per-Item Max")] [GUIColor("@this.useMaxperItem ? Color.green : Color.red")]
            [Tooltip("Enable to limit maximum concurrent active items of this type.")]
            public bool useMaxperItem;
            [FoldoutGroup("$id")][ShowIf("$useMaxperItem")]
            [Tooltip("Maximum ACTIVE instances for this item type (<=0 = unlimited).")]
            public float maximumPerItem = 0f;
            
            [FoldoutGroup("$id")][Title("Life time")] [GUIColor("@this.useLifetimeInterval ? Color.green : Color.red")]
            [Tooltip("if this enable item can despawn after lifetime")]
            public bool useLifetimeInterval;
            [FoldoutGroup("$id")] [ShowIf("$useLifetimeInterval")]
            [Tooltip("Interval of item lifetime (default 20 seconds)")]
            public float lifetimeInterval = 20f;
            
            [FoldoutGroup("$id")][Title("Spawn Conditions")] [GUIColor("@this.useSpawnConditions ? Color.green : Color.red")]
            public bool useSpawnConditions = false;
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnConditions")]
            public ConditionLogic conditionLogic = ConditionLogic.All; // All = AND, Any = OR
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnConditions")]
            public List<ItemSpawnConditionSO> spawnConditions;

            public enum ConditionLogic { All, Any }
            [NonSerialized] public int activeCount;
            
            public void InitRuntime()
            {
                activeCount = 0;

                if (useMaxperItem)
                {
                    int max = Mathf.RoundToInt(maximumPerItem); // <=0 = unlimited
                    maximumPerItem = max;
                }
            }

            public bool IsBelowPerItemMax()
            {
                if (!useMaxperItem) return true;
                int max = Mathf.RoundToInt(maximumPerItem);
                return (max <= 0) || (activeCount < max);
            }

            public bool IsSpawnable(SpawnerStateController state, MapDataSO mapData)
            {
                if (!useSpawnConditions || spawnConditions == null || spawnConditions.Count == 0) return true;

                if (conditionLogic == ConditionLogic.All)
                {
                    foreach (var c in spawnConditions)
                    {
                        if (c == null) continue;
                        if (!c.IsSatisfied(state, mapData, this))
                        {
                            //Debug.Log($"[Spawn] {id} blocked by condition {c.name} (All)");
                            return false;
                        }
                    }
                    return true;
                }

                // Any
                foreach (var c in spawnConditions)
                {
                    if (c == null) continue;
                    if (c.IsSatisfied(state, mapData, this))
                    {
                        return true;
                    }
                }
                return false;
            }
            
            public float Chance { get => chance; set => chance = value; }
            public string ItemId => id;
            public float ItemCooldown => customInterval;
            public bool TryPassChance() => Random.Range(0, 100) < chance;
        }
        
        [Serializable]
        public class EventMapOption
        {
            [FoldoutGroup("$catagolyMapEvent")] [Title("Propertie")] [GUIColor("@this.enableThisMapEvent ? Color.green : Color.red")]
            public bool enableThisMapEvent;
            
            [FoldoutGroup("$catagolyMapEvent")]
            public string catagolyMapEvent;

            [Serializable]
            public struct MapEventKv
            {
                public string mapEventID; 
                public float chance;
                
                [GUIColor("@this.useWeightRandom ? Color.green : Color.red")]
                public bool useWeightRandom;
                
                [GUIColor("@this.overrideData ? Color.green : Color.red")]
                public bool overrideData;
                [Space]
                //Override Zone
                [BoxGroup("Modify Data")][ShowIf("overrideData")]
                public float damageMap;
                
                [GUIColor("@this.useCondition ? Color.green : Color.red")]
                public bool useCondition;
                [Space]
                //Condition
                [BoxGroup("Condition")][ShowIf("useCondition")]
                public List<MapEventConditionStruct> mapEventCondition;
                
                public enum MapEventConditionType
                {
                    TimeCondition
                }
            
                [Serializable]
                public class MapEventConditionStruct
                {
                    public MapEventConditionType conditionType;
                    
                    // ----- TimeCondition -----
                    [ShowIf("@conditionType == MapEventConditionType.TimeCondition")]
                    [Tooltip("เวลาตั้งแต่เริ่มเกม (วินาที)")]
                    public float startAfter = 0f;

                    [ShowIf("@conditionType == MapEventConditionType.TimeCondition")]
                    [Tooltip("เวลาที่สิ้นสุด (วินาที) (ถ้า < 0 = ไม่มีขีดจำกัด)")]
                    public float endAt = -1f;
                }
            }
           
            [FoldoutGroup("$catagolyMapEvent")]
            public List<MapEventKv> allMapEventID;
            
            [FoldoutGroup("$catagolyMapEvent")] [Title("Chance")]
            public float eventMapChance;
          
            [FoldoutGroup("$catagolyMapEvent")] [Title("Interval")]
            public float playInterval;

            [FoldoutGroup("$catagolyMapEvent")] [GUIColor("@this.intervalCanModify ? Color.green : Color.red")]
            public bool intervalCanModify;
            
            [FoldoutGroup("$catagolyMapEvent")] [Title("Interval Increase Setting")] [ShowIf("intervalCanModify")]
            [Tooltip("playInterval will modify by this float")]
            public float rateModify;
            
            [FoldoutGroup("$catagolyMapEvent")] [ShowIf("intervalCanModify")]
            [Tooltip("rate of modify to apply to playInterval")]
            public float intervalModify;
            
            [FoldoutGroup("$catagolyMapEvent")] [ShowIf("intervalCanModify")]
            [Tooltip("minimum of interval that can be lowest")]
            public float minPlayInterval;
            
            public float Chance { get => eventMapChance; set => eventMapChance = value; }
        }
        
        #region Map Setting
        [FoldoutGroup("Map Setting")]
        [Tooltip("id")]
        public string mapId;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Name of this map")]
        public string mapName;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Description")]
        [TextArea(1, 5)]
        public string description;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Description show if it still lock")]
        [TextArea(1, 5)]
        public string lockdescription;
    
        [FoldoutGroup("Map Setting")]
        [Tooltip("Map Image")]
        public Sprite image;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Map Time")] [HideIf("endlessMode")]
        public float mapGlobalTime;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Change to Endless Mode the time will not affect this mode the time will increase instend of decrease (Time will start from 0)")]
        public bool endlessMode = false;
        #endregion

        #region Enemy Setting
        [FoldoutGroup("Enemy Setting")]
        [Tooltip("Data of each enemy")]
        public List<EnemyOption> EnemyOptions;
        
        [FoldoutGroup("Enemy Setting")]
        [Tooltip("interval of enemy chance to upgrade (Default 30)")]
        public float intervalEnemyChanceUpgrade;
        
        [FoldoutGroup("Enemy Setting")]
        [Tooltip("interval of enemy point ratio to upgrade (Default 30)")]
        public float intervalEnemyPointRatioUpgrade;
        #endregion
        
        #region Pattern Setting
        [FoldoutGroup("Pattern Setting")]
        [Tooltip("Data of each pattern")]
        public List<PatternOption> PatternOptions;
        
        [FoldoutGroup("Pattern Setting")] [Title("Time to play")]
        [InfoBox("Pattern จะถูกเล่นทุกค่านี้ ตามเวลาเกม เช่นเล่น Pattern ทั้งหมดทุกๆ 180 วิ")]
        [Tooltip("The time of the pattern to play in trigger time (default player all pattern in 3 minute / 180 seconds)")]
        public float playAllPatternIn = 180f;
        
        [FoldoutGroup("Pattern Setting")] [Title("Pattern Setting")]
        [InfoBox("เช่น มีทั้งหมด 3 Pattern ใน Pool มันจะเล่น 3 Pattern ภายในเวลานี้ (all pattern in 3 minute / 180 seconds)")]
        [Tooltip("interval of event will be trigger (Default 180 seconds)")]
        public float triggerAllPatternIn = 180f;
        
        [FoldoutGroup("Pattern Setting")]
        [Tooltip("if this enable trigger time will decrease")]
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
        
        [FoldoutGroup("Pattern Setting")]
        [Tooltip("Amount to add (default 1)")]
        public int amountToAdd = 1;
        
        [FoldoutGroup("Pattern Setting")]
        [Tooltip("Max pattern that can be add")]
        public float patternMax = 20;
        
        [FoldoutGroup("Pattern Setting")]
        [InfoBox("Pattern จะสามารถถูก Add เพิ่มเข้าไปซ้ำได้ถ้าใช้ Pattern หมดไปแล้ว")]
        public bool canDuplicateAfterHaveAllPattern;
        #endregion

        #region Item Setting
        [FoldoutGroup("Item Setting")]
        [Tooltip("Data of each item")]
        public List<ItemOption> ItemOptions;
        
        [FoldoutGroup("Item Setting")]
        [Tooltip("Max spawning of item")]
        public float maxItemSpawning;
        
        [FoldoutGroup("Item Setting")]
        [Tooltip("Item spawn interval (Default 1)")]
        public float defaultItemSpawnTimer;
        #endregion
        
        #region Event Map Setting
        
        [FoldoutGroup("Event Map Setting")]
        [Tooltip("Data of each map event")]
        public List<EventMapOption> eventmapOptions;
        
        #endregion

        #region Data Setting
        [Space]
        [FoldoutGroup("Data Setting")]
        [Title("Default Enemy Timer Setting")]
        [Tooltip("Enemy spawn interval (Default 1)")]
        public float defaultEnemySpawnTimer;
        
        [FoldoutGroup("Data Setting")]
        [Tooltip("if this enable")]
        public bool canGrowth;
        
        [FoldoutGroup("Data Setting")]
        [Tooltip("How much defaultEnemySpawnTimer will be decrease")]
        [ShowIf("canGrowth")]
        public float decreaseAmount = 0.022f;
        
        [FoldoutGroup("Data Setting")]
        [Tooltip("Frequency defaultEnemySpawnTimer will be decrease")]
        [ShowIf("canGrowth")]
        public float decreaseInterval = 30f;
        
        [FoldoutGroup("Data Setting")]
        [Tooltip("Minimum of defaultEnemySpawnTimer can be lowest")]
        [ShowIf("canGrowth")]
        public float decreaseMinimum = 0.35f;
        
        [FoldoutGroup("Data Setting")] 
        [Title("Enemy Point")]
        [Tooltip("interval of enemy point to increase (Default 60 seconds)")]
        public float intervalIncreaseEnemyPoint = 60;
        
        [FoldoutGroup("Data Setting")] 
        [Tooltip("Rate of enemy point to increase (Default 20)")]
        public float rateIncreaseEnemyPoint = 20;
        
        [FoldoutGroup("Data Setting")] 
        [Tooltip("start spawn point of every enemy to spawn (Default 30)")]
        public float startEnemyPoint = 30;
        
        [FoldoutGroup("Data Setting")] 
        [Tooltip("Max spawn point of every enemy to spawn (Default 500)")]
        public float maxEnemyPoint = 500;
        #endregion

        #region Rush Setting

        [FoldoutGroup("Rush Setting")]
        public RushDataSO rushData;

        [FoldoutGroup("Rush Setting")] 
        [Tooltip("Time to enter rush (Default Last 60 seconds)")]
        public float rushTime = 60f;

        #endregion
    }
}
