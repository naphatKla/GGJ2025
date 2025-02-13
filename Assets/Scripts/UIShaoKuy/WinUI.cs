using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using MoreMountains.Feedbacks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class WinUI : MonoBehaviour
{
    [SerializeField] private GameObject[] stars;
    [SerializeField] private GameObject[] items;
    private int _count = 0;
    private float firstTimeOpenTime = 0.01f;
    [SerializeField] private MMF_Player starsSound;
    [SerializeField] private MMF_Player scoreSound;
    [SerializeField] private MMF_Player bubbleSound;
    [SerializeField] private MMF_Player winSound;
    [SerializeField] private MMF_Player loseSound;
    [SerializeField] private StageManager _stageManager;
    private int starCount = 0;

    [SerializeField] private TextMeshProUGUI scoreQuota;
    [SerializeField] private TextMeshProUGUI killQuota;
    [SerializeField] private TextMeshProUGUI lifeQuota;
    [SerializeField] private GameObject nextLevelButton;

    private Coroutine dd;
    // Update is called once per frame
    private void Start()
    {
        dd = StartCoroutine(PlayGameObject());
        
        StageClass LevelQuota = _stageManager.stageLabels[_stageManager.currentStage];
        scoreQuota.text = LevelQuota.scoreQuota.ToString();
        killQuota.text = LevelQuota.killQuota.ToString();
        lifeQuota.text = $"x {LevelQuota.lifeQuota.ToString()}";

        if (PlayerCharacter.Instance.Score >= LevelQuota.scoreQuota)
        {
            winSound.PlayFeedbacks();
            starCount++;
            Debug.Log("Score Quota Reached");
        }
        else
        {
            loseSound.PlayFeedbacks();
        }
        if (PlayerCharacter.Instance.Kill >= LevelQuota.killQuota)
        {
            starCount++;
            Debug.Log("kill Quota Reached");
        }
        if (PlayerCharacter.Instance.Life >= LevelQuota.lifeQuota)
        {
            starCount++;
            Debug.Log("Life Quota Reached");
        }
        if (starCount == 0)
            nextLevelButton.SetActive(false);
        LevelSelector levelSelector =
            FindObjectOfType<LevelSelector>();
        levelSelector.SetScoreOnLevel(_stageManager.currentStage-1, starCount);

        StartCoroutine(StarScore());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bubbleSound.PlayFeedbacks();
            StopCoroutine(dd);
            if (_count > 0 && _count - 1 < items.Length) // เช็คก่อนเข้าถึง
            {
                if (items[_count - 1].TryGetComponent<AddNumberLoop>(out AddNumberLoop anl))
                {
                    anl.SetToTargetNumber();
                    anl.StopAllCoroutines();
                }
            }
            
            if (_count < items.Length)
            {
                ShowGameObject();
                StartCoroutine(PlayGameObject());
            }
        }
    }


    private IEnumerator PlayGameObject()
    {
        yield return new WaitForSeconds(firstTimeOpenTime);

        if (_count < items.Length)  // เช็คก่อนเข้าถึง
        {
            ShowGameObject();
        }
    
        if (_count >= items.Length) // หยุด coroutine ถ้าเกินขอบเขต
        {
            yield break;
        }

        firstTimeOpenTime = 1;
        StartCoroutine(PlayGameObject());
    }

    private void ShowGameObject()
    {
        if (_count < items.Length)  // ตรวจสอบก่อนเข้าถึง index
        {
            items[_count].SetActive(true);
            scoreSound.PlayFeedbacks();
            _count++;
        }
    }

    IEnumerator StarScore()
    {
        for (int i = 0; i < starCount; i++)
        {
            stars[i].SetActive(true);
            starsSound.PlayFeedbacks();
            yield return new WaitForSeconds(1f);
        }
    }
}
