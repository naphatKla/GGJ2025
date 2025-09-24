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
        public string groupId;

        public float triggerAt;
        public GameTimer.TriggerSpace space = GameTimer.TriggerSpace.Remaining;

        public bool triggered;
        public bool triggerWhenSkip = true;
        public Action callback;

        public TimerTrigger(float at, Action cb, string id = null, string groupId = null,
            bool triggerWhenSkip = true, GameTimer.TriggerSpace space = GameTimer.TriggerSpace.Remaining)
        {
            triggerAt = at;
            callback = cb;
            this.id = id;
            this.groupId = groupId;
            this.triggerWhenSkip = triggerWhenSkip;
            this.space = space;
        }
    }

    public class GameTimer : MMSingleton<GameTimer>
    {
        #region Inspector & Fleid

        [BoxGroup("Game Timer")] [SerializeField]
        private TMP_Text timerText;

        [BoxGroup("Game Timer")] [SerializeField]
        private float startTimer;

        [BoxGroup("Game Timer")] [SerializeField]
        private TimerMode _mode = TimerMode.Countdown;

        [BoxGroup("Countdown Timer")] [SerializeField]
        private TMP_Text countdownText;

        private CancellationTokenSource cts;

        private bool _isPaused;
        private bool _isRunning;
        private Coroutine _countdownCoroutine;
        private readonly List<TimerTrigger> _timeTriggers = new();
        private readonly HashSet<string> _registeredIds = new();

        public enum TimerMode
        {
            Countdown,
            Countup
        }

        public enum TriggerSpace
        {
            Elapsed,
            Remaining
        }

        #endregion

        [ShowInInspector] [ReadOnly] public float GlobalTimer { get; private set; }
        public float GlobalTimerDown => startTimer - GlobalTimer;
        public float StartTimerNumber => startTimer;
        public event Action OnTimerEnded;
        public event Action OnTimeChanged;
        public event Action OnTimeSkipped;
        
        public TimerMode Countmode { get => _mode; set => _mode = value;}

        public float Elapsed
            => _mode == TimerMode.Countdown ? Mathf.Max(StartTimerNumber - GlobalTimer, 0f) : GlobalTimer;

        public float Remaining
            => _mode == TimerMode.Countdown
                ? Mathf.Max(GlobalTimer, 0f)
                : StartTimerNumber > 0f
                    ? Mathf.Max(StartTimerNumber - GlobalTimer, 0f)
                    : float.PositiveInfinity;

        [Button(ButtonSizes.Large)]
        [GUIColor(1, 1, 0)]
        public void PauseTimer()
        {
            _isPaused = true;
            UpdateUIText();
            OnTimeChanged?.Invoke();
        }

        [Button(ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void StartTimer()
        {
            _isPaused = false;
            if (_countdownCoroutine == null)
                _countdownCoroutine = StartCoroutine(StartTimerLoop());
            UpdateUIText();
            OnTimeChanged?.Invoke();
        }

        [Button(ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void ResumeTimer()
        {
            _isPaused = false;
            UpdateUIText();
            OnTimeChanged?.Invoke();
        }

        [Button(ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        public void ResetTimer()
        {
            GlobalTimer = startTimer;
            foreach (var t in _timeTriggers) t.triggered = false;
            PauseTimer();
            UpdateUIText();
            OnTimeChanged?.Invoke();
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

        [Button]
        [GUIColor(0, 1, 0)]
        public void StartCountdown()
        {
            _mode = TimerMode.Countdown;
            if (_countdownCoroutine == null) _countdownCoroutine = StartCoroutine(StartTimerLoop());
        }

        [Button]
        [GUIColor(0, 1, 0)]
        public void StartCountup()
        {
            _mode = TimerMode.Countup;
            if (_countdownCoroutine == null) _countdownCoroutine = StartCoroutine(StartTimerLoop());
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
            OnTimeChanged?.Invoke();
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
            OnTimeChanged?.Invoke();
        }

        protected override void Awake()
        {
            GlobalTimer = startTimer;
            UpdateUIText();
            OnTimeChanged?.Invoke();
            base.Awake();
        }

        private void Start()
        {
            _countdownCoroutine = null;
        }

        private IEnumerator StartTimerLoop()
        {
            _isRunning = true;
            while (_isRunning)
            {
                while (_isPaused || Time.timeScale == 0f) yield return null;

                if (_mode == TimerMode.Countdown)
                {
                    GlobalTimer -= Time.deltaTime;
                    TriggerTheEvent();
                    if (GlobalTimer <= 0f)
                    {
                        GlobalTimer = 0f;
                        TimerEnd();
                        yield break;
                    }
                }
                // Countup
                else
                {
                    GlobalTimer += Time.deltaTime;
                    TriggerTheEvent();
                }

                UpdateUIText();
                OnTimeChanged?.Invoke();
                yield return null;
            }
        }

        private void TriggerTheEvent()
        {
            float elapsed   = (_mode == TimerMode.Countdown) ? Mathf.Max(startTimer - GlobalTimer, 0f) : GlobalTimer;
            float remaining = (_mode == TimerMode.Countdown) ? Mathf.Max(GlobalTimer, 0f)
                : float.PositiveInfinity;

            for (int i = 0; i < _timeTriggers.Count; i++)
            {
                var t = _timeTriggers[i];
                if (t.triggered) continue;

                bool fire =
                    (t.space == TriggerSpace.Elapsed   && elapsed   >= t.triggerAt) ||
                    (t.space == TriggerSpace.Remaining && remaining <= t.triggerAt);

                if (!fire) continue;

                t.triggered = true;
                try { t.callback?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
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
                countdownText.text = "<color=yellow>START!";
                countdownText.transform.localScale = Vector3.one * 3f;
                countdownText.transform.DOScale(1.25f, 0.5f).SetEase(Ease.OutBack);
            }

            await UniTask.Delay(1000, cancellationToken: cts.Token);

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }


        private void UpdateUIText()
        {
            if (timerText == null) return;

            float secondsToShow = Mathf.Max(GlobalTimer, 0f);

            int total = Mathf.FloorToInt(secondsToShow);
            if (total >= 3600)
            {
                int h = total / 3600;
                int m = (total % 3600) / 60;
                int s = total % 60;
                timerText.text = $"{h:00}:{m:00}:{s:00}";
            }
            else
            {
                int m = total / 60;
                int s = total % 60;
                timerText.text = $"{m:00}:{s:00}";
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

        // COUNTDOWN
        public void SkipToRemaining(float targetRemaining, bool invokeEnd = true)
        {
            targetRemaining = Mathf.Clamp(targetRemaining, 0f, startTimer);
            var from = GlobalTimer;
            var to = targetRemaining;

            if (to < from)
                CatchUpRemaining(from, to);

            GlobalTimer = to;
            UpdateUIText();
            TriggerTheEvent();
            OnTimeChanged?.Invoke();
            OnTimeSkipped?.Invoke();

            if (GlobalTimer <= 0f)
            {
                GlobalTimer = 0f;
                if (invokeEnd) TimerEnd();
            }
        } 
        
        // ENDLESS/COUNTUP (+รองรับตอนอยู่โหมด Countdown แต่จับที่แกน Elapsed)
        public void SkipToElapsed(float targetElapsed)
        {
            var from = _mode == TimerMode.Countdown
                ? Mathf.Max(startTimer - GlobalTimer, 0f)
                : Mathf.Max(GlobalTimer, 0f);
            var to = Mathf.Max(targetElapsed, 0f);
            if (to > from)
                CatchUpElapsed(from, to);

            // เซ็ตเวลามาที่ปลายทาง
            GlobalTimer = _mode == TimerMode.Countdown
                ? Mathf.Max(startTimer - to, 0f)
                : to;

            UpdateUIText();
            TriggerTheEvent();
            OnTimeChanged?.Invoke();
            OnTimeSkipped?.Invoke();
        }


        // ===== Helper: ยิงทริกเกอร์ใหม่ที่ถูกสร้างเพิ่มระหว่าง callback จนกว่าจะครบ =====
        private void CatchUpElapsed(float from, float to)
        {
            while (true)
            {
                TimerTrigger next = null;
                var best = float.PositiveInfinity;
                for (var i = 0; i < _timeTriggers.Count; i++)
                {
                    var tr = _timeTriggers[i];
                    if (tr.triggered || tr.space != TriggerSpace.Elapsed) continue;
                    if (tr.triggerAt <= from || tr.triggerAt > to) continue;
                    if (!tr.triggerWhenSkip)
                    {
                        tr.triggered = true;
                        continue;
                    }

                    if (tr.triggerAt < best)
                    {
                        best = tr.triggerAt;
                        next = tr;
                    }
                }

                if (next == null) break;
                GlobalTimer = _mode == TimerMode.Countdown
                    ? Mathf.Max(startTimer - next.triggerAt, 0f)
                    : next.triggerAt;

                try
                {
                    next.triggered = true;
                    next.callback?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                from = best;
            }
        }

        private void CatchUpRemaining(float from, float to)
        {
            while (true)
            {
                TimerTrigger next = null;

                var best = float.NegativeInfinity;
                for (var i = 0; i < _timeTriggers.Count; i++)
                {
                    var tr = _timeTriggers[i];
                    if (tr.triggered || tr.space != TriggerSpace.Remaining) continue;
                    if (tr.triggerAt > from || tr.triggerAt < to) continue;
                    if (!tr.triggerWhenSkip)
                    {
                        tr.triggered = true;
                        continue;
                    }

                    if (tr.triggerAt > best)
                    {
                        best = tr.triggerAt;
                        next = tr;
                    }
                }

                if (next == null) break;

                GlobalTimer = next.triggerAt;

                try
                {
                    next.triggered = true;
                    next.callback?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                from = best - Mathf.Epsilon;
            }
        }


        // ===== ตัวช่วย: ข้ามแบบระบุ delta เลือกโหมดให้เอง =====
        public void SkipByDeltaSeconds(float secondsFromNow, bool invokeEnd = true)
        {
            if (secondsFromNow <= 0f) return;

            if (_mode == TimerMode.Countdown)
            {
                SkipToRemaining(Mathf.Max(GlobalTimer - secondsFromNow, 0f), invokeEnd);
            }
            else // Countup / Endless
            {
                var elapsedNow = Mathf.Max(GlobalTimer, 0f);
                SkipToElapsed(elapsedNow + secondsFromNow);
            }
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
        public TimerTrigger ScheduleOnceAtRemaining(
            float remainingSeconds,
            Action callback,
            string id = null,
            bool triggerWhenSkip = true,
            string groupId = null)
        {
            remainingSeconds = Mathf.Max(remainingSeconds, 0f);

            if (!string.IsNullOrEmpty(id) && _registeredIds.Contains(id))
                return null;

            var trig = new TimerTrigger(remainingSeconds, callback, id, groupId, triggerWhenSkip);
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
        public TimerTrigger ScheduleOnceAtElapsed(
            float elapsedSeconds, Action callback, string id = null, bool triggerWhenSkip = true)
        {
            elapsedSeconds = Mathf.Max(elapsedSeconds, 0f);
            if (!string.IsNullOrEmpty(id) && _registeredIds.Contains(id)) return null;

            var trig = new TimerTrigger(elapsedSeconds, callback, id, null, triggerWhenSkip, TriggerSpace.Elapsed);
            _timeTriggers.Add(trig);
            if (!string.IsNullOrEmpty(id)) _registeredIds.Add(id);
            return trig;
        }

        /// <summary>
        ///     Trigger once when time pass in percent (0..1) of total time
        /// </summary>
        /// <param name="percent01"></param>
        /// <param name="callback"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public TimerTrigger ScheduleOnceAtPercentElapsed(
            float percent01, Action callback, string id = null, bool triggerWhenSkip = true)
        {
            percent01 = Mathf.Clamp01(percent01);
            var atElapsed = StartTimerNumber * percent01;
            return ScheduleOnceAtElapsed(atElapsed, callback, id, triggerWhenSkip);
        }

        public void ScheduleTrigger(float timeInSeconds, Action callback, bool triggerWhenSkip = true,
            string groupId = null)
        {
            _timeTriggers.Add(new TimerTrigger(timeInSeconds, callback, null, groupId, triggerWhenSkip));
        }

        public void ScheduleLoopingTrigger(bool endless, float intervalSeconds, float totalDurationSeconds,
            Action callback, bool triggerWhenSkip = true, string groupId = null)
        {
            if (!endless)
            {
                if (!string.IsNullOrEmpty(groupId)) CancelGroup(groupId);
                var count = Mathf.FloorToInt(totalDurationSeconds / intervalSeconds);
                for (var i = 1; i <= count; i++)
                {
                    var atRemaining = Mathf.Max(totalDurationSeconds - i * intervalSeconds, 0f);
                    _timeTriggers.Add(new TimerTrigger(atRemaining, callback, null, groupId, triggerWhenSkip));
                }
            }
            else
            {
                ScheduleEndless(intervalSeconds, callback, groupId, triggerWhenSkip);
            }
        }

        public void ScheduleEndless(float intervalSeconds, Action callback, string groupId = null,
            bool triggerWhenSkip = true)
        {
            intervalSeconds = Mathf.Max(0.01f, intervalSeconds);
            if (!string.IsNullOrEmpty(groupId)) CancelGroup(groupId);

            var next = Elapsed + intervalSeconds;

            void AddNext(float at)
            {
                _timeTriggers.Add(new TimerTrigger(
                    at,
                    () =>
                    {
                        callback?.Invoke();
                        AddNext(at + intervalSeconds);
                    },
                    null,
                    groupId,
                    triggerWhenSkip,
                    TriggerSpace.Elapsed));
            }

            AddNext(next);
        }


        public void CancelGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return;
            _timeTriggers.RemoveAll(t => !t.triggered && t.groupId == groupId);
        }

        // COUNTDOWN: เริ่มจาก "เวลาที่เหลือจริงตอนนี้" แล้วยิงทุก interval จนหมดเวลา
        public void ScheduleLoopingFromNow_Countdown(string groupId, float intervalSeconds, Action callback, bool triggerWhenSkip = true)
        {
            CancelGroup(groupId);
            intervalSeconds = Mathf.Max(0.01f, intervalSeconds);

            double startRemaining = GlobalTimer + 1f;
            if (startRemaining <= 0f) return;

            int count = Mathf.FloorToInt((float)startRemaining / intervalSeconds);
            for (int i = 1; i <= count; i++)
            {
                float atRemaining = Mathf.Max((float)startRemaining - i * intervalSeconds, 0f);
                _timeTriggers.Add(new TimerTrigger(atRemaining, callback, null, groupId, triggerWhenSkip, TriggerSpace.Remaining));
            }
        } 
        
        // ENDLESS/COUNTUP: อิง "Elapsed" และใช้ lazy re-schedule (ไม่บวมเมม)
        public void ScheduleLoopingFromNow_Endless(string groupId, float intervalSeconds, Action callback, bool triggerWhenSkip = true)
        {
            CancelGroup(groupId);
            intervalSeconds = Mathf.Max(0.01f, intervalSeconds);

            float startElapsed = Elapsed;
            void AddNext(float atElapsed)
            {
                _timeTriggers.Add(new TimerTrigger(atElapsed, () =>
                {
                    callback?.Invoke();
                    AddNext(atElapsed + intervalSeconds);
                }, null, groupId, triggerWhenSkip, TriggerSpace.Elapsed));
            }
            AddNext(startElapsed + intervalSeconds);
        }

        public void ScheduleLoopingFromNow(string groupId, bool endless, float intervalSeconds, Action callback, bool triggerWhenSkip = true)
        {
            if (endless)
                ScheduleLoopingFromNow_Endless(groupId, intervalSeconds, callback, triggerWhenSkip);
            else
                ScheduleLoopingFromNow_Countdown(groupId, intervalSeconds, callback, triggerWhenSkip);
        }


        #endregion
    }
}