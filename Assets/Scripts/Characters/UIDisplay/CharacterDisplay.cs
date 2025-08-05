using System;
using System.Collections;
using System.Collections.Generic;
using Characters.ComboSystems;
using Characters.HeathSystems;
using Characters.LevelSystems;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Characters.UIDisplay
{
    public class CharacterDisplay : MonoBehaviour
    {
        [FoldoutGroup("Combo Display")]
        [Title("Ref")]
        [SerializeField] public ComboSystem comboSystem;
        [Title("UI")]
        [FoldoutGroup("Combo Display")] public GameObject comboUI;
        [FoldoutGroup("Combo Display")] public TMP_Text comboStreakText;
        [FoldoutGroup("Combo Display")] public TMP_Text scoreMultiply;
        [FoldoutGroup("Combo Display")] public Slider comboTimeoutSlider;
        [FoldoutGroup("Combo Display")] public float tweenDuration = 0.1f;
        [FoldoutGroup("Combo Display")] public float scaleAmount = 1.2f;
        
        [FoldoutGroup("Level Display")]
        [Title("Ref")]
        [SerializeField] public LevelSystem levelSystem;
        [Title("UI")] 
        [FoldoutGroup("Level Display")] public TMP_Text levelText;
        [FoldoutGroup("Level Display")] public Image levelImage;
        
        [FoldoutGroup("Health Display")]
        [Title("Ref")]
        [SerializeField] public HealthSystem healthSystem;
        [Title("UI")]
        [FoldoutGroup("Health Display")] [SerializeField] public Scrollbar hpBar;
        
        private void Start()
        {
            ComboUISetup();
            comboSystem.OnComboUpdated += UpdateComboScoreText;
        }
        
        private void OnDestroy()
        {
            comboSystem.OnComboUpdated -= UpdateComboScoreText;
        }

        private void Update()
        {
            UpdateComboUI();
            UpdateLevelUI();
            UpdateHealthUI();
        }

        #region Combo UI
        private void ComboUISetup()
        {
            comboTimeoutSlider.value = comboSystem.ComboStartValue;
            comboTimeoutSlider.maxValue = comboSystem.ComboStartValue;
        }
        
        private void UpdateComboUI()
        {
            if (!comboTimeoutSlider && !comboUI) return;
            comboUI.SetActive(comboSystem.ComboActive);
            comboTimeoutSlider.value = comboSystem.CurrentComboTime;
        }
        
        private void UpdateComboScoreText(float streak)
        {
            comboUI.SetActive(true);
            if (comboStreakText != null)
                comboStreakText.text = $"Combo {streak}";

            comboUI.transform.DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => comboUI.transform.DOScale(Vector3.one, tweenDuration));
        }
        #endregion
        
        #region Health UI
        

        private void UpdateHealthUI()
        {
            float hpAmount = healthSystem.CurrentHealth / healthSystem.MaxHealth;
            hpBar.size = Mathf.Clamp01(hpAmount);
        }
        
        #endregion

        #region Level UI

        private void UpdateLevelUI()
        {
            levelText.text = levelSystem.Level.ToString();
            float fillAmount = levelSystem.ExpProgress01;
            levelImage.fillAmount = Mathf.Clamp01(fillAmount);
        }

        #endregion
    }
}
