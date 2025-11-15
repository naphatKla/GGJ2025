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
        
        public event Action<string> OnStateChanged; // string = Current Flow State 

        public void AssignData(GameObject owner, PlayerDataSo ownerData)
        {
            _owner = owner;
            _ownerData = ownerData;
            _flowStateDatas = ownerData.FlowStateDatas.OrderBy(f => f.flowingMindThreshold).ToList();
        }
        
        public void OnRankPointAdd(int point)
        {
            AddFlowingMind(_ownerData.FlowingMindGainAmount);
        }

        public void OnTakeDamage(bool success)
        {
            if (!success) return;
            AddFlowingMind(-_ownerData.FlowingMindReduceOnTakeDamage);
        }
        
        private void AddFlowingMind(int amount = 1)
        {
            if (amount == 0)
                return;

            _currentFlowingMind = Mathf.Max(0, _currentFlowingMind + amount);
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
    }
}