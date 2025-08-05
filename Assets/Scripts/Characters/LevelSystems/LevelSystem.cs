using System;
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

        public event Action<int> OnLevelUp;

        private float _baseExp;
        private float _multiplier;
        private float _currentExpToLevelUp;

        // Assign base values
        public void AssignData(float baseExpLevelUp, float expMultiplierPerLevel)
        {
            _baseExp = baseExpLevelUp;
            _multiplier = expMultiplierPerLevel;
            UpdateExpToLevelUp();
        }

        [Button("Add Exp (Test)")]
        public void AddExp(int amount)
        {
            Exp += amount;
            Exp = Mathf.CeilToInt(Exp);

            while (Exp >= _currentExpToLevelUp)
            {
                Exp -= _currentExpToLevelUp;
                Level++;
                OnLevelUp?.Invoke(Level);
                UpdateExpToLevelUp();
            }
        }

        [Button("Reset Level")]
        public void ResetLevel()
        {
            Level = 1;
            Exp = 0;
            UpdateExpToLevelUp();
        }

        private void UpdateExpToLevelUp()
        {
            _currentExpToLevelUp = Mathf.Ceil(_baseExp * Mathf.Pow(_multiplier, Level - 1));
        }
    }
}