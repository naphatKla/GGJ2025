using System.Collections.Generic;
using UnityEngine;

namespace Characters.Data
{
    public class PlayerSummaryStats
    {
        public int totalScore;
        //public int currency;
        
        public int currentLevel;
        
        public string currentRank;
        public string highestRank;

        public Dictionary<string, int> totalKillDictionary;
        public int totalEnemiesEliminated;
        public int totalDamageDeal;
        public int criticalCount;
        public int totalCounterDashCount;
        
        public int totalPrimarySkillUsed;
        public int totalSecondarySkillUsed;
        public int totalAutoSkillUsed;
        
        public int totalDamageTaken;
        public int totalHeal;

        public void DebugStat()
        {
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine($"total score : {totalScore}");
            sb.AppendLine($"current level : {currentLevel}");
            sb.AppendLine($"highest rank : {(string.IsNullOrEmpty(currentRank) ? "-" : currentRank)}");
            sb.AppendLine($"highest rank : {(string.IsNullOrEmpty(highestRank) ? "-" : highestRank)}");
            sb.AppendLine($"total enemies eliminated : {totalEnemiesEliminated}");
            sb.AppendLine($"total damage deal : {totalDamageDeal}");
            sb.AppendLine($"critical count : {criticalCount}");
            sb.AppendLine($"total counter dash count : {totalCounterDashCount}");
            sb.AppendLine($"total primary skill used : {totalPrimarySkillUsed}");
            sb.AppendLine($"total secondary skill used : {totalSecondarySkillUsed}");
            sb.AppendLine($"total auto skill used : {totalAutoSkillUsed}");
            sb.AppendLine($"total damage taken : {totalDamageTaken}");
            sb.AppendLine($"total heal : {totalHeal}");
            Debug.Log(sb.ToString());
        }

    }
}
