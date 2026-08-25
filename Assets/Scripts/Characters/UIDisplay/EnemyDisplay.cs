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

        [FoldoutGroup("Breakpoint Display")]
        [Title("Break Point Display (optional)")]
        [PropertyTooltip("แสดงเมื่อ enemy มี BreakPointSystem (เช่น boss Bright2) — ไม่ใส่ค่า = ไม่แสดง")]
        [SerializeField] private ValueBar breakPointBar;

        [FoldoutGroup("Breakpoint Display")]
        [SerializeField] private BreakPointSystem breakPointSystem;
        
        private void Start()
        {
            healthSystem.OnHealthChange += healthChange => UpdateHealthUI();

            // ----- Break Point bar (boss only) -----
            if (breakPointBar == null) return;
            if (breakPointSystem == null)
                breakPointSystem = GetComponent<BreakPointSystem>();

            if (breakPointSystem != null)
            {
                breakPointSystem.OnBreakPointChanged += (_, _) => UpdateBreakPointUI();
                breakPointSystem.OnBreakingStart += () => breakPointBar.FillImage.DOKill();
                UpdateBreakPointUI();
            }
            else
            {
                // enemy ธรรมดาไม่มี Break Point → ซ่อน bar ถาวร
                breakPointBar.gameObject.SetActive(false);
            }
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

        #region Break Point UI

        private void UpdateBreakPointUI()
        {
            if (breakPointBar == null || breakPointSystem == null) return;

            // โชว์ตั้งแต่ spawn — ให้ player รู้ตั้งแต่ต้นว่าบอสนี้มีกล Break Point
            breakPointBar.gameObject.SetActive(true);

            float bpAmount = breakPointSystem.BreakPointPercentage01 * 100f;
            breakPointBar.CurrentValue = (int)Mathf.Clamp(bpAmount, 0, 100);
        }

        #endregion
    }
}
