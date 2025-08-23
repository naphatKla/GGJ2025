using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.Controller;
using GameControl.GameState;
using MoreMountains.Feedbacks;
using ProjectExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public enum UIPanelType
    {
        None = 0,
        Pause = 1,
        MapResult = 2,
        SkillTree = 3,
        SolfUpgrade = 4,
        Setting = 5,
        SaveGame = 6,
        GameMode = 7,
        MapSelect = 8,
        QuitPanel = 9,
        TutorialPanel = 10,
        MainMenu = 11,
    }

    [Serializable]
    public class UIPanelEntry
    {
        public UIPanelType type;
        public GameObject panel;
    }

    public class UIManager : NonAutoCreateSingleton<UIManager>
    {
        [Header("Scene Names")] [SerializeField]
        private string menuScene;

        [SerializeField] private string gamePlayScene;
        [SerializeField] private string endCreditsScene;

        [Header("UI Panels (registry)")] [SerializeField]
        private List<UIPanelEntry> panelEntries = new();

        [Header("Panels that PAUSE the game when open")]
        private List<UIPanelType> _pauseOnOpenPanels = new()
        {
            UIPanelType.Pause,
            UIPanelType.SolfUpgrade,
            UIPanelType.TutorialPanel
        };

        // === Events ===
        public event Action OnAnyPanelOpen; // call every time on any Panel open.
        public event Action OnAnyUIOpenFirst; // call one time when the first panel open.
        public event Action OnAllPanelClosed; // call when all of the panel was closed.

        // === State ===
        private readonly Dictionary<UIPanelType, GameObject> _panelMap = new();
        private readonly Stack<UIPanelType> _stack = new();
        private readonly HashSet<UIPanelType> _pauseOwners = new();
        private bool _isPauseApplied;

        // === Shortcuts ===
        private bool HasOpenPanels => _stack.Count > 0;
        private UIPanelType TopType => _stack.Count > 0 ? _stack.Peek() : UIPanelType.None;

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();
            BuildPanelMapAndHideAll();
            _pauseOwners.Clear();
            _isPauseApplied = false;
            ApplyPauseState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePausePanelByEsc();
        }

        #endregion

        #region Public API

        public void OpenPanel(UIPanelType type)
        {
            if (!TryGetPanel(type, out var panel)) return;

            bool wasEmptyBefore = !HasOpenPanels;

            if (TopType == type)
            {
                ClosePanel();
                return;
            }

            // Hide previous top (keep in stack)
            SetActiveIfFound(TopType, false);

            // Show new and push
            panel.SetActive(true);
            _stack.Push(type);

            // Mark pause owner if listed
            if (_pauseOnOpenPanels.Contains(type))
                _pauseOwners.Add(type);

            // Fire events
            if (wasEmptyBefore) OnAnyUIOpenFirst?.Invoke();
            OnAnyPanelOpen?.Invoke();

            ApplyPauseState();
        }

        // close the top most panel
        public void ClosePanel()
        {
            if (!HasOpenPanels) return;

            var closing = _stack.Pop();
            SetActiveIfFound(closing, false);
            _pauseOwners.Remove(closing);

            if (HasOpenPanels)
            {
                // Reveal previous
                SetActiveIfFound(TopType, true);
            }
            else
            {
                OnAllPanelClosed?.Invoke();
            }

            ApplyPauseState();
        }


        // close th specific panel in stack.
        public void CloseSpecificPanel(UIPanelType type)
        {
            if (!HasOpenPanels || !_panelMap.ContainsKey(type) || !_stack.Contains(type))
                return;

            if (TopType == type)
            {
                ClosePanel();
                return;
            }

            // Remove from middle
            var buffer = new Stack<UIPanelType>();
            while (HasOpenPanels)
            {
                var cur = _stack.Pop();
                if (cur == type)
                {
                    SetActiveIfFound(cur, false);
                    _pauseOwners.Remove(cur);
                    break;
                }

                buffer.Push(cur);
            }

            while (buffer.Count > 0) _stack.Push(buffer.Pop());

            if (!HasOpenPanels)
                OnAllPanelClosed?.Invoke();

            ApplyPauseState();
        }

        public void CloseAllPanels()
        {
            while (HasOpenPanels)
                SetActiveIfFound(_stack.Pop(), false);

            _pauseOwners.Clear();
            OnAllPanelClosed?.Invoke();
            ApplyPauseState();
        }

        public bool IsPanelOpen(UIPanelType type)
        {
            return _panelMap.TryGetValue(type, out var go) && go.activeSelf;
        }

        #endregion

        #region Pause handling

        private void ApplyPauseState()
        {
            bool shouldPause = _pauseOwners.Count > 0;

            if (shouldPause && !_isPauseApplied)
            {
                MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0, -1, true, 10f, true);
                _isPauseApplied = true;
                return;
            }

            if (shouldPause || !_isPauseApplied) return;

            // อย่า resume ถ้าอยู่ใน SummaryState
            if (GameStateController.Instance.CurrentState is not SummaryState)
            {
                MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, -1, false, 0f, false);
                Time.timeScale = 1f;
            }

            _isPauseApplied = false;
        }

        public void TogglePausePanelByEsc()
        {
            if (TopType == UIPanelType.Pause)
            {
                ClosePanel();
                return;
            }

            OpenPanel(UIPanelType.Pause);
        }

        #endregion

        #region Scene helpers

        public async void BackMenu()
        {
            CloseAllPanels();
            await SceneManager.LoadSceneAsync(menuScene).ToUniTask();
            await UniTask.Yield();
        }

        public void LoadToGamePlayScene()
        {
            CloseAllPanels();
            SceneManager.LoadScene(gamePlayScene);
        }

        public void LoadToCreditsScene()
        {
            CloseAllPanels();
            SceneManager.LoadScene(endCreditsScene);
        }

        public void QuitGame()
        {
            Application.Quit();
            Debug.Log("Quit Game");
        }

        #endregion

        #region Preset open helpers

        public void OpenSaveGamePanel() => OpenPanel(UIPanelType.SaveGame);
        public void OpenMapSelectPanel() => OpenPanel(UIPanelType.MapSelect);
        public void OpenGameModePanel() => OpenPanel(UIPanelType.GameMode);
        public void OpenQuitPanel() => OpenPanel(UIPanelType.QuitPanel);

        public void OpenTutorialPanel()
        {
            CloseAllPanels();
            OpenPanel(UIPanelType.TutorialPanel);
        }

        public void OpenResultMenu()
        {
            CloseAllPanels();

            if (!TryGetPanel(UIPanelType.MapResult, out var panel)) return;

            panel.SetActive(true);
            _stack.Push(UIPanelType.MapResult);

            var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            DOTween.Kill(panel, complete: true);
            DOTween.Sequence()
                .Append(cg.DOFade(1f, 0.15f))
                .OnComplete(() => cg.alpha = 1f)
                .SetTarget(panel);

            OnAnyUIOpenFirst?.Invoke();
            OnAnyPanelOpen?.Invoke();

            if (_pauseOnOpenPanels.Contains(UIPanelType.MapResult))
                _pauseOwners.Add(UIPanelType.MapResult);

            ApplyPauseState();
        }

        #endregion

        #region Internals

        private void BuildPanelMapAndHideAll()
        {
            _panelMap.Clear();
            foreach (var e in panelEntries)
            {
                if (e == null || e.panel == null) continue;
                if (_panelMap.ContainsKey(e.type)) continue;

                _panelMap.Add(e.type, e.panel);
                e.panel.SetActive(false);
            }
        }

        private bool TryGetPanel(UIPanelType type, out GameObject go)
        {
            if (!_panelMap.TryGetValue(type, out go) || go == null)
            {
                Debug.LogWarning($"[UIManager] Panel not registered or null: {type}");
                return false;
            }

            return true;
        }

        private void SetActiveIfFound(UIPanelType type, bool active)
        {
            if (type == UIPanelType.None) return;
            if (_panelMap.TryGetValue(type, out var go) && go != null)
                go.SetActive(active);
        }

        #endregion
    }
}