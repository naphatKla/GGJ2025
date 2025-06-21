using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GameControl
{
    public class Timer : MMSingleton<Timer>
    {
        #region Inspector & Fleid

        [SerializeField] private TMP_Text timerText;
        [SerializeField] private float startTimer;

        private bool _isPaused;
        private bool _isRunning;
        private Coroutine _countdownCoroutine;

        #endregion
        
        [ShowInInspector, ReadOnly]
        public float GlobalTimer { get; private set; }
        public float GlobalTimerDown => startTimer - GlobalTimer;
        public float StartTimerNumber => startTimer;
        
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
        
        private void TimerEnd()
        {
            _isRunning = false;
            StopCoroutine(_countdownCoroutine);
        }
    }
}