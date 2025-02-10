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
    private float _score => PlayerCharacter.HitCombo;
    private float _scoreMultiply => PlayerCharacter.Instance.ScoreMultiply;
    [SerializeField] private float maxScoreMultiply;
    public MMF_Player voiceFeedback;
    private int _lastTriggeredScore = -1;

    void Start()
    {
        if (hitText != null) {PlayerCharacter.OnHitComboChanged.AddListener(PlayTween);}
    }

    private void Update()
    {
        UpdateScoreText();
        UpdateComboTime();
        UpdateComboUI();
        
        if (comboTimeoutSlider.value <= 0)
        {
            isCombo = false;
            PlayerCharacter.HitCombo = 0;
            PlayerCharacter.Instance.ResetscoreMultiply();
        }
        
        if (_score > 0 && _score % 5 == 0 && _score != _lastTriggeredScore)
        {
            OnEveryFiveHitcombo();
            _lastTriggeredScore = (int)_score;
        }
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
    
    
    private void OnEveryFiveHitcombo()
    {
        Debug.Log("Every 5 Hitcombo!");
        voiceFeedback.PlayFeedbacks();
        PlayerCharacter.Instance.IncreaseScoreMultiply(0.1f, maxScoreMultiply);
    }
}
