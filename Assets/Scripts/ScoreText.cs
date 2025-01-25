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
    private float _score => Player.Instance.Score;

    void Start()
    {
        if (scoreText != null) {Player.Instance.onPickUpScore.AddListener(PlayTween);}
    }

    private void Update()
    {
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score " + _score;
    }
    
    private void PlayTween()
    {
        scoreText.transform.DOScale(new Vector3(scaleAmount, scaleAmount, 1), tweenDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                scoreText.transform.DOScale(Vector3.one, tweenDuration);
            });
    }
}
