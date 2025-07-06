using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GameControl
{
    [Serializable]
    public class TimerTrigger
    {
        public float triggerTime;
        public bool triggered = false; 
        public Action callback;

        public TimerTrigger(float time, Action cb)
        {
            triggerTime = time;
            callback = cb;
        }
    }

    public class GameTimer : MMSingleton<GameTimer>
    {
        #region Inspector & Fleid

        [BoxGroup("Game Timer")]
        [SerializeField] private TMP_Text timerText;
        [BoxGroup("Game Timer")]
        [SerializeField] private float startTimer;
        
        [BoxGroup("Countdown Timer")]
        [SerializeField] private TMP_Text countdownText;
        private CancellationTokenSource cts;

        private bool _isPaused;
        private bool _isRunning;
        private Coroutine _countdownCoroutine;
        private List<TimerTrigger> _timeTriggers = new();

        #endregion
        
        [ShowInInspector, ReadOnly]
        public float GlobalTimer { get; private set; }
        public float GlobalTimerDown => startTimer - GlobalTimer;
        public float StartTimerNumber => startTimer;
        public event Action OnTimerEnded;
        
        [Button(ButtonSizes.Large)]
        [GUIColor(1, 1, 0)]
        public void PauseTimer()
        {
            _isPaused = true;
            UpdateUIText();
        }
        
        [Button(ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void StartTimer()
        {
            _isPaused = false;
            if (_countdownCoroutine == null) 
                _countdownCoroutine = StartCoroutine(StartCountdown());
            UpdateUIText();
        }
        
        [Button(ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void ResumeTimer()
        {
            _isPaused = false;
            UpdateUIText();
        }
        
        [Button(ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        public void ResetTimer()
        {
            GlobalTimer = startTimer;
            PauseTimer();
            UpdateUIText();
        }
        
        [Button(ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        public void StopTimer()
        {
            StopCountdown();
        }
        
        private void OnDestroy()
        {
            cts?.Cancel();
        }

        public bool IsPaused()
        {
            return _isPaused;
        }
        
        public void SetTimer(float timer)
        {
            startTimer = timer;
            GlobalTimer = startTimer;
            UpdateUIText();
        }
        
        public void ScheduleTrigger(float timeInSeconds, Action callback)
        {
            _timeTriggers.Add(new TimerTrigger(timeInSeconds, callback));
        }
        
        public void ScheduleLoopingTrigger(float intervalSeconds, float totalDurationSeconds, Action callback)
        {
            int count = Mathf.FloorToInt(totalDurationSeconds / intervalSeconds);
            for (int i = 1; i <= count; i++)
            {
                float triggerTime = totalDurationSeconds - i * intervalSeconds;
                ScheduleTrigger(triggerTime, callback);
            }
        }
        
        private void Start()
        {
            _countdownCoroutine = null;
        }

        private IEnumerator StartCountdown()
        {
            _isRunning = true;

            while (_isRunning && GlobalTimer > 0f)
            {
                while (_isPaused || Time.timeScale == 0f || EditorApplication.isPaused) yield return null;
                GlobalTimer -= Time.deltaTime;

                foreach (var trigger in _timeTriggers)
                    if (!trigger.triggered && GlobalTimer <= trigger.triggerTime)
                    {
                        trigger.triggered = true;
                        trigger.callback?.Invoke();
                    }

                if (GlobalTimer <= 0f)
                {
                    GlobalTimer = 0f;
                    TimerEnd();
                    yield break;
                }

                UpdateUIText();
                yield return null;
            }
        }

        public async UniTask StartCountdownAsync(float seconds)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();

            if (countdownText != null) countdownText.gameObject.SetActive(true);

            while (seconds > 0f)
            {
                var displayNum = Mathf.CeilToInt(seconds);

                if (countdownText != null)
                {
                    countdownText.text = displayNum.ToString();
                    countdownText.transform.localScale = Vector3.one * 2.5f;
                    countdownText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
                }

                await UniTask.Delay(1000, cancellationToken: cts.Token);
                seconds -= 1f;
            }

            if (countdownText != null)
            {
                countdownText.text = "Start!";
                countdownText.transform.localScale = Vector3.one * 3f;
                countdownText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }

            await UniTask.Delay(1000, cancellationToken: cts.Token);

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }

        
        private void UpdateUIText()
        {
            if (timerText != null)
            {
                var minutes = Mathf.FloorToInt(GlobalTimer / 60);
                var seconds = Mathf.FloorToInt(GlobalTimer % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
        
        public void StopCountdown()
        {
            _isRunning = false;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }
        
        public void ClearAllTriggers()
        {
            _timeTriggers.Clear();
        }
        
        private void TimerEnd()
        {
            _isRunning = false;
            StopCoroutine(_countdownCoroutine);
            OnTimerEnded?.Invoke();
        }
    }
}