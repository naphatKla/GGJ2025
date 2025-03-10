using Characters;
using DG.Tweening;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboText : MonoBehaviour
{
    [FoldoutGroup("UI")] public TextMeshProUGUI hitText;
    [FoldoutGroup("UI")] public TextMeshProUGUI scoreMultiplyText;
    [FoldoutGroup("UI")] public Slider comboTimeoutSlider;
    [FoldoutGroup("UI")] public float tweenDuration = 0.1f;
    [FoldoutGroup("UI")] public float scaleAmount = 1.2f;
    [SerializeField] private float maxScoreMultiply;
    [SerializeField] [Unit(Units.Second)] private float comboTimeOut = 3f;
    public MMF_Player voiceFeedback;
    private int _lastTriggeredScore = -1;
    private bool isCombo = false;
    private float _score => PlayerCharacter.Instance.HitCombo;
    private float _scoreMultiply => PlayerCharacter.Instance.ScoreMultiply;
    private float _comboTimeCounter;

    void Start()
    {
        if (hitText != null) {PlayerCharacter.OnHitComboChanged.AddListener(PlayTween);}
        //if (hitText != null) {PlayerCharacter.Instance.onHitWithDamage.AddListener(ResetCombo);}
        comboTimeoutSlider.maxValue = comboTimeOut;
        comboTimeoutSlider.value = comboTimeOut;
        _comboTimeCounter = comboTimeOut;
    }

    private void Update()
    {
        if (!LoadScene.Instance.hasStarted) return;
        UpdateScoreText();
        UpdateComboTime();
        UpdateComboUI();
        
        if (_comboTimeCounter <= 0 && PlayerCharacter.Instance.HitCombo > 0)
        {
            ResetCombo();
        }

        if (_score > 0 && _score % 1 == 0 && _score != _lastTriggeredScore)
        {
            if (_score % 25 == 0)
            {
                OnEveryTwentyFiveHitCombo();
            }
            else if (_score % 15 == 0)
            {
                OnEveryFifteenHitCombo();
            }
            else if (_score % 10 == 0)
            {
                voiceFeedback.PlayFeedbacks();
            }
            else if (_score % 1 == 0)
            {
                OnEveryHitCombo();
            }
    
            _lastTriggeredScore = (int)_score;
        }
    }

    public void ResetCombo()
    {
        isCombo = false;
        PlayerCharacter.Instance.HitCombo = 0;
        PlayerCharacter.Instance.ResetscoreMultiply();
        PlayerCharacter.Instance.ResetDamage(PlayerCharacter.Instance);
    }

    private void UpdateScoreText()
    {
        hitText.text = "Hit " + _score;
        scoreMultiplyText.text = $"Score x {_scoreMultiply:F2}";
    }
    
    private void UpdateComboTime()
    {
        if (isCombo)
        {
            _comboTimeCounter -= Time.deltaTime;
            comboTimeoutSlider.value = _comboTimeCounter;
        }
    }

    
    private void UpdateComboUI()
    {
        if (isCombo)
        {
            hitText.gameObject.SetActive(true);
        }
        else
        {
            hitText.gameObject.SetActive(false);
        }
    }
    
    private void PlayTween()
    {
        if (_score > 0)
        {
            isCombo = true;
            comboTimeoutSlider.value = comboTimeOut;
            _comboTimeCounter = comboTimeOut;
        }
        hitText.transform.DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                hitText.transform.DOScale(Vector3.one, tweenDuration);
            });
    }
    
    
    private void OnEveryHitCombo()
    {
        PlayerCharacter.Instance.IncreaseScoreMultiply(0.1f, maxScoreMultiply);
    }

    [Button]
    private void OnEveryFifteenHitCombo()
    {
        Debug.Log("Every 15 Hits! Heal");
        PlayerCharacter.Instance.AddHealth(1);
    }

    private void OnEveryTwentyFiveHitCombo()
    {
        Debug.Log("Every 25 Hits! Damage Boost");
        voiceFeedback.PlayFeedbacks();
        PlayerCharacter.Instance.DamageBoost(PlayerCharacter.Instance);
    }

}
