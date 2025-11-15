using System;
using System.Collections.Generic;
using System.Linq;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using UnityEngine;

namespace Characters.ComboSystems
{
    public class CombatRankSystem : MonoBehaviour
    {
        // ===== Events =====
        public event Action<int> OnRankPointAdded; // Added Amount (delta)
        public event Action<int> OnRankPointChanged; // Current Rank Point
        public event Action<string> OnRankChanged; // Current Rank Changed ID
        public event Action<string, string> OnRankUp; // (oldRankId, newRankId)
        public event Action<string, string> OnRankDown; // (oldRankId, newRankId)

        public event Action<int, int, int>
            OnUpdateRankPointProgression; // (CurrentThreshold, CurrentPoint, NextThreshold)

        public event Action<int> OnKillStrikeChanged; // Current KillStrike

        // ===== Data =====
        private List<CombatRankData> _rankDatas = new();
        private int _currentRankPoint;
        private string _currentRankId;
        private string _highestRecordedRankId;
        private int _killStrike;
        private float _rankPointMultiplier = 1f;
        private PlayerDataSo _ownerData;

        public int CurrentRankPoint => _currentRankPoint;
        public string CurrentRankId => _currentRankId;
        public string HighestRecordedRankId => _highestRecordedRankId;
        public int KillStrike => _killStrike;

        public void AssignRankData(PlayerDataSo ownerData)
        {
            _ownerData = ownerData;
            _rankDatas = ownerData.CombatRankDatas
                .OrderBy(r => r.rankPointThreshold)
                .ToList();

            _currentRankId = _rankDatas[0].rankId;
            _highestRecordedRankId = _rankDatas[0].rankId;
        }

        // Gain Rank Point Method
        private float _startTimeKill;
        private int deltaKillCountInTime;

        public void OnKillCondition(BaseController targetKilled)
        {
            AddKillStrike(1);
            AddRankPoints(_ownerData.KillConditionData.pointPerKill);

            var killConfig = _ownerData.KillConditionData;
            float duration = killConfig.killWithInDuration;
            int requiredKills = killConfig.killAmountToGainPoint;

            if (Time.time <= _startTimeKill + duration)
            {
                deltaKillCountInTime++;
            }
            else
            {
                _startTimeKill = Time.time;
                deltaKillCountInTime = 1;
            }

            if (deltaKillCountInTime >= requiredKills)
            {
                deltaKillCountInTime = 0;
                _startTimeKill = 0f;
                EnemyDataSo enemyData = targetKilled.CharacterData as EnemyDataSo;

                AddRankPoints(100);
            }
        }

        // Reduce Rank
        public void OnTakeDamageCondition(bool success)
        {
            if (!success) return;
            AddKillStrike(-_killStrike); // reset kill strike
        }

        private void AddRankPoints(int amount)
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

            int currentRankIndex = GetRankIndex(_currentRankId);
            int lastIndexPossible = _rankDatas.Count - 1;
            int nextRankIndex = currentRankIndex >= lastIndexPossible ? currentRankIndex : currentRankIndex + 1;

            OnUpdateRankPointProgression?.Invoke(_rankDatas[currentRankIndex].rankPointThreshold, _currentRankPoint,
                _rankDatas[nextRankIndex].rankPointThreshold);
        }

        private void AddKillStrike(int amount)
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
            OnRankChanged?.Invoke(newRankId);

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

        private int GetRankIndex(string rankId)
        {
            if (string.IsNullOrEmpty(rankId) || _rankDatas == null)
                return -1;

            return _rankDatas.FindIndex(r => r.rankId == rankId);
        }

        public void ResetCombatRankSystem(bool resetHighest = false)
        {
            /*_currentRankPoint = 0;
            OnRankPointChanged?.Invoke(_currentRankPoint);

            _killStrike = 0;
            OnKillStrikeChanged?.Invoke(_killStrike);

            if (!resetHighest) return;
            _highestRecordedRankId = _rankDatas[0].rankId;*/
        }
    }
}