using System.Collections;
using System.Collections.Generic;
using Characters;
using DG.Tweening;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboText : MonoBehaviour
{
    public TextMeshProUGUI hitText;
    public TextMeshProUGUI scoreMultiplyText;
    public float tweenDuration = 0.1f;
    public float scaleAmount = 1.2f;
    public int comboTimeOut = 100;
    public int comboMultiplyTimeOut = 10;
    public int maxcomboMultiplyTimeOut = 100;
    public Slider comboTimeoutSlider;
    private bool isCombo = false;
    private float _score => PlayerCharacter.Instance.HitCombo;
    private float _scoreMultiply => PlayerCharacter.Instance.ScoreMultiply;
    [SerializeField] private float maxScoreMultiply;
    public MMF_Player voiceFeedback;
    private int _lastTriggeredScore = -1;

    void Start()
    {
        if (hitText != null) {PlayerCharacter.OnHitComboChanged.AddListener(PlayTween);}
        if (hitText != null) {PlayerCharacter.Instance.OnTakeDamage.AddListener(ResetCombo);}
    }

    private void Update()
    {
        UpdateScoreText();
        UpdateComboTime();
        UpdateComboUI();
        
        if (comboTimeoutSlider.value <= 0)
        {
            ResetCombo();
        }

        if (_score > 0 && _score % 5 == 0 && _score != _lastTriggeredScore)
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
                OnEveryTenHitCombo();
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
        scoreMultiplyText.text = "Score x " + _scoreMultiply;
    }
    
    private void UpdateComboTime()
    {
        if (isCombo)
        {
            float decayRate = Mathf.Clamp(comboMultiplyTimeOut + (_score * 0.5f), 1, maxcomboMultiplyTimeOut);
            comboTimeoutSlider.value -= decayRate * Time.deltaTime;
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
        }
        hitText.transform.DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                hitText.transform.DOScale(Vector3.one, tweenDuration);
            });
    }
    
    
    private void OnEveryTenHitCombo()
    {
        Debug.Log("Every 5 Hits!");
        voiceFeedback.PlayFeedbacks();
        PlayerCharacter.Instance.IncreaseScoreMultiply(0.1f, maxScoreMultiply);
    }

    private void OnEveryFifteenHitCombo()
    {
        Debug.Log("Every 15 Hits! Heal");
        PlayerCharacter.Instance.Heal(PlayerCharacter.Instance);
    }

    private void OnEveryTwentyFiveHitCombo()
    {
        Debug.Log("Every 25 Hits! Damage Boost");
        voiceFeedback.PlayFeedbacks();
        PlayerCharacter.Instance.DamageBoost(PlayerCharacter.Instance);
    }

}
