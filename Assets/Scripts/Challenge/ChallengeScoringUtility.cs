using System.Collections.Generic;
using Challenge;
using UnityEngine;

namespace Challenge
{
    public static class ChallengeScoringUtility
    {
        public struct ScoreBreakdown
        {
            public float flatBonus;          // จาก SO ตรง ๆ
            public float playerPercent;      // จาก Debuff ผู้เล่น (คิดเฉพาะค่าติดลบ)
            public float enemiesPercentSum;  // จาก Enemy Groups (คิดเฉพาะค่าติดบวก รวมทุก group)
            public float totalPercent;       // flat + player + enemies
            public float multiplier;         // 1 + totalPercent/100
        }

        public static ScoreBreakdown Compute(ChallengeDataSO def)
        {
            var b = new ScoreBreakdown();
            if (def == null) { b.multiplier = 1f; return b; }

            b.flatBonus = def.flatScoreBonusPercent;
            b.playerPercent = PosScoreFromPlayerDebuffs(AggregatePlayer(def.playerMods));
            b.enemiesPercentSum = SumEnemyGroups(def.enemyGroups);
            b.totalPercent = b.flatBonus + b.playerPercent + b.enemiesPercentSum;
            b.multiplier = 1f + (b.totalPercent / 100f);
            return b;
        }

        // ----- internal helpers -----
        private static Dictionary<PlayerStat, float> AggregatePlayer(List<PlayerStatMod> mods)
        {
            var agg = new Dictionary<PlayerStat, float>();
            if (mods == null) return agg;
            foreach (var m in mods)
            {
                if (agg.ContainsKey(m.stat)) agg[m.stat] += m.percentDelta;
                else agg[m.stat] = m.percentDelta;
            }
            return agg;
        }

        private static float PosScoreFromPlayerDebuffs(Dictionary<PlayerStat, float> mods)
        {
            mods.TryGetValue(PlayerStat.MaxHP,     out var hp);
            mods.TryGetValue(PlayerStat.ExpGain,   out var exp);
            mods.TryGetValue(PlayerStat.BaseDamage,out var dmg);
            mods.TryGetValue(PlayerStat.MoveSpeed, out var mspd);

            float p = 0f;
            p += Mathf.Max(0f, -hp)   * 1f; // -1% Max HP = +1%
            p += Mathf.Max(0f, -exp)  * 3f; // -1% EXP    = +3%
            p += Mathf.Max(0f, -dmg)  * 1f; // -1% DMG    = +1%
            p += Mathf.Max(0f, -mspd) * 2f; // -1% MSPD   = +2%
            return p;
        }

        private static float SumEnemyGroups(List<EnemyGroupMod> groups)
        {
            if (groups == null) return 0f;
            float sum = 0f;
            foreach (var g in groups)
            {
                if (g == null || g.stats.IsZero) continue;
                sum += Mathf.Max(0f, g.stats.maxHP)       * 0.5f; // +1% HP = +0.5%
                sum += Mathf.Max(0f, g.stats.damage)      * 0.5f; // +1% DMG = +0.5%
                sum += Mathf.Max(0f, g.stats.moveSpeed)   * 1.0f; // +1% MSPD = +1%
            }
            return sum;
        }
    }
}
