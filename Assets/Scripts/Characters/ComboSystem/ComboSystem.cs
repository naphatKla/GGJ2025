using System.Collections.Generic;
using Characters.Controllers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Characters.ComboSystems
{
    [System.Serializable]
    public class ComboMilestoneEvent
    {
        public int milestone;
        public UnityEvent onReached;
    }
    
    public class ComboSystem : MonoBehaviour
    {
        [FoldoutGroup("UI")] public GameObject comboUI;
        [FoldoutGroup("UI")] public TMP_Text comboStreakText;
        [FoldoutGroup("UI")] public TMP_Text scoreMultiply;
        [FoldoutGroup("UI")] public Slider comboTimeoutSlider;

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


        [ShowInInspector] [ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _comboStreak;

        [ShowInInspector] [ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentTime;

        [ShowInInspector] [ReadOnly] [ShowIf("@UnityEngine.Application.isPlaying")]
        private float _currentDecayRate;
        
        private bool _IsCombo;
        private HashSet<int> triggeredMilestones = new HashSet<int>();

        private void Update()
        {
            UpdateScoreText();
            UpdateComboTime();
            UpdateComboUI();
            if (_IsCombo && _currentTime <= 0) ResetCombo();
        }

        public void RegisterHit(BaseController target)
        {
            if (!_IsCombo) OnComboStart();

            _comboStreak += _comboNumber * _comboMultiplyer;
            _comboStreak = Mathf.Round(_comboStreak);
            _currentTime = _comboTimeStart;
            CheckComboMilestones();
        }


        public void ResetCombo()
        {
            _IsCombo = false;
            _comboStreak = 0;
            _currentTime = 0;
            triggeredMilestones.Clear();
        }


        private void UpdateScoreText()
        {
            if (comboStreakText != null) comboStreakText.text = "Combo " + _comboStreak;
        }

        private void OnComboStart()
        {
            if (_IsCombo) return;
            _IsCombo = true;
            comboTimeoutSlider.maxValue = _comboTimeStart;
            _currentTime = _comboTimeStart;
        }

        private void UpdateComboUI()
        {
            if (_IsCombo && comboUI != null)
                comboUI.SetActive(true);
            else
                comboUI.SetActive(false);
        }

        private void UpdateComboTime()
        {
            if (_IsCombo && comboToDecayRate != null)
            {
                _currentDecayRate = comboToDecayRate.Evaluate(_comboStreak);
                _currentTime -= _currentDecayRate * Time.deltaTime;
                comboTimeoutSlider.value = _currentTime;
            }
        }
        
        private void CheckComboMilestones()
        {
            foreach (var entry in comboMilestoneEvents)
            {
                if (_comboStreak >= entry.milestone && !triggeredMilestones.Contains(entry.milestone))
                {
                    triggeredMilestones.Add(entry.milestone);
                    entry.onReached?.Invoke();
                }
            }
        }

    }
}