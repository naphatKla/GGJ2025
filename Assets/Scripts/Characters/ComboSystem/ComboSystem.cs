using System;
using System.Collections.Generic;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Characters.ComboSystem
{
    /// <summary>
    /// Represents a combo milestone and its associated UnityEvent when reached.
    /// </summary>
    [System.Serializable]
    public class ComboMilestoneEvent
    {
        public int milestone;
        public UnityEvent onReached;
    }

    /// <summary>
    /// Handles combo logic including streaks, decay, milestones, and UI feedback.
    /// </summary>
    public class ComboSystem : MonoBehaviour, IFixedUpdateable
    {

        #region Combo Settings

        [FoldoutGroup("Combo Setting")] [SerializeField]
        private float _comboNumber = 1f;

        [FoldoutGroup("Combo Setting")] [SerializeField]
        private float _comboMultiplyer = 1.0f;

        [FoldoutGroup("Combo Setting")] [SerializeField]
        private float _comboTimeStart;

        [FoldoutGroup("Combo Setting")] [SerializeField]
        private AnimationCurve comboToDecayRate;

        [FoldoutGroup("Combo Setting")]
        [LabelText("Combo Milestones Events")]
        [SerializeField]
        private List<ComboMilestoneEvent> comboMilestoneEvents = new();
        public event Action<float> OnComboUpdated;
        public event Action<float> OnComboTimeUpdated;

        public bool ComboActive => _IsCombo;
        public float CurrentComboTime => _currentTime;
        public float ComboStartValue => _comboTimeStart;
        #endregion

        #region Runtime State

        [ShowInInspector] [ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _comboStreak;

        [ShowInInspector] [ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentTime;

        [ShowInInspector] [ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentDecayRate;

        private bool _IsCombo;
        private HashSet<int> triggeredMilestones = new HashSet<int>();

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
        }

        public void OnFixedUpdate()
        {
            UpdateComboTime();

            if (_IsCombo && _currentTime <= 0) ResetCombo();
        }
        
        private void OnDisable()
        {
            FixedUpdateManager.Instance.Unregister(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a hit from a valid target and updates combo state.
        /// </summary>
        public void RegisterHit(DamageData damageData)
        {
            if (!_IsCombo)
            {
                _IsCombo = true;
                _currentTime = _comboTimeStart;
            }

            _comboStreak += _comboNumber * _comboMultiplyer;
            _comboStreak = Mathf.Round(_comboStreak);
            _currentTime = _comboTimeStart;
            OnComboUpdated?.Invoke(_comboStreak);

            CheckComboMilestones();
        }

        /// <summary>
        /// Resets the combo system to its default state.
        /// </summary>
        public void ResetCombo()
        {
            _IsCombo = false;
            _comboStreak = 0;
            _currentTime = 0;
            
            OnComboTimeUpdated?.Invoke(_currentTime);
            OnComboUpdated?.Invoke(_comboStreak); 
        }

        #endregion

        #region Private Methods

        private void UpdateComboTime()
        {
            if (_IsCombo && comboToDecayRate != null)
            {
                _currentDecayRate = comboToDecayRate.Evaluate(_comboStreak);
                _currentTime -= _currentDecayRate * Time.fixedDeltaTime;
                _currentTime = Mathf.Max(_currentTime, 0);

                OnComboTimeUpdated?.Invoke(_currentTime);
            }
        }

        
        /// <summary>
        /// Checks and triggers combo milestone events if conditions are met.
        /// </summary>
        private void CheckComboMilestones()
        {
            foreach (var entry in comboMilestoneEvents)
            {
                if (_comboStreak >= entry.milestone /*&& !triggeredMilestones.Contains(entry.milestone)*/)
                {
                    // triggeredMilestones.Add(entry.milestone);
                    entry.onReached?.Invoke();
                }
            }
        }

        #endregion
    }
}
