using System.Collections.Generic;
using Characters.Controllers;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Characters.ComboSystems
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
    public class ComboSystem : MonoBehaviour
    {
        #region UI Fields

        [FoldoutGroup("UI")] public GameObject comboUI;
        [FoldoutGroup("UI")] public TMP_Text comboStreakText;
        [FoldoutGroup("UI")] public TMP_Text scoreMultiply;
        [FoldoutGroup("UI")] public Slider comboTimeoutSlider;
        [FoldoutGroup("UI")] public float tweenDuration = 0.1f;
        [FoldoutGroup("UI")] public float scaleAmount = 1.2f;

        #endregion

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

        private void Update()
        {
            UpdateScoreText();
            UpdateComboTime();
            UpdateComboUI();

            if (_IsCombo && _currentTime <= 0)
                ResetCombo();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a hit from a valid target and updates combo state.
        /// </summary>
        public void RegisterHit()
        {
            if (!_IsCombo)
                OnComboStart();

            PlayTween();
            _comboStreak += _comboNumber * _comboMultiplyer;
            _comboStreak = Mathf.Round(_comboStreak);
            _currentTime = _comboTimeStart;
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
            // triggeredMilestones.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Plays a UI tween animation when a combo is triggered.
        /// </summary>
        private void PlayTween()
        {
            if (comboUI == null) return;
            if (_comboStreak > 0)
            {
                comboUI.transform.DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => comboUI.transform.DOScale(Vector3.one, tweenDuration));
            }
        }

        /// <summary>
        /// Updates the text showing current combo streak.
        /// </summary>
        private void UpdateScoreText()
        {
            if (comboStreakText != null)
                comboStreakText.text = "Combo " + _comboStreak;
        }

        /// <summary>
        /// Called when a combo sequence starts.
        /// </summary>
        private void OnComboStart()
        {
            if (comboStreakText == null) return;
            if (_IsCombo) return;

            _IsCombo = true;
            comboTimeoutSlider.maxValue = _comboTimeStart;
            _currentTime = _comboTimeStart;
        }

        /// <summary>
        /// Enables or disables the combo UI based on current combo state.
        /// </summary>
        private void UpdateComboUI()
        {
            if (comboUI != null)
                comboUI.SetActive(_IsCombo);
        }

        /// <summary>
        /// Updates the remaining combo time and decay.
        /// </summary>
        private void UpdateComboTime()
        {
            if (comboStreakText == null) return;
            if (_IsCombo && comboToDecayRate != null)
            {
                _currentDecayRate = comboToDecayRate.Evaluate(_comboStreak);
                _currentTime -= _currentDecayRate * Time.deltaTime;
                comboTimeoutSlider.value = _currentTime;
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
