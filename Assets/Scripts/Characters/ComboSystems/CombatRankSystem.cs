using System;
using System.Collections.Generic;
using System.Linq;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.ComboSystems
{
    public class CombatRankSystem : MonoBehaviour
    {
        // ===== Events =====
        public event Action<int> OnRankPointAdded; // Added Amount
        public event Action<int> OnRankPointChanged; // Current Rank Point
        public event Action<string, string> OnRankChanged; // (oldRankId, newRankId)
        public event Action<string, string> OnRankUp; // (oldRankId, newRankId)
        public event Action<string, string> OnRankDown; // (oldRankId, newRankId)

        // ===== Data =====
        private List<CombatRankData> _rankDatas = new();
        private int _currentRankPoint = 0;
        private string _currentRankId = null;
        private string _highestRecordedRankId = null;

        public int CurrentRankPoint => _currentRankPoint;
        public string CurrentRankId => _currentRankId;
        public string HighestRecordedRankId => _highestRecordedRankId;

        public void AssignRankData(List<CombatRankData> rankDatas)
        {
            _rankDatas = rankDatas
                .OrderBy(r => r.rankPointThreshold)
                .ToList();
        }

        private int GetRankIndex(string rankId)
        {
            if (string.IsNullOrEmpty(rankId) || _rankDatas == null)
                return -1;

            return _rankDatas.FindIndex(r => r.rankId == rankId);
        }

        private void CalculateRank()
        {
            if (_rankDatas == null || _rankDatas.Count == 0)
            {
                Debug.LogWarning("Rank data are not assigned");
                return;
            }

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

            int oldIndex = GetRankIndex(oldRankId);
            int newIndex = GetRankIndex(newRankId);
            int highestIndex = GetRankIndex(_highestRecordedRankId);

            if (newIndex > oldIndex)
                OnRankUp?.Invoke(oldRankId, newRankId);
            else if (newIndex < oldIndex)
                OnRankDown?.Invoke(oldRankId, newRankId);

            if (newIndex > highestIndex)
                _highestRecordedRankId = newRankId;
        }

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
        
        public void ResetCombatRankSystem(bool resetHighest = false)
        {
            _currentRankPoint = 0;
            OnRankPointChanged?.Invoke(_currentRankPoint);

            _currentRankId = null;
            CalculateRank();

            if (resetHighest)
                _highestRecordedRankId = null;
        }
    }
}