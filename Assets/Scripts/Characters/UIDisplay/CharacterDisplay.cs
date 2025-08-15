using System;
using System.Collections.Generic;
using Characters.CombatSystems;
using Characters.ComboSystem;
using Characters.Controllers;
using Characters.HeathSystems;
using Characters.LevelSystems;
using Characters.ScoreSystems;
using Characters.SkillSystems;
using Characters.SO.ComboStreakDataSO.StageDataSO;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using PixelUI;
using Sirenix.OdinInspector;
using TMPro;
using UI.IngameModal;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// ====== เพิ่มเติม ======

// ComboStreakDataSo
// ถ้า ComboStreakSystem อยู่ในเนมสเปซอื่น ให้แก้ using ให้ตรง
// using Characters.ComboSystem;

namespace Characters.UIDisplay
{
    public class CharacterDisplay : MonoBehaviour
    {
        [FoldoutGroup("Combo Display"), Title("Ref"), SerializeField]
        private ComboStreakSystem comboStreakSystem;

        [Title("UI"), FoldoutGroup("Combo Display")]
        public GameObject comboUI;

        [FoldoutGroup("Combo Display")] public TMP_Text killComboText;
        [FoldoutGroup("Combo Display")] public TMP_Text scoreMultiply; // แสดงตัวคูณ Boost (xN)
        [FoldoutGroup("Combo Display")] public GameObject lightningCombo;
        [FoldoutGroup("Combo Display")] public ValueBar comboStreakBar;
        [FoldoutGroup("Combo Display")] public float tweenDuration = 0.1f;
        [FoldoutGroup("Combo Display")] public float scaleAmount = 1.2f;
        
        
        [Title("Grade Combo")] [FoldoutGroup("Combo Display")]
        public GradeComboDisplay gradeComboDisplay;

        // ========= Combat =========
        [FoldoutGroup("Combat Display"), SerializeField]
        private CombatSystem combatSystem;

        [FoldoutGroup("Combat Display"), SerializeField]
        private TextMeshProUGUI worldTextUIPrefab;

        // ========= Level =========
        [FoldoutGroup("Level Display"), Title("Ref"), SerializeField]
        public LevelSystem levelSystem;

        [Title("UI"), FoldoutGroup("Level Display")]
        public TMP_Text levelText;

        [FoldoutGroup("Level Display")] public ValueBar levelbar;

        // ========= Health =========
        [FoldoutGroup("Health Display"), Title("Ref"), SerializeField]
        public HealthSystem healthSystem;

        [Title("UI"), FoldoutGroup("Health Display"), SerializeField]
        public MMProgressBar hpProgressBar;

        // ========= Solf Upgrade =========
        [FoldoutGroup("SolfUpgrade Display"), Title("Ref"), SerializeField]
        public SkillUpgradeController skillUpgradeController;

        [FoldoutGroup("SolfUpgrade Display"), Title("UI"), FoldoutGroup("SolfUpgrade Display")]
        public GameObject solfUpgradePanel;

        [FoldoutGroup("SolfUpgrade Display")] public SolfUpgradeModel solfUpgradeModel;

        private readonly Queue<BaseSkillDataSo> skillQueue = new();
        private bool isChoosingSkill = false;

        // ========= Skill Slot =========
        [FoldoutGroup("SkillSlot Display"), Title("Ref"), SerializeField]
        public SkillSystem skillSystem;

        [FoldoutGroup("SkillSlot Display"), Title("UI"), FoldoutGroup("SkillSlot Display"), SerializeField]
        private List<SkillSlotModel> skillSlotModel;

        // ========= Score =========
        [FoldoutGroup("Score Display"), Title("Ref"), SerializeField]
        private ScoreSystem scoreSystem;

        [FoldoutGroup("Score Display"), SerializeField, Title("UI")]
        private TextMeshProUGUI scoreText;

        private System.Action<float> _onHealthChangeUpdateUIHandler;
        private System.Action<float> _onHealthChangeTextHandler;

