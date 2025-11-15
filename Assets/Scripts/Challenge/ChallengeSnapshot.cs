using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Challenge
{
    namespace Challenge
    {
        public readonly struct PlayerSnapshot
        {
            // (Additive)
            public readonly IReadOnlyDictionary<PlayerAdditiveStat, float> AdditivePercent;
            // (Set)
            public readonly IReadOnlyDictionary<PlayerSetStat, float> SetOverrides;

            public PlayerSnapshot(
                IReadOnlyDictionary<PlayerAdditiveStat, float> additivePercent,
                IReadOnlyDictionary<PlayerSetStat, float> setOverrides)
            {
                AdditivePercent = additivePercent;
                SetOverrides = setOverrides;
            }

            public float GetAdd(PlayerAdditiveStat stat) =>
                AdditivePercent != null && AdditivePercent.TryGetValue(stat, out var v) ? v : 0f;

            public bool TryGetSet(PlayerSetStat stat, out float value)
            {
                value = 0f;
                return SetOverrides != null && SetOverrides.TryGetValue(stat, out value);
            }
        }
        
        public readonly struct EnemySnapshot
        {
            public readonly string EnemyId;
            public readonly IReadOnlyDictionary<EnemyStat, float> PercentByStat;

            public EnemySnapshot(string enemyId, IReadOnlyDictionary<EnemyStat, float> dict)
            {
                EnemyId = enemyId;
                PercentByStat = dict;
            }

            public float Get(EnemyStat stat)
            {
                return PercentByStat != null && PercentByStat.TryGetValue(stat, out var v) ? v : 0f;
            }
        }
        
        public readonly struct ChallengeSnapshot
        {
            [BoxGroup("Score")]
            public readonly float OverallScoreMultiplier;
            [BoxGroup("Score")]
            public readonly float FlatBonusPercent;
            [BoxGroup("Score")]
            public readonly float PlayerPercent;
            [BoxGroup("Score")]
            public readonly float EnemiesPercent;
            
            [BoxGroup("Player")]
            public readonly PlayerSnapshot Player;
            
            [BoxGroup("Enemy")]
            public readonly IReadOnlyDictionary<string, EnemySnapshot> Enemies;

            public ChallengeSnapshot(PlayerSnapshot player, IReadOnlyDictionary<string, EnemySnapshot> enemies,
                float overallScoreMultiplier, float flatBonusPercent, float playerPercent, float enemiesPercent)
            {
                Player = player;
                Enemies = enemies;
                OverallScoreMultiplier = overallScoreMultiplier;
                FlatBonusPercent = flatBonusPercent;
                PlayerPercent = playerPercent;
                EnemiesPercent = enemiesPercent;
            }
            
            public EnemySnapshot GetEnemy(string enemyId)
            {
                var id = string.IsNullOrWhiteSpace(enemyId) ? string.Empty : enemyId;
                if (Enemies == null)
                    return new EnemySnapshot(id, new Dictionary<EnemyStat, float>());

                // ถ้ามี enemyId อยู่แล้ว (ซึ่งใน result ผสาน global ไว้แล้ว) ก็คืนเลย
                if (Enemies.TryGetValue(id, out var self))
                    return self;

                // enemyId → คืน global ตรง ๆ
                if (Enemies.TryGetValue("*", out var global))
                    return global;

                // ไม่มีก็คืนว่าง
                return new EnemySnapshot(id, new Dictionary<EnemyStat, float>());
            }

        }
    }
}