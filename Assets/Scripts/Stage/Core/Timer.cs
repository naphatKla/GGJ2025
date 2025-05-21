using System;
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

    #endregion

    #region Properties
    
    public float GlobalTimer => currentTimer;
    public float StartTimer => startTimer;
    
    [FoldoutGroup("Time Control"), Button(ButtonSizes.Large), GUIColor(1, 1, 0)]
    public void PauseTimer() => isPaused = true;
    [FoldoutGroup("Time Control"), Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void ResumeTimer() => isPaused = false;
    [FoldoutGroup("Time Control"), Button(ButtonSizes.Large), GUIColor(1, 0, 0)]
    public void ResetTimer() => currentTimer = startTimer;
    public void TogglePause() => isPaused = !isPaused;
    public bool IsPaused() => isPaused;
    public void SetTimer(float timer) { startTimer = timer; ResetTimer(); }
    
    #endregion

    #region Unity Methods

    private void Start()
    {
        stageManager = GetComponent<StageManager>();
        ResetTimer();
        StartCountdownAsync().Forget();
    }
    #endregion

    #region Methods
    public async UniTaskVoid StartCountdownAsync()
    {
        isRunning = true;
        while (isRunning)
        {
            if (!IsSpawnerStoppedOrPaused() && !isPaused)
            {
                if (currentTimer > 0)
                {
                    currentTimer -= Time.deltaTime;

                    if (currentTimer <= 0)
                    {
                        currentTimer = 0;
                        TimerEnd();
                        break;
                    }
                }
            }
            UpdateUIText();
            await UniTask.Yield();
        }
    }

    private void UpdateUIText()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimer / 60);
            int seconds = Mathf.FloorToInt(currentTimer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private bool IsSpawnerStoppedOrPaused()
    {
        return stageManager.IsSpawningStoppedOrPaused();
    }

    private void TimerEnd()
    {
        isRunning = false;
        stageManager.StopSpawning();
        stageManager.ClearEnemies();
    }
    #endregion
}
