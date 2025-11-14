using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.ComboSystems
{
    public class CombatRankSystem : MonoBehaviour
    {
        private Dictionary<CombatRankID, CombatRankData> _rankDataMaps;
        private CombatRankID _currentRankID = CombatRankID.None;
        private int _rankPoint = 0;

        public void AssignRankData(PlayerController owner, Dictionary<CombatRankID, CombatRankData> rankDataMap)
        {
            _rankDataMaps = rankDataMap;
        }

        private void CalculateRank()
        {
            CombatRankID bestRank = CombatRankID.None;
            float bestThreshold = float.MinValue;

            foreach (var kvp in _rankDataMaps)
            {
                var rankId = kvp.Key;
                var threshold = kvp.Value.rankPointThreshold;
                
                if (_rankPoint < threshold) continue;
                if (threshold <= bestThreshold) continue;
                
                bestThreshold = threshold;
                bestRank = rankId;
            }

            _currentRankID = bestRank;
        }
        
        private void AddRankPoint()
        {
            
        }

        private void ReduceRankPoint()
        {
            
        }
        
        private void ResetToUnRank()
        {
            
        }
        public void ResetCombatRankSystem()
        {
            
        }
    }
}
