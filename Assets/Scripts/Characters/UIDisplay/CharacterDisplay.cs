using System.Collections.Generic;
using Characters.CombatSystems;
using Characters.Controllers;
using Characters.HeathSystems;
using Characters.LevelSystems;
using Characters.SkillSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.Controller;
using Manager;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using PixelUI;
using Sirenix.OdinInspector;
using TMPro;
using UI.IngameModal;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VHierarchy.Libs;

namespace Characters.UIDisplay
{
    public class CharacterDisplay : MonoBehaviour
    {
        //Combo
        [FoldoutGroup("Combo Display")] [Title("Ref")] [SerializeField]
        public ComboSystem.ComboSystem comboSystem;

        [Title("UI")] [FoldoutGroup("Combo Display")]
        public GameObject comboUI;

        [FoldoutGroup("Combo Display")] public TMP_Text comboStreakText;
        [FoldoutGroup("Combo Display")] public TMP_Text scoreMultiply;
        [FoldoutGroup("Combo Display")] public ValueBar comboTimeoutBar;
        [FoldoutGroup("Combo Display")] public float tweenDuration = 0.1f;
        [FoldoutGroup("Combo Display")] public float scaleAmount = 1.2f;
        
        //Combat
        [FoldoutGroup("Combat Display")] [SerializeField]
        private CombatSystem combatSystem;

        [FoldoutGroup("Combat Display")] [SerializeField]
        private TextMeshProUGUI damageTextPrefab;

        //Level
        [FoldoutGroup("Level Display")] [Title("Ref")] [SerializeField]
        public LevelSystem levelSystem;

        [Title("UI")] [FoldoutGroup("Level Display")]
        public TMP_Text levelText;

        [FoldoutGroup("Level Display")] public ValueBar levelbar;

        //Health
        [FoldoutGroup("Health Display")] [Title("Ref")] [SerializeField]
        public HealthSystem healthSystem;

        [Title("UI")] [FoldoutGroup("Health Display")] [SerializeField]
        public SlotBar hpBar;
        
        //Solf Upgrade
        [FoldoutGroup("SolfUpgrade Display")]
        [Title("Ref")] [SerializeField]
        public SkillUpgradeController skillUpgradeController;
        
        [FoldoutGroup("SolfUpgrade Display")] [Title("UI")] 
        [FoldoutGroup("SolfUpgrade Display")]
        public GameObject solfUpgradePanel;
        [FoldoutGroup("SolfUpgrade Display")]
        public SolfUpgradeModel solfUpgradeModel;
        
        private Queue<BaseSkillDataSo> skillQueue = new();
        private bool isChoosingSkill = false;
        
        //Skill Slot
        [FoldoutGroup("SkillSlot Display")]
        [Title("Ref")] [SerializeField]
        public SkillSystem skillSystem;
        
        [FoldoutGroup("SkillSlot Display")] [Title("UI")] 
        [FoldoutGroup("SkillSlot Display")] [SerializeField] private List<SkillSlotModel> skillSlotModel;

        private void Start()
        {
            PlayerController.Instance.OnResetAllBehavior += UpdateAllUI;
            
            comboSystem.OnComboUpdated += UpdateComboScoreText;
            comboSystem.OnComboTimeUpdated += UpdateComboTimeBar;

            levelSystem.OnLevelUpdate += UpdateLevelUI;
            skillUpgradeController.OnSkillUpgradeOptionsGenerated += SolfUpgradePopup;

            healthSystem.OnHealthChange += UpdateHealthUI;

            skillSystem.OnNewSkillAssign += AssignSkillSlot;
            skillSystem.OnSkillCooldownUpdate += UpdateCooldownSlot;
            skillSystem.OnSkillCooldownReset += ResetSkillSlot;

            combatSystem.OnDealDamage += UpdateDamageText;
            PoolingManager.Instance.Create<TextMeshProUGUI>(damageTextPrefab.name, PoolingGroupName.UI, CreateDamageText);
        }

