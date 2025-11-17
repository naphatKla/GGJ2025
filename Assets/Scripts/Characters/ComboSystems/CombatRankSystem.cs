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
        #region Events

        public event Action<int> OnRankPointAdded; // Added Amount (delta)
        public event Action<CombatRankConditionData> OnRankConditionTrigger;
        public event Action<CombatRankData, CombatRankData> OnRankChanged; // (old, new)

        public event Action<int, int, int>
            OnUpdateRankPointProgression; // (CurrentThreshold, CurrentPoint, NextThreshold)

        public event Action<int> OnKillStrikeChanged; // Current KillStrike

        #endregion

        #region Variables

        [SerializeField] private FlowStateController flowStateController;
        
        private List<CombatRankData> _rankDatas = new();
        private int _currentRankPoint;
        private string _currentRankId;
        private string _highestRecordedRankId;
        private int _killStrike;
        private PlayerDataSo _ownerData;
        private const float _localRankPointMultiplier = 1f;

        public int CurrentRankPoint => _currentRankPoint;
        public string CurrentRankId => _currentRankId;
        public string HighestRecordedRankId => _highestRecordedRankId;
        public int KillStrike => _killStrike;
        public float RankPointMultiplier => _localRankPointMultiplier * flowStateController.FlowScoreMultiplier;

        #endregion

        #region Methods

        public void AssignRankData(PlayerDataSo ownerData)
        {
            _ownerData = ownerData;
            _rankDatas = ownerData.CombatRankDatas
                .OrderBy(r => r.rankPointThreshold)
                .ToList();

            _currentRankId = _rankDatas[0].rankId;
            _highestRecordedRankId = _rankDatas[0].rankId;
        }

        private void AddRankPoints(int amount)
        {
            if (amount == 0)
                return;

            int oldPoint = _currentRankPoint;
            int calculatedAmount = Mathf.CeilToInt(amount * RankPointMultiplier);

            _currentRankPoint = Mathf.Clamp(_currentRankPoint + calculatedAmount, 0, _rankDatas[^1].rankPointThreshold);

            int delta = _currentRankPoint - oldPoint;
            if (delta == 0)
                return;

            OnRankPointAdded?.Invoke(delta);

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

            int oldIndex = GetRankIndex(oldRankId);
            int newIndex = GetRankIndex(newRankId);
            int highestIndex = GetRankIndex(_highestRecordedRankId);

            _currentRankId = newRankId;
            OnRankChanged?.Invoke(_rankDatas[oldIndex], _rankDatas[newIndex]);

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

        #endregion

        #region Condition Event Methods

        // Gain Rank Point Method
        private float _startTimeKill;
        private int deltaKillCountInTime;
        private int deltaScore;

        public void OnKillCondition(BaseController targetKilled)
        {
            AddKillStrike(1);
            
            AddRankPoints(_ownerData.SingleKillConditionData.pointAddedPerKill);
            OnRankConditionTrigger?.Invoke(_ownerData.SingleKillConditionData);

            var killConfig = _ownerData.GroupKillConditionData;
            float duration = killConfig.killWithInDuration;
            int requiredKills = killConfig.killAmountToGainPoint;

            var enemyData = targetKilled.CharacterData as EnemyDataSo;
            if (enemyData == null) return;

            if (Time.time <= _startTimeKill + duration)
            {
                deltaKillCountInTime++;
                deltaScore += enemyData.ScoreDrop;
            }
            else
            {
                _startTimeKill = Time.time;
                deltaKillCountInTime = 1;
                deltaScore = enemyData.ScoreDrop;
            }

            if (deltaKillCountInTime >= requiredKills)
            {
                int calculatedPoint =
                    Mathf.CeilToInt(deltaScore * (_ownerData.GroupKillConditionData.scorePercentage / 100));
                deltaKillCountInTime = 0;
                deltaScore = 0;
                _startTimeKill = 0f;

                AddRankPoints(calculatedPoint);
                OnRankConditionTrigger?.Invoke(_ownerData.GroupKillConditionData);
            }
        }

        public void OnParrySuccessCondition(bool isPerfect, int damageNegate)
        {
            float calculateMultiplier = isPerfect
                ? _ownerData.ParryConditionData.perfectParryPercentage / 100
                : _ownerData.ParryConditionData.normalParryPercentage / 100;

            int calculatedPoint = Mathf.CeilToInt(damageNegate * calculateMultiplier);
            
            AddRankPoints(calculatedPoint);
            OnRankConditionTrigger?.Invoke(_ownerData.ParryConditionData);
        }

        public void OnCounterDashCondition(int damageNegate)
        {
            int calculatedPoint = Mathf.CeilToInt(damageNegate * (_ownerData.CounterDashConditionData.counterDashPercentage/100));
            AddRankPoints(calculatedPoint);
            OnRankConditionTrigger?.Invoke(_ownerData.CounterDashConditionData);
        }

        public void OnHealCondition(int healAmount)
        {
            int calculatedPoint = Mathf.CeilToInt(healAmount * (_ownerData.HealConditionData.healPercentage / 100));
            AddRankPoints(calculatedPoint);
            OnRankConditionTrigger?.Invoke(_ownerData.HealConditionData);
        }

        // Reduce Rank
        public void OnTakeDamageCondition()
        {
            int calculatedPoint =
                Mathf.CeilToInt(_currentRankPoint * (_ownerData.TakeDamageConditionData.lostPointPercentage / 100));
            AddKillStrike(-_killStrike); // reset kill strike
            AddRankPoints(-calculatedPoint);
            OnRankConditionTrigger?.Invoke(_ownerData.TakeDamageConditionData);
        }

        //public void OnBuff
        //public void OnDeBuff

        #endregion
    }
}