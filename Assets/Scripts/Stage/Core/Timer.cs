using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using MoreMountains.Tools;
using Sirenix.OdinInspector;

public class Timer : MMSingleton<Timer>
{
    #region Inspector & Fleid
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float startTimer;
    private StageManager stageManager;
    
    private float currentTimer;
    private bool isPaused = false;
    private bool isRunning = false;
    private Coroutine countdownCoroutine;


    #endregion

    #region Properties
    
    public float GlobalTimer => currentTimer;
    public float GlobalTimerDown => startTimer - currentTimer;

    public float StartTimer => startTimer;
    
    /// <summary>
    /// Pauses the timer.
    /// </summary>
    [FoldoutGroup("Time Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void PauseTimer() => isPaused = true;
    
    /// <summary>
    /// Resume the timer.
    /// </summary>
    [FoldoutGroup("Time Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void ResumeTimer() => isPaused = false;
    [FoldoutGroup("Time Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    
    /// <summary>
    /// Reset timer to start timer.
    /// </summary>
    public void ResetTimer() => currentTimer = startTimer;
    public void TogglePause() => isPaused = !isPaused;
    public bool IsPaused() => isPaused;

    /// <summary>
    /// Set timer to start timer.
    /// </summary>
    /// <param name="timer"></param>
    public void SetTimer(float timer)
    {
        startTimer = timer;
        currentTimer = startTimer;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        countdownCoroutine = StartCoroutine(StartCountdown());
    }


    
    /// <summary>
    /// Stop the timer and resume after delay
    /// </summary>
    /// <param name="time"></param>
    public async void StopDelayTimer(float time) { PauseTimer(); await UniTask.Delay(TimeSpan.FromSeconds(time)); ResumeTimer(); }

    #endregion

    #region Unity Methods

    private void Start()
    {
        stageManager = GetComponent<StageManager>();
    }

    #endregion

    #region Methods
    /// <summary>
    /// Starts the countdown asynchronously and updates the timer value.
    /// </summary>
    private IEnumerator StartCountdown()
    {
        isRunning = true;

        while (isRunning && currentTimer > 0f)
        {
            while (isPaused || Time.timeScale == 0f || UnityEditor.EditorApplication.isPaused)
            {
                yield return null;
            }

            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                TimerEnd();
                yield break;
            }

            UpdateUIText();
            yield return null;
        }
    }



    /// <summary>
    /// Updates the timer text in the UI.
    /// </summary>
    private void UpdateUIText()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimer / 60);
            int seconds = Mathf.FloorToInt(currentTimer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    /// <summary>
    /// Stop countdown
    /// </summary>
    public void StopCountdown()
    {
        isRunning = false;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }


    
    /// <summary>
    /// Handles the end of the timer by stopping spawning and clearing enemies.
    /// </summary>
    private void TimerEnd()
    {
        isRunning = false;
        stageManager.GameTimeEnd();
        StopCoroutine(countdownCoroutine);
    }
    
    #endregion
}
