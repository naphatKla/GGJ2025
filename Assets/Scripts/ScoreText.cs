using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScoreText : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public float tweenDuration = 0.5f;
    public float scaleAmount = 1.2f;
    private static float Score => Player.Instance.Score;

    private void OnEnable()
    {
        Player.Instance.onPickUpScore.AddListener(PlayTween);
    }

    private void OnDisable()
    {
        Player.Instance.onPickUpScore.RemoveListener(PlayTween);
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
