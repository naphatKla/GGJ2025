using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace GameControl
{
    [Serializable]
    public class TimerTrigger
    {
        public string id;
        public float triggerTime;
        public bool triggered = false; 
        public bool triggerWhenSkip = true;
        public Action callback;

        public TimerTrigger(float time, Action cb, string id = null, bool triggerWhenSkip = true)
        {
            triggerTime = time;
            callback = cb;
            this.id = id;
            this.triggerWhenSkip = triggerWhenSkip;
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
        private readonly HashSet<string> _registeredIds = new();

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
            foreach (var t in _timeTriggers) t.triggered = false;
            PauseTimer();
            UpdateUIText();
        }
        
        [Button(ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        public void StopTimer()
        {
            StopCountdown();
        }
        
        [Button(ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void SkipTime(float time)
        {
            SkipByDeltaSeconds(time);
        }
        
        private void OnDestroy()
        {
            cts?.Cancel();
        }

        public bool IsPaused()
        {
            return _isPaused;
        }
        
        public void ResetAndStop()
        {
            StopCountdown();
            GlobalTimer = startTimer;
            foreach (var t in _timeTriggers) t.triggered = false;
            _isPaused = true;
            UpdateUIText();
        }
        
        public void ResetAndStart()
        {
            ResetAndStop();
            StartTimer();
        }
        
        public void SetTimer(float timer)
        {
            startTimer = timer;
            GlobalTimer = startTimer;
            UpdateUIText();
        }
        
        protected override void Awake()
        {
            GlobalTimer = startTimer;
            UpdateUIText();
            base.Awake();
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
                while (_isPaused || Time.timeScale == 0f) yield return null;
                GlobalTimer -= Time.deltaTime;

                TriggerTheEvent();
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

        private void TriggerTheEvent()
        {
            var snapshot = _timeTriggers;
            for (var i = 0; i < _timeTriggers.Count; i++)
            {
                var trigger = _timeTriggers[i];
                if (!trigger.triggered && GlobalTimer <= trigger.triggerTime)
                {
                    trigger.triggered = true;
                    trigger.callback?.Invoke();
                }
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
                    countdownText.transform.DOScale(1.25f, 0.5f).SetEase(Ease.OutBack);
                }

                await UniTask.Delay(1000, cancellationToken: cts.Token);
                seconds -= 1f;
            }

            if (countdownText != null)
            {
                countdownText.text = $"<color=yellow>START!";
                countdownText.transform.localScale = Vector3.one * 3f;
                countdownText.transform.DOScale(1.25f, 0.5f).SetEase(Ease.OutBack);
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
            _registeredIds.Clear();
        }
        
        private void TimerEnd()
        {
            _isRunning = false;
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
            OnTimerEnded?.Invoke();
        }

        #region Skip Time

        public void SkipToRemaining(float targetRemaining, bool invokeEnd = true, bool fireInOrder = true)
        {
            targetRemaining = Mathf.Clamp(targetRemaining, 0f, startTimer);
            var from = GlobalTimer;
            var to = targetRemaining;

            if (to < from)
            {
                var due = new List<TimerTrigger>();
                var n = _timeTriggers.Count;

                for (var i = 0; i < n; i++)
                {
                    var tr = _timeTriggers[i];
                    if (!tr.triggered && tr.triggerTime <= from && tr.triggerTime >= to)
                    {
                        if (tr.triggerWhenSkip)
                            due.Add(tr);
                        else
                            tr.triggered = true;
                    }
                }

                if (fireInOrder) due.Sort((a, b) => b.triggerTime.CompareTo(a.triggerTime));

                foreach (var tr in due)
                {
                    GlobalTimer = tr.triggerTime;
                    try
                    {
                        tr.triggered = true;
                        tr.callback?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            GlobalTimer = to;
            UpdateUIText();
            TriggerTheEvent();

            if (GlobalTimer <= 0f)
            {
                GlobalTimer = 0f;
                if (invokeEnd)
                    TimerEnd();
            }
        }
        
        public void SkipByDeltaSeconds(float secondsFromNow, bool invokeEnd = true)
        {
            if (secondsFromNow <= 0f) return;
            SkipToRemaining(Mathf.Max(GlobalTimer - secondsFromNow, 0f), invokeEnd);
        }
        
        public void SkipToElapsed(float elapsedSeconds, bool invokeEnd = true)
        {
            elapsedSeconds = Mathf.Max(elapsedSeconds, 0f);
            var targetRemaining = Mathf.Max(startTimer - elapsedSeconds, 0f);
            SkipToRemaining(targetRemaining, invokeEnd);
        }
        
        public void SkipToPercentElapsed(float percent01, bool invokeEnd = true)
        {
            percent01 = Mathf.Clamp01(percent01);
            var targetRemaining = Mathf.Max(startTimer * (1f - percent01), 0f);
            SkipToRemaining(targetRemaining, invokeEnd);
        }

        #endregion
        
        #region Trigger Event
           
        /// <summary>
        /// Trigger once when time pass X Second (remaining) (60 = last 1 minute)
        /// ใส่ id เพื่อกันลงซ้ำได้ (ถ้าไม่ใส่ก็ไม่กัน)
        /// </summary>
        /// <param name="remainingSeconds"></param>
        /// <param name="callback"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public TimerTrigger ScheduleOnceAtRemaining(float remainingSeconds, Action callback, string id = null, bool triggerWhenSkip = true)
        {
            remainingSeconds = Mathf.Max(remainingSeconds, 0f);

            if (!string.IsNullOrEmpty(id) && _registeredIds.Contains(id)) return null;

            var trig = new TimerTrigger(remainingSeconds, callback, id, triggerWhenSkip);
            _timeTriggers.Add(trig);

            if (!string.IsNullOrEmpty(id)) _registeredIds.Add(id);
            return trig;
        }
        
        /// <summary>
        /// Trigger once when time pass X Second (elapsed)
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        /// <param name="callback"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public TimerTrigger ScheduleOnceAtElapsed(float elapsedSeconds, Action callback, string id = null, bool triggerWhenSkip = true)
        {
            var remaining = Mathf.Max(startTimer - elapsedSeconds, 0f);
            return ScheduleOnceAtRemaining(remaining, callback, id, triggerWhenSkip);
        }

        /// <summary>
        /// Trigger once when time pass in percent (0..1) of total time
        /// </summary>
        /// <param name="percent01"></param>
        /// <param name="callback"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public TimerTrigger ScheduleOnceAtPercentElapsed(float percent01, Action callback, string id = null, bool triggerWhenSkip = true)
        {
            percent01 = Mathf.Clamp01(percent01);
            var remaining = Mathf.Max(startTimer * (1f - percent01), 0f);
            return ScheduleOnceAtRemaining(remaining, callback, id, triggerWhenSkip);
        }
        
        public void ScheduleTrigger(float timeInSeconds, Action callback, bool triggerWhenSkip = true)
        {
            _timeTriggers.Add(new TimerTrigger(timeInSeconds, callback, id: null, triggerWhenSkip: triggerWhenSkip));
        }
        
        public void ScheduleLoopingTrigger(float intervalSeconds, float totalDurationSeconds, Action callback, bool triggerWhenSkip = true)
        {
            int count = Mathf.FloorToInt(totalDurationSeconds / intervalSeconds);
            for (int i = 1; i <= count; i++)
            {
                float triggerTime = totalDurationSeconds - i * intervalSeconds;
                ScheduleTrigger(triggerTime, callback, triggerWhenSkip);
            }
        }
        #endregion
    }
}