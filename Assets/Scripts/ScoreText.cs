using System;
using System.Collections;
using Characters;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScoreText : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public float tweenDuration = 0.5f;
    public float scaleAmount = 1.2f;
    private static float Score => PlayerCharacter.Instance.Score;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private StageManager _stageManager;

    [SerializeField] private TextMeshProUGUI killTarget;
    [SerializeField] private TextMeshProUGUI scoreTarget;
    [SerializeField] private TextMeshProUGUI lifeTarget;
    [SerializeField] private GameObject quotaPanel;

    private IEnumerator Start()
    {
        yield return null;
        PlayerCharacter.Instance.onPickUpScore.AddListener(PlayTween);
        levelText.text = $"Level {_stageManager.currentStage}";
        scoreText.text = "Score " + 0;
        
        killTarget.text = _stageManager.stageLabels[_stageManager.currentStage].killQuota.ToString();
        scoreTarget.text = _stageManager.stageLabels[_stageManager.currentStage].scoreQuota.ToString();   
        lifeTarget.text = _stageManager.stageLabels[_stageManager.currentStage].lifeQuota.ToString();
        
        
    }

    private void UpdateScoreText()
    {
        if (!scoreText) return;
        scoreText.text = "Score " + Score;
    }
    
    private void PlayTween()
    {
        if (!scoreText) return;
        UpdateScoreText();
        scoreText.transform.DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                scoreText.transform.DOScale(Vector3.one, tweenDuration);
            });
    }
    
}