        private void Start()
        {
            PlayerController.Instance.OnResetAllBehavior += UpdateAllUI;

            if (comboStreakSystem != null)
            {
                comboStreakSystem.OnKillComboChanged += UpdateKillComboText; // int → UI streak
                comboStreakSystem.OnStageUpdate += UpdateComboStreakBar;
                comboStreakSystem.OnBoostChanged += UpdateBoostMultiplierText; // float xN
                //comboStreakSystem.OnTimerTick  // float seconds
                comboStreakSystem.OnGradeChanged += gradeComboDisplay.UpdateGradeCombo;
                comboStreakSystem.OnStageEnter += ComboValueBarUpdate;
                comboStreakSystem.OnStageExit += ComboValueBarUpdate;
                comboStreakSystem.OnBerserkEnter += OnBerserkEnter;
                comboStreakSystem.OnBerserkExit += OnBerserkExit;
                //comboStreakSystem.OnStageExit
            }

            levelSystem.OnLevelUpdate += UpdateLevelUI;
            skillUpgradeController.OnSkillUpgradeOptionsGenerated += SolfUpgradePopup;

            _onHealthChangeUpdateUIHandler = OnHealthChange_UpdateHPUI;
            _onHealthChangeTextHandler = UpdateHealthText;

            healthSystem.OnHealthChange += _onHealthChangeUpdateUIHandler;
            healthSystem.OnHealthChange += _onHealthChangeTextHandler;

            skillSystem.OnNewSkillAssign += AssignSkillSlot;
            skillSystem.OnSkillCooldownUpdate += UpdateCooldownSlot;
            skillSystem.OnSkillCooldownReset += ResetSkillSlot;
            skillSystem.OnSkillPerform += SkillPerfrom;

            combatSystem.OnDealDamage += UpdateDamageText;
            scoreSystem.OnScoreChange += UpdateScoreUI;

            PoolingManager.Instance.Create<TextMeshProUGUI>(worldTextUIPrefab.name, PoolingGroupName.UI,
                CreateDamageText);

            UpdateAllUI();
        }

        private void OnDestroy()
        {
            PlayerController.Instance.OnResetAllBehavior -= UpdateAllUI;

            if (comboStreakSystem != null)
            {
                comboStreakSystem.OnStreakChanged -= UpdateKillComboText;
                comboStreakSystem.OnBoostChanged -= UpdateBoostMultiplierText;
                comboStreakSystem.OnStageUpdate -= UpdateComboStreakBar;
                comboStreakSystem.OnStageEnter -= ComboValueBarUpdate;
                comboStreakSystem.OnStageExit -= ComboValueBarUpdate;
                comboStreakSystem.OnBerserkEnter -= OnBerserkEnter;
                comboStreakSystem.OnBerserkExit -= OnBerserkExit;
                comboStreakSystem.OnGradeChanged -= gradeComboDisplay.UpdateGradeCombo;
            }

            levelSystem.OnLevelUpdate -= UpdateLevelUI;
            skillUpgradeController.OnSkillUpgradeOptionsGenerated -= SolfUpgradePopup;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChange -= _onHealthChangeUpdateUIHandler;
                healthSystem.OnHealthChange -= _onHealthChangeTextHandler;
            }

            skillSystem.OnNewSkillAssign -= AssignSkillSlot;
            skillSystem.OnSkillCooldownUpdate -= UpdateCooldownSlot;
            skillSystem.OnSkillCooldownReset -= ResetSkillSlot;

            combatSystem.OnDealDamage -= UpdateDamageText;
            scoreSystem.OnScoreChange -= UpdateScoreUI;

            PoolingManager.Instance.ClearPool(worldTextUIPrefab.name);
        }

        private void UpdateAllUI()
        {
            if (comboStreakBar != null && comboStreakSystem.Data != null)
                comboStreakBar.CurrentValue = 0f;

            if (comboUI) comboUI.SetActive(false);
            if (killComboText) killComboText.text = "0 STRIKE!";
            if (scoreMultiply) scoreMultiply.text = "x0";

            UpdateLevelUI();
            UpdateHealthUI();
        }

        #region Combo UI (ใหม่)

        private void UpdateComboStreakBar(int currentStageMinStreak, int currentStreak, int nextStageMinStreak)
        {
            if (!comboStreakBar || !comboUI) return;

            comboUI.SetActive(currentStreak > 0);
            comboStreakBar.MinValue = currentStageMinStreak == nextStageMinStreak
                ? currentStageMinStreak - 1
                : currentStageMinStreak;
            comboStreakBar.MaxValue = nextStageMinStreak;
            var clampValue = Mathf.Clamp(currentStreak, currentStageMinStreak, comboStreakBar.MaxValue);
            comboStreakBar.CurrentValue = clampValue;
        }

