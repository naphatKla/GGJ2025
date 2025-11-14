using System;
using System.Collections.Generic;
using System.Linq;
using Characters.SO.CharacterDataSO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.ComboSystems
{
    public class CombatRankSystem : MonoBehaviour
    {
        // ===== Events =====
        public event Action<int> OnRankPointAdded;      // Added Amount (delta)
        public event Action<int> OnRankPointChanged;    // Current Rank Point
        public event Action<string, string> OnRankChanged; // (oldRankId, newRankId)
        public event Action<string, string> OnRankUp;      // (oldRankId, newRankId)
        public event Action<string, string> OnRankDown;    // (oldRankId, newRankId)
        public event Action<int> OnKillStrikeChanged;   // Current KillStrike

        // ===== Data =====
        private List<CombatRankData> _rankDatas = new();
        private int _currentRankPoint;
        private string _currentRankId;
        private string _highestRecordedRankId;
        private int _killStrike;

        public int CurrentRankPoint => _currentRankPoint;
        public string CurrentRankId => _currentRankId;
        public string HighestRecordedRankId => _highestRecordedRankId;
        public int KillStrike => _killStrike;

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
           
        }
        
        public void AssignRankData(List<CombatRankData> rankDatas)
        {
            _rankDatas = rankDatas
                .OrderBy(r => r.rankPointThreshold)
                .ToList();

            _currentRankId = _rankDatas[0].rankId;
            _highestRecordedRankId = _rankDatas[0].rankId;
        }
        
        [Button]
        public void AddRankPoints(int amount)
        {
            if (amount == 0)
                return;

            int oldPoint = _currentRankPoint;
            _currentRankPoint = Mathf.Max(0, _currentRankPoint + amount);

            int delta = _currentRankPoint - oldPoint;
            if (delta == 0)
                return;

            OnRankPointAdded?.Invoke(delta);
            OnRankPointChanged?.Invoke(_currentRankPoint);

            CalculateRank();
        }

        [Button]
        public void AddKillStrike(int amount)
        {
            if (amount == 0)
                return;

            int old = _killStrike;
            _killStrike = Mathf.Max(0, _killStrike + amount);

            if (_killStrike == old)
                return;

            OnKillStrikeChanged?.Invoke(_killStrike);
        }
        
        private void CalculateRank()
        {
            string oldRankId = _currentRankId;
            string newRankId = null;

            // calculate rank 
            foreach (var rank in _rankDatas)
            {
                if (_currentRankPoint < rank.rankPointThreshold)
                    break;

                newRankId = rank.rankId;
            }

            if (newRankId == oldRankId)
                return;

            _currentRankId = newRankId;
            OnRankChanged?.Invoke(oldRankId, newRankId);

            int oldIndex     = GetRankIndex(oldRankId);
            int newIndex     = GetRankIndex(newRankId);
            int highestIndex = GetRankIndex(_highestRecordedRankId);

            if (newIndex > oldIndex)
                OnRankUp?.Invoke(oldRankId, newRankId);
            else if (newIndex < oldIndex)
                OnRankDown?.Invoke(oldRankId, newRankId);

            if (newIndex > highestIndex)
                _highestRecordedRankId = newRankId;
        }
        
        private int GetRankIndex(string rankId)
        {
            if (string.IsNullOrEmpty(rankId) || _rankDatas == null)
                return -1;

            return _rankDatas.FindIndex(r => r.rankId == rankId);
        }
        
        public void ResetCombatRankSystem(bool resetHighest = false)
        {
            _currentRankPoint = 0;
            OnRankPointChanged?.Invoke(_currentRankPoint);
            
            _killStrike = 0;
            OnKillStrikeChanged?.Invoke(_killStrike);
            
            if (!resetHighest) return;
            _highestRecordedRankId = _rankDatas[0].rankId;
        }
    }
}
