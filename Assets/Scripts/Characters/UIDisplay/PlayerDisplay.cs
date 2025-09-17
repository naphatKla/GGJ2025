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
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
using Manager.SoundManager;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using PixelUI;
using Sirenix.OdinInspector;
using TMPro;
using UI;
using UI.IngameViewholder;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;

// ====== เพิ่มเติม ======

// ComboStreakDataSo
// ถ้า ComboStreakSystem อยู่ในเนมสเปซอื่น ให้แก้ using ให้ตรง
// using Characters.ComboSystem;

namespace Characters.UIDisplay
{
    public class PlayerDisplay : MonoBehaviour
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
        public GradeComboViewholder gradeComboViewholder;
        
        [Title("FlowStage Combo")] [FoldoutGroup("Combo Display")]
        public FlowStageComboViewholder flowStageComboViewholder;

        // ========= Combat =========
        [FoldoutGroup("Combat Display"), SerializeField]
        private CombatSystem combatSystem;

        [FoldoutGroup("Combat Display"), SerializeField]
        private TextMeshProUGUI worldTextUIPrefab;
        
        // ========= UI Feedback =========
        [FoldoutGroup("Feedback UI Display"), SerializeField]
        private TextMeshProUGUI worldTextUISkillFeedbackPrefab;
        
        [FoldoutGroup("Feedback UI Display"), SerializeField]
        private TextMeshProUGUI worldTextUIParryFeedbackPrefab;

        [FoldoutGroup("Feedback UI Display"), SerializeField]
        private MMF_Player feedbackSkill;

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

        [FoldoutGroup("Health Display"), SerializeField]
        public TextMeshProUGUI hpText;

        // ========= Solf Upgrade =========
        [FoldoutGroup("SolfUpgrade Display"), Title("Ref"), SerializeField]
        public SkillUpgradeController skillUpgradeController;
        [FoldoutGroup("SolfUpgrade Display")][SerializeField] private int cardsPerShow = 3;

        [FoldoutGroup("SolfUpgrade Display"), Title("UI"), FoldoutGroup("SolfUpgrade Display")]
        [FoldoutGroup("SolfUpgrade Display")]public GameObject solfUpgradePanel;

        [FoldoutGroup("SolfUpgrade Display")] public SolfUpgradeViewholder solfUpgradeViewholder;
        [FoldoutGroup("SolfUpgrade Display")] public Button solfUpgradeSelectButton;
        [FoldoutGroup("SolfUpgrade Display")] public ParticleSystem buttonFeedBack;

        private readonly Queue<BaseSkillDataSo> skillQueue = new();
        private readonly List<SolfUpgradeViewholder> _cards = new();

        private SolfUpgradeViewholder _currentSelectVH;
        private BaseSkillDataSo _currentSelect;
        private bool isChoosingSkill;

        // ========= Skill Slot =========
        [FoldoutGroup("SkillSlot Display"), Title("Ref"), SerializeField]
        public PlayerSkillSystem skillSystem;

        [FoldoutGroup("SkillSlot Display"), Title("UI"), FoldoutGroup("SkillSlot Display"), SerializeField]
        private TextMeshProUGUI cooldownIsNotReadyText;
        
        [FoldoutGroup("SkillSlot Display"), FoldoutGroup("SkillSlot Display"), SerializeField, PropertySpace(10,0)]
        private List<SkillSlotViewholder> skillSlotModel;

        // ========= Score =========
        [FoldoutGroup("Score Display"), Title("Ref"), SerializeField]
        private ScoreSystem scoreSystem;

        [FoldoutGroup("Score Display"), SerializeField, Title("UI")]
        private TextMeshProUGUI scoreText;

        [FoldoutGroup("Status Display"), SerializeField, Title("Ref")]
        private StatusEffectSystem statusEffectSystem;

        [FoldoutGroup("Status Display"), SerializeField, Title("UI")]
        private List<StatusSlotViewholder> statusSlots;

        private System.Action<float> _onHealthChangeUpdateUIHandler;
        private System.Action<float> _onHealthChangeTextHandler;