        private void OnDestroy()
        {
            PlayerController.Instance.OnResetAllBehavior -= UpdateAllUI;
            
            comboSystem.OnComboUpdated -= UpdateComboScoreText;
            comboSystem.OnComboTimeUpdated -= UpdateComboTimeBar;

            levelSystem.OnLevelUpdate -= UpdateLevelUI;
            
            healthSystem.OnHealthChange -= UpdateHealthUI;
            
            skillSystem.OnNewSkillAssign -= AssignSkillSlot;
            skillSystem.OnSkillCooldownUpdate -= UpdateCooldownSlot;
            skillSystem.OnSkillCooldownReset -= ResetSkillSlot;
            
            
            combatSystem.OnDealDamage -= UpdateDamageText;
            PoolingManager.Instance.ClearPool(damageTextPrefab.name);
        }

        private void UpdateAllUI()
        {
            comboTimeoutBar.CurrentValue = comboSystem.ComboStartValue;
            comboTimeoutBar.MaxValue = comboSystem.ComboStartValue;
            
            UpdateLevelUI();
            UpdateHealthUI();
        }

        #region Combo UI
        private void UpdateComboTimeBar(float currentTime)
        {
            if (!comboTimeoutBar || !comboUI) return;
            comboUI.SetActive(comboSystem.ComboActive);
            comboTimeoutBar.CurrentValue = currentTime;
        }

        private void UpdateComboScoreText(float streak)
        {
            comboUI.SetActive(comboSystem.ComboActive);
            if (comboStreakText != null)
                comboStreakText.text = $"{streak} STRIKE!";

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

            // Reset & Prepare
            Transform tf = textInstance.transform;
            tf.position = damageData.HitPosition;
            tf.localScale = Vector3.zero;
            textInstance.text = damageData.Damage.ToString();
            textInstance.color = Color.white;

            // CanvasGroup for fade
            var canvasGroup = textInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = textInstance.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1;

            // Critical formatting
            bool isCrit = damageData.IsCritical;
            if (isCrit)
            {
                textInstance.text += " Crit!";
                textInstance.color = new Color(1f, 0.85f, 0.2f); // Gold-ish
                tf.SetAsLastSibling(); // Ensure appears on top
            }

            textInstance.gameObject.SetActive(true);

            // === Animation Settings ===
            float floatDuration = 0.2f; // Total time text will float and stay on screen
            float fadeOutDuration = 0.3f; // Duration for the fade-out at the end
            float delayBeforeFade = floatDuration - fadeOutDuration;

            float normalRiseAmount = 0.75f; // Distance text rises for normal hit
            float critRiseAmount = 1.4f; // Distance text rises for critical hit
            float riseAmount = isCrit ? critRiseAmount : normalRiseAmount;

            float scaleInNormal = 1.2f; // Initial pop scale for normal
            float scaleInCrit = 1.4f; // Initial pop scale for critical
            float scaleIn = isCrit ? scaleInCrit : scaleInNormal;

            float settleScale = 1.0f; // Final scale after settle
            float popDuration = 0.15f; // Duration of pop-in animation
            float settleDuration = 0.15f; // Duration of scale settle after pop

            // Kill any previous tweens on same target
            DOTween.Kill(tf);
            DOTween.Kill(canvasGroup);

            // === Sequence ===
            var seq = DOTween.Sequence();
            seq.Append(tf.DOScale(scaleIn, popDuration).SetEase(Ease.OutBack)) // Pop!
                .Append(tf.DOScale(settleScale, settleDuration).SetEase(Ease.InOutSine)) // Settle
                .Join(tf.DOMoveY(tf.position.y + riseAmount, floatDuration).SetEase(Ease.OutQuad)) // Float up
                .AppendInterval(delayBeforeFade) // Hold before fade
                .Append(canvasGroup.DOFade(0, fadeOutDuration)) // Fade out
                .AppendCallback(() =>
                {
                    textInstance.gameObject.SetActive(false);
                    PoolingManager.Instance.Release(damageTextPrefab.name, textInstance);
                });
        }

        #endregion

        #region Health UI

        private void UpdateHealthUI()
        {
            float hpAmount = (healthSystem.CurrentHealth / healthSystem.MaxHealth) * 15;
            hpBar.CurrentSlots = (int)Mathf.Clamp(hpAmount, 0, 15);
        }

