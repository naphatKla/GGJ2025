using System;
using System.Collections;
using System.Collections.Generic;
using Characters.CollectItemSystems.CollectableItems;
using Characters.Controllers;
using GameControl.Controller;
using GameControl.Interface;
using GameControl.Pattern;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
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
            public EnemyController enemyController;
            
            [FoldoutGroup("$id")] [Title("Spawn Point")]
            [Tooltip("Start of spawn point")]
            public float spawnPoint;
            [FoldoutGroup("$id")]
            [Tooltip("if this enable this enemy point will increase every 30 seconds")]
            public bool enemyPointCanGrowth;
            [FoldoutGroup("$id")] [ShowIf("$enemyPointCanGrowth")]
            [Tooltip("Growth rate of spawn point (Default 12.5%)")]
            public float enemyPointGrowthRate = 12.5f;
            
            [FoldoutGroup("$id")][Title("Chance Setting")]
            public float chance = 100;
            [FoldoutGroup("$id")][Tooltip("if this enable this enemy chance will increase and auto weight every 30 seconds")]
            public bool enemyChanceCanGrowth;
            [FoldoutGroup("$id")] [ShowIf("$enemyChanceCanGrowth")]
            [Tooltip("Growth rate of chance")]
            public float enemyChanceGrowthRate = 0f;
            
            [FoldoutGroup("$id")][Title("Enemy Interval")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            
            [FoldoutGroup("$id")][Title("Spawn Conditions")]
            public bool useSpawnConditions = false;
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnConditions")]
            public ConditionLogic conditionLogic = ConditionLogic.All; // All = AND, Any = OR
            [FoldoutGroup("$id")]
            [ShowIf("$useSpawnConditions")]
            public List<SpawnConditionSO> spawnConditions;

            public enum ConditionLogic { All, Any }

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
            
            public MapDataSO.EnemyOption Clone()
            {
                return new MapDataSO.EnemyOption
                {
                    id = this.EnemyId,
                    chance = this.chance,
                };
            }

        }
        
        [Serializable]
        public class PatternOption
        {
            [FoldoutGroup("$pattern")]
            public bool enableThisPattern;
            
            [FoldoutGroup("$pattern")]
            public bool bypassSpawnCondition;
            
            [FoldoutGroup("$pattern")]
            public bool enableSpecificEnemy;

            [Serializable]
            public struct EnemyKv { public string enemyID; public float chance; }
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
            [FoldoutGroup("$pattern")]
            [Tooltip("if enable you can set custom center of the pattern")]
            public bool enablePatternCenter;
            [FoldoutGroup("$pattern")] [ShowIf("$enablePatternCenter")]
            public Vector2 patternCenter;
            
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
            
            [FoldoutGroup("$id")][Title("Item Generate Interval")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            
            [FoldoutGroup("$id")][Title("Life time")]
            [Tooltip("if this enable item can despawn after lifetime")]
            public bool useLifetimeInterval;
            [FoldoutGroup("$id")] [ShowIf("$useLifetimeInterval")]
            [Tooltip("Interval of item lifetime (default 20 seconds)")]
            public float lifetimeInterval = 20f;
            
            public float Chance { get => chance; set => chance = value; }
            public string ItemId => id;
            public float ItemCooldown => customInterval;
            public bool TryPassChance() => Random.Range(0, 100) < chance;
        }
        
        [Serializable]
        public class EventMapOption
        {
            [FoldoutGroup("$catagolyMapEvent")] [Title("Propertie")]
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
            }
           
            [FoldoutGroup("$catagolyMapEvent")]
            public List<MapEventKv> allMapEventID;
            
            [FoldoutGroup("$catagolyMapEvent")] [Title("Chance")]
            public float eventMapChance;
          
            [FoldoutGroup("$catagolyMapEvent")] [Title("Interval")]
            public float playInterval;

            [FoldoutGroup("$catagolyMapEvent")]
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
        [Tooltip("Name of this map")]
        public string mapName;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Background image to use for this map")]
        public Sprite background;
    
        [FoldoutGroup("Map Setting")]
        [Tooltip("Map Image")]
        public Sprite image;
        
        [FoldoutGroup("Map Setting")]
        [Tooltip("Map Time")]
        public float mapGlobalTime;
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
        public float patternMax;
        
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
        [Tooltip("Enemy spawn interval (Default 1)")]
        public float defaultEnemySpawnTimer;

        [FoldoutGroup("Data Setting")] 
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
    }
}
