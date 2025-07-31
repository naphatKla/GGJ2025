using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
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
            public EnemyController enemyController;
            [FoldoutGroup("$id")]
            public int prewarmCount = 30;
            
            [FoldoutGroup("$id")] [Title("Spawn Point")]
            [Tooltip("Start of spawn point")]
            public float spawnPoint;
            [FoldoutGroup("$id")]
            [Tooltip("if this enable this enemy point will increase every 30 seconds")]
            public bool enemyCanGrowth;
            [FoldoutGroup("$id")] [ShowIf("$enemyCanGrowth")]
            [Tooltip("Growth rate of spawn point (Default 12.5%)")]
            public float enemyPointGrowthRate = 12.5f;
            
            [FoldoutGroup("$id")][Title("Chance Setting")]
            [Range(0, 100)] public float chance = 100;
            
            [FoldoutGroup("$id")][Title("Enemy Interval")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            
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
            [FoldoutGroup("$pattern")] [Title("Setting")]
            public BaseSpawnPattern pattern;
            [FoldoutGroup("$pattern")]
            public float delayBetweenEnemy = 0.1f;
            [FoldoutGroup("$pattern")]
            public float delayBetweenRows = 1f;
            [FoldoutGroup("$pattern")]
            [Tooltip("the amount of point to use calcalate how many enemy should spawn base on point")]
            public float patternPoint;
            
            public float DelayBetweenRows => delayBetweenRows;
            public float DelayBetweenEnemy => delayBetweenEnemy;
        }
        
        [Serializable]
        public class ItemOption : IRandomable
        {
            [FoldoutGroup("$id")][Title("Setting")]
            public string id;
            [FoldoutGroup("$id")]
            public PoolableComponent itemObj;
            [FoldoutGroup("$id")]
            public int prewarmCount = 30;
          
            [FoldoutGroup("$id")][Title("Chance Setting")]
            [Range(0, 100)] public float chance = 100;
            
            [FoldoutGroup("$id")][Title("Item Generate Interval")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            
            public float Chance { get => chance; set => chance = value; }
            public string ItemId => id;
            public float ItemCooldown => customInterval;
            public bool TryPassChance() => Random.Range(0, 100) < chance;
        }
        
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
        
        [FoldoutGroup("Enemy Setting")]
        [Tooltip("Data of each enemy")]
        public List<EnemyOption> EnemyOptions;
        
        [FoldoutGroup("Pattern Setting")]
        [Tooltip("Data of each pattern")]
        public List<PatternOption> PatternOptions;
        
        [FoldoutGroup("Item Setting")]
        [Tooltip("Data of each item")]
        public List<ItemOption> ItemOptions;
        
        [FoldoutGroup("Item Setting")]
        [Tooltip("Max spawning of item")]
        public float maxItemSpawning;
        
        [FoldoutGroup("Item Setting")]
        [Tooltip("Item spawn interval (Default 1)")]
        public float defaultItemSpawnTimer;
        
        [FoldoutGroup("Pattern Setting")]
        [Tooltip("center of pattern")]
        public Vector2 PatternCenter;
        
        [Space]
        [FoldoutGroup("Data Setting")]
        [Tooltip("Enemy spawn interval (Default 1)")]
        public float defaultEnemySpawnTimer;

        [FoldoutGroup("Data Setting")] 
        [Tooltip("start spawn point of every enemy to spawn (Default 30)")]
        public float startEnemyPoint = 30;
        
        [FoldoutGroup("Data Setting")] 
        [Tooltip("Max spawn point of every enemy to spawn (Default 500)")]
        public float maxEnemyPoint = 500;
        
        [FoldoutGroup("Data Setting")] 
        [Tooltip("Max spawn point of every enemy to spawn (Default 20)")]
        public float increaseRateEnemyPoint;
    }
}
