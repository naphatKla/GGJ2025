using System;
using System.Collections.Generic;
using System.Linq;
using Characters.SO.CharacterDataSO;
using Manager;
using UnityEngine;

namespace Characters.ComboSystems
{
    public class FlowStateController : MonoBehaviour
    {
        private List<FlowStateData> _flowStateDatas = new List<FlowStateData>();
        private string _currentFlowStateId;
        private int _currentFlowingMind;
        private GameObject _owner;
        private PlayerDataSo _ownerData;
        private float _lastedTimeStackModify;
        
        public event Action<string> OnStateChanged; // string = Current Flow State 
        public float FlowScoreMultiplier { get; private set; } = 1f; // depends to rank point in combat rank system

        private void FixedUpdate()
        {
            // reduce stack every n second
            if (_currentFlowingMind <= 0) return;
            if (Time.time < _lastedTimeStackModify + _ownerData.FlowingMindLifeTimePerStack) return;
            AddFlowingMind(-1);
        }

        public void AssignData(GameObject owner, PlayerDataSo ownerData)
        {
            _owner = owner;
            _ownerData = ownerData;
            _flowStateDatas = ownerData.FlowStateDatas.OrderBy(f => f.flowingMindThreshold).ToList();
        }

        public void OnRankConditionTrigger(CombatRankConditionData conditionData)
        {
            AddFlowingMind(conditionData.flowingMindModify);
        }
        
        private void AddFlowingMind(int amount = 1)
        {
            if (amount == 0)
                return;

            _lastedTimeStackModify = Time.time;
            _currentFlowingMind = Mathf.Clamp(_currentFlowingMind + amount, 0, _ownerData.FlowingMindMaxCap);
            CalculateFlowState();
        }

        private void CalculateFlowState()
        {
            string oldStateId = _currentFlowStateId;
            string newStateId = null;

            foreach (var state in _flowStateDatas)
            {
                if (_currentFlowingMind < state.flowingMindThreshold)
                    break;

                newStateId = state.flowStateId;
            }

            if (newStateId == oldStateId)
                return;

            _currentFlowStateId = newStateId;
            OnStateChanged?.Invoke(newStateId);

            // effect apply
            int currentIndex = GetStateIndex(_currentFlowStateId);
            int oldIndex = GetStateIndex(oldStateId);

            if (oldIndex > 0)
                StatusEffectManager.RemoveEffectAt(_owner, _flowStateDatas[oldIndex].effectsApply);

            if (currentIndex > 0)
                StatusEffectManager.ApplyEffectTo(_owner, _flowStateDatas[currentIndex].effectsApply);
        }

        private int GetStateIndex(string stateId)
        {
            if (string.IsNullOrEmpty(stateId) || _flowStateDatas == null)
                return -1;

            return _flowStateDatas.FindIndex(f => f.flowStateId == stateId);
        }

        public void AddFlowScoreMultiplier(float amount)
        {
            FlowScoreMultiplier += amount;
        }
    }
}