        private void Start()
        {
            PlayerController.Instance.OnResetAllBehavior += UpdateAllUI;
            if (solfUpgradeSelectButton != null)
            {
                solfUpgradeSelectButton.transform.DOKill();
                solfUpgradeSelectButton.onClick.RemoveAllListeners();
                solfUpgradeSelectButton.onClick.AddListener(() => OnConfirmPressed());
            }

            if (comboStreakSystem != null)
            {
                comboStreakSystem.OnKillComboChanged += UpdateKillComboText; // int → UI streak
                comboStreakSystem.OnStageUpdate += UpdateComboStreakBar;
                comboStreakSystem.OnBoostChanged += UpdateBoostMultiplierText; // float xN
                //comboStreakSystem.OnTimerTick  // float seconds
                comboStreakSystem.OnGradeChanged += gradeComboViewholder.UpdateGradeCombo;
                comboStreakSystem.OnStageEnter += ComboValueBarUpdate;
                //comboStreakSystem.OnStageExit += ComboValueBarUpdate;
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
            skillSystem.OnSlotCooldownSpeedChanged += OverloopFeedback;
            skillSystem.OnSkillPerformFail += NotifySkillPerformFail;
            skillSystem.OnSecondarySuccess += UpdateParryFeedbackText;

            combatSystem.OnDealDamage += UpdateDamageText;
            scoreSystem.OnScoreChange += UpdateScoreUI;

            statusEffectSystem.OnStatusUIUpdate += UpdateStatusUI;

            PoolingManager.Instance.Create<TextMeshProUGUI>(worldTextUIPrefab.name, PoolingGroupName.UI,
                CreateDamageText);
            
            PoolingManager.Instance.Create<TextMeshProUGUI>(worldTextUISkillFeedbackPrefab.name, PoolingGroupName.UI,
                CreateFeedbackText);

            PoolingManager.Instance.Create<TextMeshProUGUI>(worldTextUIParryFeedbackPrefab.name, PoolingGroupName.UI,
                CreateParryFeedbackText);
            
            UpdateAllUI();
        }

        private void OnDestroy()
        {
            PlayerController.Instance.OnResetAllBehavior -= UpdateAllUI;
            if (solfUpgradeSelectButton != null)
                solfUpgradeSelectButton.transform.DOKill();

            if (comboStreakSystem != null)
            {
                comboStreakSystem.OnStreakChanged -= UpdateKillComboText;
                comboStreakSystem.OnBoostChanged -= UpdateBoostMultiplierText;
                comboStreakSystem.OnStageUpdate -= UpdateComboStreakBar;
                comboStreakSystem.OnStageEnter -= ComboValueBarUpdate;
                //comboStreakSystem.OnStageExit -= ComboValueBarUpdate;
                comboStreakSystem.OnBerserkEnter -= OnBerserkEnter;
                comboStreakSystem.OnBerserkExit -= OnBerserkExit;
                comboStreakSystem.OnGradeChanged -= gradeComboViewholder.UpdateGradeCombo;
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
            skillSystem.OnSlotCooldownSpeedChanged -= OverloopFeedback;
            skillSystem.OnSkillPerformFail -= NotifySkillPerformFail;

            combatSystem.OnDealDamage -= UpdateDamageText;
            scoreSystem.OnScoreChange -= UpdateScoreUI;

            statusEffectSystem.OnStatusUIUpdate -= UpdateStatusUI;

            PoolingManager.Current?.ClearPool(worldTextUIPrefab.name);
        }

        private void UpdateAllUI()
        {
            if (comboStreakBar != null && comboStreakSystem.Data != null)
                comboStreakBar.CurrentValue = 0f;

            if (comboUI) comboUI.SetActive(false);
            if (killComboText) killComboText.text = "0 STRIKE!";
            if (scoreMultiply) scoreMultiply.text = "x0";

            foreach (var statusSlotModel in statusSlots)
                statusSlotModel.gameObject.SetActive(false);

            UpdateLevelUI();
            UpdateHealthUI();
            scoreText.text = "0";
            cooldownIsNotReadyText.alpha = 0;
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
            if (combo == null)     // unstage
            {
                flowStageComboViewholder.CloseCurrent();
                comboStreakBar.FillImage.color = Color.green;
                return;
            }
            
            flowStageComboViewholder.UpdateFlowSceneFeedback(combo.stageId);
            
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
                NotificationManager.Instance.PlayNotification("notify_skilluse", "Critical!", 4.0f);
                textInstance.text += " Crit!";
                textInstance.color = new Color(1f, 0.85f, 0.2f);
                tf.SetAsLastSibling();
            }

            textInstance.gameObject.SetActive(true);

            // === Animation Settings ===
            float floatDuration = 0.25f;
            float fadeOutDuration = 0.25f;
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
                    PoolingManager.Current?.Release(worldTextUIPrefab.name, textInstance);
                });
        }

        private void UpdateHealthText(float healthChange)
        {
            var textInstance = PoolingManager.Instance.Get<TextMeshProUGUI>(worldTextUIPrefab.name);

            Transform tf = textInstance.transform;
            Vector3 p = healthSystem.transform.position;
            Vector2 off = Random.insideUnitCircle * 2.65f;
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
                NotificationManager.Instance.PlayNotification("notify_skilluse", $"+{healthChange} Health", 4.0f);
                textInstance.text = "+" + healthChange + " HP";
                textInstance.color = Color.green;
                tf.SetAsLastSibling();
            }

            textInstance.gameObject.SetActive(true);

            float floatDuration = 0.2f;
            float fadeOutDuration = 0.3f;
            float delayBeforeFade = floatDuration - fadeOutDuration;
            float riseAmount = 0.75f;
            float scaleIn = 1.0f;
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
                    PoolingManager.Current?.Release(worldTextUIPrefab.name, textInstance);
                });
        }

        #endregion

        #region Health UI

        private void OnHealthChange_UpdateHPUI(float _) => UpdateHealthUI();

        private void UpdateHealthUI()
        {
            float hpAmount01 = Mathf.Clamp01(healthSystem.CurrentHealth / Mathf.Max(1f, healthSystem.MaxHealth));
            hpText.text = $"{healthSystem.CurrentHealth} / {healthSystem.MaxHealth}";
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

        public void SolfUpgradePopup(List<BaseSkillDataSo> skillList)
        {
            if (skillList == null || skillList.Count == 0) return;

            foreach (var skill in skillList)
                skillQueue.Enqueue(skill);

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

            UIManager.Instance.OpenPanel(UIPanelType.SolfUpgrade).Forget();
            ClearSkillCards();

            int count = Mathf.Min(cardsPerShow, skillQueue.Count);
            for (int i = 0; i < count; i++)
            {
                var skill = skillQueue.Dequeue();
                CreateSkillCard(skill).Forget();
            }

            UpdateConfirmButtonState();
            PanelCardFeedback(solfUpgradePanel.transform);
        }

        private void ClearSkillCards()
        {
            _currentSelectVH = null;
            _currentSelect = null;

            foreach (Transform child in solfUpgradePanel.transform)
                Destroy(child.gameObject);
            _cards.Clear();
        }
        
        private async UniTask CreateSkillCard(BaseSkillDataSo skill)
        {
            var skillcard = Instantiate(solfUpgradeViewholder.gameObject, solfUpgradePanel.transform);
            var vh = skillcard.GetComponent<SolfUpgradeViewholder>();
            _cards.Add(vh);

            bool isNew = !skillSystem.ContainsSkillWithSameRoot(skill);
            vh.UpdateUIModal(skill, isNew);
            vh.Bind(skill, isNew, HandleCardClicked, HandleCardHoldClicked);
            await SkillCardFeedback(skillcard.transform);
        }

        private void PanelCardFeedback(Transform tf)
        {
            tf.localPosition = new Vector2(1920, 0);
            var seq = DOTween.Sequence();
            seq.Append(tf.DOLocalMove(new Vector3(0, 0, 0), 0.7f).SetEase(Ease.OutBack))
                .Join(tf.DOShakeRotation(0.7f, 0f, vibrato: 10, randomness: 90).SetEase(Ease.OutBack))
                .Join(tf.DOScale(1.125f, 0.2f).SetEase(Ease.InOutSine))
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

        private void HandleCardClicked(SolfUpgradeViewholder vh)
        {
            if (vh == null) return;
            if (_currentSelectVH == vh) return;
            if (_currentSelectVH != null) _currentSelectVH.SetSelected(false);
            
            _currentSelectVH = vh;
            _currentSelect = vh.Data;
            _currentSelectVH.SetSelected(true);
            
            UpdateConfirmButtonState();
        }
        
        private void HandleCardHoldClicked(SolfUpgradeViewholder vh)
        {
            if (vh == null) return;
            if (_currentSelectVH == vh) return;
            if (_currentSelectVH != null) _currentSelectVH.SetSelected(false);
            
            _currentSelectVH = vh;
            _currentSelect = vh.Data;
            _currentSelectVH.SetSelected(true);

            OnChooseSkillAsync().Forget();
        }
        
        public void OnConfirmPressed()
        {
            if (_currentSelect == null) return;
            OnChooseSkill();
        }
        
        private async UniTask OnChooseSkillAsync()
        {
            await UniTask.Delay(200, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, destroyCancellationToken);
            OnChooseSkill();
        }

        private void OnChooseSkill()
        {
            UIManager.Instance.CloseAllPanels();
            skillUpgradeController.SelectSkill(_currentSelect);
            
            ClearSkillCards();
            ShowNextSkillPopup();
            _currentSelect = null;
        }
        
        private void UpdateConfirmButtonState()
        {
            if (solfUpgradeSelectButton == null) return;
          
            solfUpgradeSelectButton.gameObject.SetActive(_currentSelect != null);
            var t = solfUpgradeSelectButton.transform;
            t.DOKill();
            t.DOPunchScale(t.localScale * 0.08f, 0.2f, 8, 0.9f).SetUpdate(true);
            if (_currentSelect != null) buttonFeedBack.Play();
        }

        #endregion
        
        #region Parry Success
        
        private TextMeshProUGUI CreateParryFeedbackText()
        {
            return Instantiate(worldTextUIParryFeedbackPrefab);
        }
        
        private void UpdateParryFeedbackText(BaseSkillDataSo skillDataSo)
        {
            var textInstance = PoolingManager.Instance.Get<TextMeshProUGUI>(worldTextUIParryFeedbackPrefab.name);
            NotificationManager.Instance.PlayNotification("notify_skilluse", skillDataSo.SkillName, 4.0f);

            // Reset & Prepare
            Transform tf = textInstance.transform;
            tf.position = PlayerController.Instance.transform.position;
            tf.localScale = Vector3.zero;
            textInstance.text = "PARRY SUCCESS!";
            textInstance.color = Color.white;

            // CanvasGroup for fade
            var canvasGroup = textInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = textInstance.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1;
            tf.SetAsLastSibling();

            textInstance.gameObject.SetActive(true);

            // === Animation Settings ===
            float floatDuration = 1f;
            float fadeOutDuration = 0.25f;
            float delayBeforeFade = floatDuration - fadeOutDuration;

            float riseAmount = 1.4f;
            float scaleIn =1.4f;
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
                    PoolingManager.Current?.Release(worldTextUIParryFeedbackPrefab.name, textInstance);
                });
        }
        
        #endregion

        #region Skill Slot

        private void AssignSkillSlot(BaseSkillDataSo skill, int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;

            skillSlotModel[skillIndex].UpdateLevelText(skill.Level, skill);
            ResetSkillSlot(skillIndex);
        }

        private void SkillPerfrom(BaseSkillDataSo skilldata,int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;

            SkillPlayFeedback(skilldata, skillIndex);
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
            skillSlotModel[skillIndex].ResetSkillSlot();
            
            if (skillIndex != 0) skillSlotModel[skillIndex].PlayCooldownFinishFeedback();
        }

        private void SkillPlayFeedback(BaseSkillDataSo skillDataSo, int skillIndex )
        {
            if (skillIndex > 1)
            {
                UpdateFeedbackText(skillDataSo);
            }
        }
        
        private TextMeshProUGUI CreateFeedbackText()
        {
            return Instantiate(worldTextUISkillFeedbackPrefab);
        }
        
        private void UpdateFeedbackText(BaseSkillDataSo skillDataSo)
        {
            var textInstance = PoolingManager.Instance.Get<TextMeshProUGUI>(worldTextUISkillFeedbackPrefab.name);
            NotificationManager.Instance.PlayNotification("notify_skilluse", skillDataSo.SkillName, 4.0f);
            PopupUIManager.Instance.ShowPopup("SkillTopPullup", 3f);
            PopupUIManager.Instance.ShowPopup("SkillBottomPullup", 3f);
            feedbackSkill?.PlayFeedbacks();

            // Reset & Prepare
            Transform tf = textInstance.transform;
            tf.position = PlayerController.Instance.transform.position;
            tf.localScale = Vector3.zero;
            textInstance.text = skillDataSo.SkillName;
            textInstance.color = Color.white;

            // CanvasGroup for fade
            var canvasGroup = textInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = textInstance.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1;
            tf.SetAsLastSibling();

            textInstance.gameObject.SetActive(true);

            // === Animation Settings ===
            float floatDuration = 1f;
            float fadeOutDuration = 0.25f;
            float delayBeforeFade = floatDuration - fadeOutDuration;

            float riseAmount = 1.4f;
            float scaleIn =1.4f;
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
                    PoolingManager.Current?.Release(worldTextUISkillFeedbackPrefab.name, textInstance);
                });

            SoundManager.Instance.PlayUI(SoundName.UI.Gameplay_SkillNotify);
        }

        private Sequence _skillResetSequence;

        private void SkillResetFeedback(Transform tf, Image skillframe)
        {
            _skillResetSequence.Kill(true);
            _skillResetSequence = DOTween.Sequence();
            _skillResetSequence.Append(skillframe.DOColor(Color.green, 0.15f).SetDelay(0.1f)
                .SetLoops(2, LoopType.Yoyo));
        }

        private void OverloopFeedback(int skillIndex, float multiply)
        {
            if (skillIndex < 0 || skillIndex >= skillSlotModel.Count) return;
            if (skillSlotModel[skillIndex] == null) return;
            
            skillSlotModel[skillIndex].OverloopFeedback(multiply);
        }


        private Sequence _skillPerformFailSequence;
        private void NotifySkillPerformFail(string contextReason)
        {
            if (_skillPerformFailSequence.IsActive()) return;

            var originPos = cooldownIsNotReadyText.transform.position;
            cooldownIsNotReadyText.text = contextReason;
            cooldownIsNotReadyText.alpha = 0;
            cooldownIsNotReadyText.transform.localScale = Vector3.zero;
            _skillPerformFailSequence = DOTween.Sequence();
            _skillPerformFailSequence
                .Join(cooldownIsNotReadyText.transform.DOScale(Vector3.one, 0.2f))
                .Join(cooldownIsNotReadyText.transform.DOMoveY(originPos.y + 0.15f, 0.5f))
                .Join(cooldownIsNotReadyText.DOFade(1, 0.3f))
                .AppendInterval(0.85f).Append(cooldownIsNotReadyText.DOFade(0, 0.3f))
                .OnComplete(() =>
                {
                    cooldownIsNotReadyText.transform.position = originPos;
                    cooldownIsNotReadyText.transform.localScale = Vector3.zero;
                });
        }

        #endregion

        #region Score UI

        public void UpdateScoreUI(int score)
        {
            scoreText.text = $"{score}";

            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;

            Sequence seq = DOTween.Sequence();
            seq.Append(scoreText.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
            seq.Append(scoreText.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));
            seq.SetUpdate(true);
        }



        #endregion

        #region Status UI

        private void UpdateStatusUI(IReadOnlyList<StatusEffectUIData> datas)
        {
            int maxSlot = Mathf.Min(statusSlots.Count, datas.Count);

            for (var i = 0; i < statusSlots.Count; i++)
            {
                if (i >= maxSlot)
                {
                    statusSlots[i].Show(false);
                    continue;
                }

                var data = datas[i];
                var slot = statusSlots[i];

                slot.Show(true);
                slot.buffIcon.sprite = data.Icon;
                if (data.CurrentDuration >= 100)
                    slot.durationText.text = "";
                else
                    slot.durationText.text = data.CurrentDuration <= 1
                        ? $"{data.CurrentDuration:F1}"
                        : $"{data.CurrentDuration:F0}";
                slot.valueBar.CurrentValue = 1 - data.CurrentDuration / data.MaxDuration;
                slot.statusFrame.color = data.isDebuff ? Color.red : Color.green;
            }
        }

        #endregion
    }
}