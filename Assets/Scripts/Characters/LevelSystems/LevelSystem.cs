using System;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.LevelSystems
{
    public class LevelSystem : MonoBehaviour
    {
        [Title("Read-only Data")]
        [ShowInInspector, ReadOnly] public int Level { get; private set; } = 1;
        [ShowInInspector, ReadOnly] public float Exp { get; private set; } = 0;
        [ShowInInspector, ReadOnly] public float ExpToLevelUp => _currentExpToLevelUp;
        [ShowInInspector, ReadOnly] public float ExpProgress01 => Mathf.Clamp01(Exp / _currentExpToLevelUp);
        private BaseController _owner;
        private bool _active = true;

        public event Action<int> OnLevelUp;
        public event Action OnLevelUpdate;

        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        private float _baseExp;
        private float _stepThreshold;
        private float _stepValue;
        private float _currentExpToLevelUp;

        // Assign base values
        public void AssignData(BaseController owner, float baseExpLevelUp, float stepThreshold, float stepValue)
        {
            _baseExp = baseExpLevelUp;
            _stepThreshold = stepThreshold;
            _stepValue = stepValue;
            _owner = owner;
            UpdateExpToLevelUp();
        }

        [Button("Add Exp (Test)")]
        public void AddExp(int amount)
        {
            if (!_active) return;
            
            Exp += amount;
            Exp = Mathf.CeilToInt(Exp);

            while (Exp >= _currentExpToLevelUp)
            {
                Exp -= _currentExpToLevelUp;
                Level++;
                OnLevelUp?.Invoke(Level);
                UpdateExpToLevelUp();
            }
            OnLevelUpdate?.Invoke();
        }

        [Button("Reset Level")]
        public void ResetLevel()
        {
            Level = 1;
            Exp = 0;
            _active = true;
            UpdateExpToLevelUp();
        }
        
        public void ForceLevelUp()
        {
            AddExp( Mathf.RoundToInt(_currentExpToLevelUp) );
        }

        protected virtual void UpdateExpToLevelUp()
        {
            float multiplier = Mathf.Floor(((Level - 1) / _stepThreshold));
            _currentExpToLevelUp = _baseExp + (_stepValue * multiplier);
            
            if (Level == 1) return;
            _owner.TryPlayFeedback(FeedbackName.Character.LevelUp);
        }
    }
}