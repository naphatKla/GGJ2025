using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using GameControl.Interface;
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
            [FoldoutGroup("$id")]
            public string id;
            [FoldoutGroup("$id")]
            public EnemyController enemyController;
            [FoldoutGroup("$id")]
            public bool useCustomInterval;
            [FoldoutGroup("$id")] [ShowIf("$useCustomInterval")]
            public float customInterval;
            [FoldoutGroup("$id")]
            [Tooltip("Start of spawn point")]
            public float spawnPoint;
            [FoldoutGroup("$id")]
            [Tooltip("Growth rate of spawn point (Default 12.5%)")]
            public float enemyPointGrowthRate = 12.5f;
            [FoldoutGroup("$id")]
            public int prewarmCount = 30;
            [FoldoutGroup("$id")]
            [Range(0, 100)] public float chance = 100;
            
            public float Chance { get => chance; set => chance = value; }
            public float EnemyPoint { get => spawnPoint; set => spawnPoint = value; }
            public string EnemyId => id;
            public float EnemyCooldown => customInterval;
            public GameObject EnemyObject => enemyController.gameObject;
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
        
        [FoldoutGroup("Enemy Setting")]
        [Tooltip("Data of each enemy")]
        public List<EnemyOption> EnemyOptions;
        
        [FoldoutGroup("Enemy Setting")]
        [Tooltip("Enemy spawn interval (Default 1)")]
        public float defaultEnemySpawnTimer;

        [FoldoutGroup("Enemy Setting")] 
        [Tooltip("start spawn point of every enemy to spawn (Default 30)")]
        public float startEnemyPoint = 30;
        
        [FoldoutGroup("Enemy Setting")] 
        [Tooltip("Max spawn point of every enemy to spawn (Default 500)")]
        public float maxEnemyPoint = 500;
        
        [FoldoutGroup("Enemy Setting")] 
        [Tooltip("Max spawn point of every enemy to spawn (Default 20)")]
        public float increaseRateEnemyPoint;
    }
}
