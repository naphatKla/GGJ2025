using System;
using System.Collections;
using System.Collections.Generic;
using Characters.HeathSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PixelUI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.UIDisplay
{
    public class EnemyDisplay : MonoBehaviour
    {
        [FoldoutGroup("Health Display")] [Title("Ref")]
        [SerializeField] private HealthSystem healthSystem;

        [FoldoutGroup("Health Display")] [Title("UI")] [SerializeField]
        private ValueBar hpBar;
        [FoldoutGroup("Health Display")]
        [SerializeField] private float feedbackDelay;
        
        private void Start()
        {
            healthSystem.OnHealthChange += healthChange => UpdateHealthUI();
        }
        
        private void OnDestroy()
        {
            healthSystem.OnHealthChange -= healthChange => UpdateHealthUI();
        }

        private void OnEnable()
        {
            UpdateHealthUI();
        }

        #region Health UI

        private void UpdateHealthUI()
        {
            hpBar.gameObject.SetActive(healthSystem.CurrentHealth < healthSystem.MaxHealth);
            HealthFeedback();
            float hpAmount = (healthSystem.CurrentHealth / healthSystem.MaxHealth) * 100;
            hpBar.CurrentValue = (int)Mathf.Clamp(hpAmount, 0, 100);
        }

        private void HealthFeedback()
        {
            if (hpBar == null) return;
            hpBar.FillImage.DOKill();
            hpBar.FillImage.color = Color.white;
            hpBar.FillImage.DOColor(Color.green, 0.2f).SetDelay(feedbackDelay);
        }

        #endregion
    }
}