        private void UpdateKillComboText(int streak)
        {
            if (!comboUI) return;

            comboUI.SetActive(streak > 0);

            if (killComboText != null)
                killComboText.text = $"{streak} STRIKE!";
            
            // pop tween
            comboUI.transform
                .DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => comboUI.transform.DOScale(Vector3.one, tweenDuration));
        }

        private void UpdateBoostMultiplierText(float multiplierX)
        {
            if (scoreMultiply == null) return;
            scoreMultiply.text = $"x{multiplierX:0.##} ENERGY!";
        }

        private void ComboValueBarUpdate(BaseComboStageSo combo)
        {
            switch (combo.stageId)
            {
                case "flow_i":
                    comboStreakBar.FillImage.color = Color.yellow;
                    break;
                case "flow_ii":
                    comboStreakBar.FillImage.color = Color.red;
                    break;
                default:
                    comboStreakBar.FillImage.color = Color.green;
                    break;
            }
        }

        private void OnBerserkEnter()
        {
            lightningCombo.SetActive(true);
        }
        
        private void OnBerserkExit()
        {
            lightningCombo.SetActive(false);
        }

        #endregion

        #region Combat UI

        private TextMeshProUGUI CreateDamageText()
        {
            return Instantiate(worldTextUIPrefab);
        }

        private void UpdateDamageText(DamageData damageData)
        {
            var textInstance = PoolingManager.Instance.Get<TextMeshProUGUI>(worldTextUIPrefab.name);

            // Reset & Prepare
            Transform tf = textInstance.transform;
            Vector3 p = damageData.HitPosition;
            Vector2 off = Random.insideUnitCircle * 1f;
            tf.position = new Vector3(p.x + off.x, p.y + off.y);
            tf.localScale = Vector3.zero;
            textInstance.text = damageData.Damage.ToString();
            textInstance.color = Color.white;

            // CanvasGroup for fade
            var canvasGroup = textInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = textInstance.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1;

            bool isCrit = damageData.IsCritical;
            if (isCrit)
            {
                textInstance.text += " Crit!";
                textInstance.color = new Color(1f, 0.85f, 0.2f);
                tf.SetAsLastSibling();
            }

            textInstance.gameObject.SetActive(true);

            // === Animation Settings ===
            float floatDuration = 0.2f;
            float fadeOutDuration = 0.3f;
            float delayBeforeFade = floatDuration - fadeOutDuration;

            float riseAmount = isCrit ? 1.4f : 0.75f;
            float scaleIn = isCrit ? 1.4f : 1.2f;
            float settleScale = 1.0f;
            float popDuration = 0.15f;
            float settleDuration = 0.15f;

            DOTween.Kill(tf);
            DOTween.Kill(canvasGroup);

            var seq = DOTween.Sequence();
            seq.Append(tf.DOScale(scaleIn, popDuration).SetEase(Ease.OutBack))
                .Append(tf.DOScale(settleScale, settleDuration).SetEase(Ease.InOutSine))
                .Join(tf.DOMoveY(tf.position.y + riseAmount, floatDuration).SetEase(Ease.OutQuad))
                .AppendInterval(delayBeforeFade)
                .Append(canvasGroup.DOFade(0, fadeOutDuration))
                .AppendCallback(() =>
                {
                    textInstance.gameObject.SetActive(false);
                    PoolingManager.Instance.Release(worldTextUIPrefab.name, textInstance);
                });
        }

        private void UpdateHealthText(float healthChange)
        {
            var textInstance = PoolingManager.Instance.Get<TextMeshProUGUI>(worldTextUIPrefab.name);

            Transform tf = textInstance.transform;
            Vector3 p = healthSystem.transform.position;
            Vector2 off = Random.insideUnitCircle * 1f;
            tf.position = new Vector3(p.x + off.x, p.y + off.y);
            tf.localScale = Vector3.zero;
            textInstance.text = healthChange + " HP";
            textInstance.color = Color.red;

            var canvasGroup = textInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = textInstance.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1;

            if (healthChange >= 0)
            {
                textInstance.text = "+" + healthChange + " HP";
                textInstance.color = Color.green;
                tf.SetAsLastSibling();
            }

            textInstance.gameObject.SetActive(true);

            float floatDuration = 0.2f;
            float fadeOutDuration = 0.3f;
            float delayBeforeFade = floatDuration - fadeOutDuration;
            float riseAmount = 0.75f;
            float scaleIn = 1.25f;
            float settleScale = 1.0f;
            float popDuration = 0.15f;
            float settleDuration = 0.15f;

            DOTween.Kill(tf);
            DOTween.Kill(canvasGroup);

            var seq = DOTween.Sequence();
            seq.Append(tf.DOScale(scaleIn, popDuration).SetEase(Ease.OutBack))
                .Append(tf.DOScale(settleScale, settleDuration).SetEase(Ease.InOutSine))
                .Join(tf.DOMoveY(tf.position.y + riseAmount, floatDuration).SetEase(Ease.OutQuad))
                .AppendInterval(delayBeforeFade)
                .Append(canvasGroup.DOFade(0, fadeOutDuration))
                .AppendCallback(() =>
                {
                    textInstance.gameObject.SetActive(false);
                    PoolingManager.Instance.Release(worldTextUIPrefab.name, textInstance);
                });
        }

        #endregion

        #region Health UI

        private void OnHealthChange_UpdateHPUI(float _) => UpdateHealthUI();

        private void UpdateHealthUI()
        {
            float hpAmount01 = Mathf.Clamp01(healthSystem.CurrentHealth / Mathf.Max(1f, healthSystem.MaxHealth));
            hpProgressBar.UpdateBar01(hpAmount01);
        }

        #endregion

        #region Level UI

        private void UpdateLevelUI()
        {
            levelText.text = "LEVEL " + levelSystem.Level;
            float fillPercent = Mathf.Clamp01(levelSystem.ExpProgress01) * 100f;
            levelbar.CurrentValue = fillPercent;
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
                Destroy(child.gameObject);
        }

        private async UniTask CreateSkillCard(BaseSkillDataSo skill)
        {
            var skillcard = Instantiate(solfUpgradeModel.gameObject, solfUpgradePanel.transform);
            var modal = skillcard.GetComponent<SolfUpgradeModel>();
            modal.UpdateUIModal(skill);
            await SkillCardFeedback(skillcard.transform);

            modal.SelectButton.onClick.AddListener(() => { OnSkillSelected(skill); });
        }

        private void PanelCardFeedback(Transform tf)
        {
            tf.localPosition = new Vector2(1920, 0);
            var seq = DOTween.Sequence();
            seq.Append(tf.DOLocalMove(new Vector3(0, 0, 0), 0.7f).SetEase(Ease.OutBack))
                .Join(tf.DOShakeRotation(0.7f, 0f, vibrato: 10, randomness: 90).SetEase(Ease.OutBack))
                .Join(tf.DOScale(1.325f, 0.2f).SetEase(Ease.InOutSine))
                .Append(tf.DOScale(1f, 0.15f))
                .Append(tf.DOShakePosition(0.2f, 10f, vibrato: 10, randomness: 40))
                .SetUpdate(true);
        }

        private async UniTask SkillCardFeedback(Transform tf)
        {
            await tf.DOLocalRotate(new Vector3(0, 720f, 0), 0.7f, RotateMode.FastBeyond360)
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

        private void SkillPerfrom(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;

            SkillPlayFeedback(skillSlotModel[skillIndex].transform, skillSlotModel[skillIndex].skillframe);
        }

        private void UpdateCooldownSlot(float maxCooldown, float progression, int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;

            var currentCooldown = (maxCooldown * (1 - progression));
            skillSlotModel[skillIndex].cooldownText.text =
                currentCooldown <= 1 ? $"{currentCooldown:F1}" : $"{currentCooldown:F0}";
            skillSlotModel[skillIndex].valueBar.CurrentValue = 1 - progression;
        }

        private void ResetSkillSlot(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;

            SkillResetFeedback(skillSlotModel[skillIndex].transform, skillSlotModel[skillIndex].skillframe);
            skillSlotModel[skillIndex].cooldownText.text = "";
            skillSlotModel[skillIndex].valueBar.CurrentValue = 0;
        }

        private void SkillPlayFeedback(Transform tf, Image skillframe)
        {
            /*var seq = DOTween.Sequence();
            seq.Append(tf.DOScale(new Vector3(tf.localScale.x + -0.05f, tf.localScale.y + -0.05f, 1), 0.15f)
                .SetLoops(2, LoopType.Yoyo));*/
        }

        private Sequence _skillResetSequence;

        private void SkillResetFeedback(Transform tf, Image skillframe)
        {
            _skillResetSequence.Kill(true);
            _skillResetSequence = DOTween.Sequence();
            _skillResetSequence.Append(skillframe.DOColor(Color.green, 0.15f).SetDelay(0.1f)
                .SetLoops(2, LoopType.Yoyo));
        }

        #endregion

        #region Score UI

        public void UpdateScoreUI(int score)
        {
            scoreText.text = $"{score}";
        }

        #endregion
    }
}