using System;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SO.SkillDataSo
{
    [CreateAssetMenu(fileName = "SkillEnemySummonData", menuName = "GameData/SkillData/SkillEnemySummonData")]
    public class SkillEnemySummonDataSo : BaseSkillDataSo
    {
        public enum SpawnOrigin
        {
            Owner,
            Player,
            MidpointOwnerPlayer
        }

        [Serializable]
        public class SummonOption
        {
            [FoldoutGroup("$id")]
            public bool enabled = true;

            [FoldoutGroup("$id")]
            public string id;

            [FoldoutGroup("$id")]
            [Tooltip("Fallback prefab used only when pooled summon is unavailable and fallback is enabled.")]
            public EnemyController enemyPrefab;

            [FoldoutGroup("$id")]
            public bool overrideCharacterData;

            [FoldoutGroup("$id")]
            [ShowIf(nameof(overrideCharacterData))]
            public EnemyDataSo characterDataOverride;

            [FoldoutGroup("$id")]
            [MinValue(0)]
            public int minAmount;

            [FoldoutGroup("$id")]
            [MinValue(0)]
            public int maxAmount = 1;

            [FoldoutGroup("$id")]
            [MinValue(0)]
            public float weight = 100f;
        }

        [FoldoutGroup("Summon Count")]
        [MinValue(0)]
        [SerializeField] private int minTotalSummonCount = 1;

        [FoldoutGroup("Summon Count")]
        [MinValue(0)]
        [SerializeField] private int maxTotalSummonCount = 1;

        [FoldoutGroup("Summon Count")]
        [Tooltip("0 = unlimited. Counts only summons created by this runtime instance that are still active.")]
        [MinValue(0)]
        [SerializeField] private int maxAliveSummons;

        [FoldoutGroup("Pooling")]
        [SerializeField] private bool useSpawnerPool = true;

        [FoldoutGroup("Pooling")]
        [ShowIf(nameof(useSpawnerPool))]
        [Tooltip("If true, summon will skip this option when the spawner pool has no inactive prewarmed instance, preventing runtime instantiation.")]
        [SerializeField] private bool requirePrewarmedPoolInstance = true;

        [FoldoutGroup("Pooling")]
        [ShowIf(nameof(useSpawnerPool))]
        [Tooltip("If true, pooled summons count toward each MapData EnemyOption maximumPerEnemy and return enemy points on death.")]
        [SerializeField] private bool countTowardMapPerEnemyMax;

        [FoldoutGroup("Pooling")]
        [SerializeField] private bool allowInstantiateFallback;

        [FoldoutGroup("Pooling")]
        [SerializeField] private bool showTrailOnSpawn = true;

        [FoldoutGroup("Timing")]
        [MinValue(0)]
        [SerializeField] private float delayBeforeFirstSummon;

        [FoldoutGroup("Timing")]
        [MinValue(0)]
        [SerializeField] private float delayBetweenSummons;

        [FoldoutGroup("Spawn Position")]
        [SerializeField] private SpawnOrigin spawnOrigin = SpawnOrigin.Owner;

        [FoldoutGroup("Spawn Position")]
        [MinValue(0)]
        [SerializeField] private float minSpawnRadius = 1.5f;

        [FoldoutGroup("Spawn Position")]
        [MinValue(0)]
        [SerializeField] private float maxSpawnRadius = 4f;

        [FoldoutGroup("Spawn Position")]
        [SerializeField] private bool useForwardCone;

        [FoldoutGroup("Spawn Position")]
        [ShowIf(nameof(useForwardCone))]
        [Range(0f, 360f)]
        [SerializeField] private float coneAngle = 120f;

        [FoldoutGroup("Spawn Position")]
        [SerializeField] private bool facePlayerOnSpawn = true;

        [FoldoutGroup("Spawn Position")]
        [MinValue(1)]
        [SerializeField] private int positionAttempts = 8;

        [FoldoutGroup("Spawn Position")]
        [SerializeField] private bool avoidOverlap;

        [FoldoutGroup("Spawn Position")]
        [ShowIf(nameof(avoidOverlap))]
        [MinValue(0)]
        [SerializeField] private float overlapRadius = 0.5f;

        [FoldoutGroup("Spawn Position")]
        [ShowIf(nameof(avoidOverlap))]
        [SerializeField] private LayerMask overlapLayer;

        [FoldoutGroup("Summon Options")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<SummonOption> summonOptions = new();

        public int MinTotalSummonCount => minTotalSummonCount;
        public int MaxTotalSummonCount => maxTotalSummonCount;
        public int MaxAliveSummons => maxAliveSummons;
        public bool UseSpawnerPool => useSpawnerPool;
        public bool RequirePrewarmedPoolInstance => requirePrewarmedPoolInstance;
        public bool CountTowardMapPerEnemyMax => countTowardMapPerEnemyMax;
        public bool AllowInstantiateFallback => allowInstantiateFallback;
        public bool ShowTrailOnSpawn => showTrailOnSpawn;
        public float DelayBeforeFirstSummon => delayBeforeFirstSummon;
        public float DelayBetweenSummons => delayBetweenSummons;
        public SpawnOrigin Origin => spawnOrigin;
        public float MinSpawnRadius => minSpawnRadius;
        public float MaxSpawnRadius => maxSpawnRadius;
        public bool UseForwardCone => useForwardCone;
        public float ConeAngle => coneAngle;
        public bool FacePlayerOnSpawn => facePlayerOnSpawn;
        public int PositionAttempts => positionAttempts;
        public bool AvoidOverlap => avoidOverlap;
        public float OverlapRadius => overlapRadius;
        public LayerMask OverlapLayer => overlapLayer;
        public List<SummonOption> SummonOptions => summonOptions;

        private void OnValidate()
        {
            if (maxTotalSummonCount < minTotalSummonCount)
                maxTotalSummonCount = minTotalSummonCount;

            if (maxSpawnRadius < minSpawnRadius)
                maxSpawnRadius = minSpawnRadius;

            if (summonOptions == null) return;

            foreach (var option in summonOptions)
            {
                if (option == null) continue;
                if (string.IsNullOrWhiteSpace(option.id) && option.enemyPrefab != null)
                    option.id = option.enemyPrefab.name;

                option.minAmount = Mathf.Max(0, option.minAmount);
                option.maxAmount = Mathf.Max(option.minAmount, option.maxAmount);
                option.weight = Mathf.Max(0f, option.weight);
            }
        }
    }
}
