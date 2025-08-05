using Characters.CombatSystems;
using Characters.HeathSystems;
using Characters.LevelSystems;
using DG.Tweening;
using Manager;
using PixelUI;
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
        [SerializeField] public ComboSystem.ComboSystem comboSystem;
        [Title("UI")]
        [FoldoutGroup("Combo Display")] public GameObject comboUI;
        [FoldoutGroup("Combo Display")] public TMP_Text comboStreakText;
        [FoldoutGroup("Combo Display")] public TMP_Text scoreMultiply;
        [FoldoutGroup("Combo Display")] public Slider comboTimeoutSlider;
        [FoldoutGroup("Combo Display")] public float tweenDuration = 0.1f;
        [FoldoutGroup("Combo Display")] public float scaleAmount = 1.2f;

        
        [FoldoutGroup("Combat Display")] 
        [SerializeField] private CombatSystem combatSystem;
        [FoldoutGroup("Combat Display")] 
        [SerializeField]private TextMeshProUGUI damageTextPrefab;
        
        [FoldoutGroup("Level Display")]
        [Title("Ref")]
        [SerializeField] public LevelSystem levelSystem;
        [Title("UI")] 
        [FoldoutGroup("Level Display")] public TMP_Text levelText;
        [FoldoutGroup("Level Display")] public ValueBar levelbar;
        
        [FoldoutGroup("Health Display")]
        [Title("Ref")]
        [SerializeField] public HealthSystem healthSystem;
        [Title("UI")]
        [FoldoutGroup("Health Display")] [SerializeField] public Scrollbar hpBar;
        
        private void Start()
        {
            ComboUISetup();
            comboSystem.OnComboUpdated += UpdateComboScoreText;
            combatSystem.OnDealDamage += UpdateDamageText;
            PoolingManager.Instance.Create<TextMeshProUGUI>(damageTextPrefab.name, PoolingGroupName.UI, CreateDamageText);
        }
        
        private void OnDestroy()
        {
            comboSystem.OnComboUpdated -= UpdateComboScoreText;
            combatSystem.OnDealDamage -= UpdateDamageText;
            PoolingManager.Instance.ClearPool(damageTextPrefab.name);
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

        #region Combat UI

        private TextMeshProUGUI CreateDamageText()
        {
            return Instantiate(damageTextPrefab);
        }

        private void UpdateDamageText(DamageData damageData)
        {
            var textInstance = PoolingManager.Instance.Get<TextMeshProUGUI>(damageTextPrefab.name);
            textInstance.text = damageData.Damage.ToString();
            textInstance.transform.position = damageData.HitPosition;
            textInstance.gameObject.SetActive(true);
         
            DOVirtual.DelayedCall(0.7f, () =>
            {
                PoolingManager.Instance.Release(damageTextPrefab.name, textInstance);
            });
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
            float fillAmount = levelSystem.ExpProgress01 * 100;
            levelbar.CurrentValue = Mathf.Clamp(fillAmount, 0, 100);
        }

        #endregion
    }
}
