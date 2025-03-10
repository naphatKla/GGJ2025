using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using MoreMountains.Feedbacks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class EndGameUI : MonoBehaviour
{
    #region Inspectors & Fields
    [SerializeField] private StageManager _stageManager;
    [Header("WinUI")]
    [SerializeField] private GameObject[] stars;
    [SerializeField] private GameObject[] items;
    [SerializeField] private TextMeshProUGUI scoreQuota;
    [SerializeField] private TextMeshProUGUI killQuota;
    [SerializeField] private TextMeshProUGUI lifeQuota;
    [SerializeField] private GameObject nextLevelButton;
    
    [Header("Sound")]
    [SerializeField] private MMF_Player starsSound;
    [SerializeField] private MMF_Player scoreSound;
    [SerializeField] private MMF_Player bubbleSound;
    [SerializeField] private MMF_Player winSound;
    [SerializeField] private MMF_Player loseSound;
    #endregion -------------------------------------------------------------------------------------------------------------------
    
    #region Properties
    private int _count = 0;
    private float firstTimeOpenTime = 0.01f;
    private int starCount = 0;
    private StageClass LevelQuota;
    private LevelSelector levelSelector;
    private Coroutine buttonCoroutine;
    private bool canClick;
    #endregion -------------------------------------------------------------------------------------------------------------------
    
    #region UnityMethods
    private void Start()
    {
        buttonCoroutine = StartCoroutine(PlayGameObject());
        levelSelector = FindObjectOfType<LevelSelector>();
        LevelQuota = _stageManager.stageLabels[_stageManager.currentStage];
        scoreQuota.text = LevelQuota.scoreQuota.ToString();
        killQuota.text = LevelQuota.killQuota.ToString();
        lifeQuota.text = $"x {LevelQuota.lifeQuota.ToString()}";
        StarsCount();

        StartCoroutine(EnableClickAfterDelay());
        StartCoroutine(StarScore());
    }

    private void Update()
    {
        if (canClick == false) return;
        if (Input.GetMouseButtonDown(0))
        {
            bubbleSound.PlayFeedbacks();
            StopCoroutine(buttonCoroutine);
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
    #endregion -------------------------------------------------------------------------------------------------------------------

    #region Methods
    private void StarsCount()
    {
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
    
        if (starCount == 0 || levelSelector.level >= 6)
            nextLevelButton.SetActive(false);
        
        levelSelector.SetScoreOnLevel(_stageManager.currentStage-1, starCount);
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
    
    private IEnumerator EnableClickAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        canClick = true;
    }

    #endregion -------------------------------------------------------------------------------------------------------------------
    
}