        #endregion

        #region Level UI

        private void UpdateLevelUI()
        {
            levelText.text = "LEVEL " + levelSystem.Level;
            float fillAmount = levelSystem.ExpProgress01 * 100;
            levelbar.CurrentValue = Mathf.Clamp(fillAmount, 0, 100);
        }

        #endregion

        #region Solf Upgrade

        private void SolfUpgradePopup(List<BaseSkillDataSo> skillList)
        {
            if (skillList.Count <= 0) return;

            foreach (var skill in skillList) skillQueue.Enqueue(skill);

            if (!isChoosingSkill)
                ShowNextSkillPopup();
        }

        private void ShowNextSkillPopup()
        {
            if (skillQueue.Count == 0)
            {
                isChoosingSkill = false;
                return;
            }

            isChoosingSkill = true;

            UIManager.Instance.OpenPanel(UIPanelType.SolfUpgrade);
            ClearSkillCards();

            int skillsToShow = Mathf.Min(3, skillQueue.Count);
            for (int i = 0; i < skillsToShow; i++)
            {
                var skill = skillQueue.Dequeue();
                CreateSkillCard(skill).Forget();
            }
            PanelCardFeedback(solfUpgradePanel.transform);
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0, -1, true, 6.2f, true);
        }

        private void ClearSkillCards()
        {
            foreach (Transform child in solfUpgradePanel.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private async UniTask CreateSkillCard(BaseSkillDataSo skill)
        {
            var skillcard = Instantiate(solfUpgradeModel.gameObject, solfUpgradePanel.transform);
            var modal = skillcard.GetComponent<SolfUpgradeModel>();
            modal.UpdateUIModal(skill);
            await SkillCardFeedback(skillcard.transform);
            
            modal.SelectButton.onClick.AddListener(() =>
            {
                OnSkillSelected(skill);
            });
        }

        private void PanelCardFeedback(Transform tf)
        {
            // === Sequence ===
            tf.localPosition = new Vector2(1920, 0);
            var seq = DOTween.Sequence();
            seq.Append(tf.DOLocalMove(new Vector3(0,0,0), 0.7f).SetEase(Ease.OutBack))
                .Join(tf.DOShakeRotation(0.7f, 0f, vibrato: 10, randomness: 90).SetEase(Ease.OutBack))
                .Join(tf.DOScale(1.325f, 0.2f).SetEase(Ease.InOutSine))
                .Append(tf.DOScale(1f, 0.15f))
                .Append(tf.DOShakePosition(0.2f, 10f, vibrato: 10, randomness: 40))
                .SetUpdate(true);
        }
        
        private async UniTask SkillCardFeedback(Transform tf)
        {
            await tf.DOLocalRotate(
                    new Vector3(0, 720f, 0),
                    0.7f,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        private void OnSkillSelected(BaseSkillDataSo skill)
        {
            UIManager.Instance.CloseAllPanels();
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1f, 0f, true, 0f, false);
            skillUpgradeController.SelectSkill(skill);
            ClearSkillCards();
            ShowNextSkillPopup();
        }

        #endregion

        #region Skill Slot

        private void AssignSkillSlot(BaseSkillDataSo skill, int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;
            
            skillSlotModel[skillIndex].cooldownText.text = "";
            skillSlotModel[skillIndex].skillIcon.sprite = skill.SkillIcon;
            ResetSkillSlot(skillIndex);
        }

        private void UpdateCooldownSlot(float maxCooldown,float progression, int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;

            var currentCooldown = (maxCooldown * (1 - progression));
            
            skillSlotModel[skillIndex].cooldownText.text = currentCooldown <= 1 ? $"{currentCooldown:F1}" : $"{currentCooldown:F0}";
            skillSlotModel[skillIndex].valueBar.CurrentValue = 1 - progression;
        }


        private void ResetSkillSlot(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;
    
            skillSlotModel[skillIndex].cooldownText.text = "";
            skillSlotModel[skillIndex].valueBar.CurrentValue = 0;
        }


        #endregion
    }
}