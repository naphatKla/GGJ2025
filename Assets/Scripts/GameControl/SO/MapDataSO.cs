using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameControl.SO
{
    [CreateAssetMenu(fileName = "MapData", menuName = "GameControl/SO/Map Data", order = 0)]
    public class MapDataSO : ScriptableObject
    {
        [Serializable]
        public class EnemyOption
        {
            [FoldoutGroup("$id")]
            public string id;
            [FoldoutGroup("$id")]
            public EnemyController enemyController;
            [FoldoutGroup("$id")]
            [Tooltip("Start of spawn point")]
            public float spawnPoint;
            [FoldoutGroup("$id")]
            [Tooltip("Growth rate of spawn point (Default 12.5%)")]
            public float enemyPointGrowthRate = 12.5f;
            [FoldoutGroup("$id")]
            public int prewarmCount = 30;
            [FoldoutGroup("$id")]
            public int weight = 1;
            [FoldoutGroup("$id")]
            [Range(0, 100)] public int chance = 100;
            
            public int EnemyWeight => weight;
            public int EnemyChance => chance;
            public float EnemyPoint => spawnPoint;
            public string EnemyId => id;
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
