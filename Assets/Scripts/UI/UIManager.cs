using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ProjectExtensions;
using Sirenix.OdinInspector;
using UI.Transition;
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
    
    public enum StackType
    {
        /// <summary>ซ่อนเฉพาะตัวบนสุดเดิม แล้วเปิดตัวใหม่; ถ้าตัวใหม่นี้อยู่ในกองอยู่แล้ว จะปิดตัวที่อยู่เหนือมันจนมันขึ้นมาอยู่บนสุด</summary>
        PushStack,

        /// <summary>ซ่อนทุกตัวในกอง (ไม่ลบกอง) แล้วค่อย push ตัวใหม่; ปิดตัวใหม่เมื่อไหร่ ตัวก่อนหน้าบนสุดจะกลับมาโชว์</summary>
        ShowOnlyPushStack,

        /// <summary>ซ่อนและล้างกองทั้งหมดแบบเงียบๆ จากนั้น push ตัวเอง (ปิดแล้วจะไม่มีตัวก่อนหน้าให้ย้อน)</summary>
        CloseAllAndPush
    }

    [Serializable]
    public class UIPanelEntry
    {
        [FoldoutGroup("$type")]
        public UIPanelType type;
        [FoldoutGroup("$type")]
        public StackType stackType = StackType.PushStack;
        
        [FoldoutGroup("$type")][Tooltip("Open this panel will make game TimeScale=0")]
        public bool pauseGameWhileOpen = false;
        [FoldoutGroup("$type")][Tooltip("Block all input behide this panel")]
        public bool blockInputBehind;
        [FoldoutGroup("$type")]
        public GameObject panel;

        [Title("Transition")] 
        [FoldoutGroup("$type")] public TransitionBase appearTransition;
        [FoldoutGroup("$type")] public TransitionBase disappearTransition;
    }

    public class UIManager : NonAutoCreateSingleton<UIManager>
    {
        [Header("Scene Names")] [SerializeField]
        private string menuScene;

        [SerializeField] private string gamePlayScene;
        [SerializeField] private string endCreditsScene;
      
        [Header("UI Panels (registry)")] [SerializeField]
        private List<UIPanelEntry> panelEntries = new();
        
        // === Blocker ===
        [Space]
        [SerializeField] private GameObject inputBlockerPrefab;
        private GameObject _blocker;

        // === Events ===
        public event Action OnAnyPanelOpen; // call every time on any Panel open.
        public event Action OnAnyUIOpenFirst; // call one time when the first panel open.
        public event Action OnAllPanelClosed; // call when all of the panel was closed.

        // === State ===
        private readonly Dictionary<UIPanelType, bool> _pauseFlagMap = new();
        private readonly Dictionary<UIPanelType, bool> _blockFlagMap = new();
        private readonly Dictionary<UIPanelType, GameObject> _panelMap = new();
        private readonly Dictionary<UIPanelType, StackType> _stackTypeMap = new();
        private readonly Stack<UIPanelType> _stack = new();
        
        private readonly HashSet<UIPanelType> _pauseOwners = new();
        private bool _isPauseApplied;
        private bool _isTransitioning;

        // === Shortcuts ===
        private bool HasOpenPanels => _stack.Count > 0;
        private UIPanelType TopType => _stack.Count > 0 ? _stack.Peek() : UIPanelType.None;

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();
            BuildPanelRegistryAndHideAll();
            _pauseOwners.Clear();
            _isPauseApplied = false;
            ApplyPauseState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePausePanelByEsc();
        }

        protected override void OnDestroy()
        {
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, -1, false, 0f, false);
            base.OnDestroy();
        }

        #endregion

        #region Public API

        public async void OpenPanel(UIPanelType type)
        {
            if (_isTransitioning) return;
            if (!TryGetPanel(type, out _)) return;

            _isTransitioning = true;
            try
            {
                var before = _stack.Count;
                var mode = GetStackTypeFor(type);

                if (TopType == type)
                {
                    await ClosePanelAsync();
                    return;
                }

                switch (mode)
                {
                    case StackType.PushStack: await Open_PushStackAsync(type); break;
                    case StackType.ShowOnlyPushStack: await Open_PushNonActiveStackAsync(type); break;
                    case StackType.CloseAllAndPush: await Open_CloseAllAndPushAsync(type); break;
                }

                var isFirst = before == 0 && _stack.Count > 0;
                if (isFirst) OnAnyUIOpenFirst?.Invoke();

                OnAnyPanelOpen?.Invoke();
                ApplyPauseState();
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UIManager] OpenPanel {type} was cancelled");
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public async UniTask ClosePanelAsync()
        {
            if (!HasOpenPanels || _isTransitioning) return;
            _isTransitioning = true;
            try
            {
                var closing = _stack.Pop();
                await HidePanel(closing);

                if (HasOpenPanels)
                {
                    await ShowPanel(TopType);
                    RestoreOverlayChainFromTop();
                }
                else
                {
                    OnAllPanelClosed?.Invoke();
                }

                ApplyPauseState();
                RefreshTopAsync().Forget();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UIManager] ClosePanelAsync was cancelled");
            }
            finally
            {
                _isTransitioning = false;
            }
        }


        // close th specific panel in stack.
        public async UniTask CloseSpecificPanel(UIPanelType type)
        {
            if (!HasOpenPanels || _isTransitioning) return;
            
            _isTransitioning = true;
            try
            {
                if (TopType == type)
                {
                    _isTransitioning = false;
                    await ClosePanelAsync();
                    return;
                }
                
                RemoveFromStack(type);
                await HidePanel(type);

                if (!HasOpenPanels) OnAllPanelClosed?.Invoke();
                ApplyPauseState();
                RefreshTopAsync().Forget();
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UIManager] CloseSpecificPanel {type} was cancelled");
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public async void CloseAllPanels()
        {
            try
            {
                await ClearStackAsync(invokeClosedEvent: true);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UIManager] CloseAllPanels was cancelled");
            }
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
            
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, -1, false, 0f, false);
            Time.timeScale = 1f;
            
            _isPauseApplied = false;
        }

        public void TogglePausePanelByEsc()
        {
            if (TopType == UIPanelType.Pause)
            {
                ClosePanelAsync().Forget();
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
        public void OpenTutorialPanel() => OpenPanel(UIPanelType.TutorialPanel);
        public void OpenResultMenu() => OpenPanel(UIPanelType.MapResult);
        
        #endregion

        #region Internals

        private void BuildPanelRegistryAndHideAll()
        {
            _panelMap.Clear();
            _stackTypeMap.Clear();
            _pauseFlagMap.Clear();
            _blockFlagMap.Clear();

            foreach (var e in panelEntries)
            {
                if (e == null || e.panel == null) continue;
                if (_panelMap.ContainsKey(e.type)) continue;
                _panelMap.Add(e.type, e.panel);
                _stackTypeMap[e.type] = e.stackType;
                _pauseFlagMap[e.type] = e.pauseGameWhileOpen;
                _blockFlagMap[e.type] = e.blockInputBehind;
                e.panel.SetActive(false);
            }
            
            if (!_blocker && inputBlockerPrefab)
            {
                _blocker = Instantiate(inputBlockerPrefab, transform);
                _blocker.SetActive(false);
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
        
        private StackType GetStackTypeFor(UIPanelType type)
        {
            return _stackTypeMap.TryGetValue(type, out var st) ? st : StackType.PushStack;
        }
        
        private UIPanelEntry GetType(UIPanelType type)
        {
            var entry = panelEntries.Find(e => e.type == type);
            if (entry == null) return null;
            return entry;
        }
        
        private async UniTask ShowPanel(UIPanelType type)
        {
            if (type == UIPanelType.None) return;
            if (!_panelMap.TryGetValue(type, out var go) || go == null)return;
   
            var entry = GetType(type);
            if (entry == null)return;

            if (!go.activeSelf) go.SetActive(true);
            if (_pauseFlagMap.TryGetValue(type, out var p) && p)
                _pauseOwners.Add(type);

            if (entry.appearTransition != null)
            {
                await entry.appearTransition.PlayAsync(go, true, destroyCancellationToken);
            }
        }

        private async UniTask HidePanel(UIPanelType type)
        {
            if (type == UIPanelType.None) return;
            if (!_panelMap.TryGetValue(type, out var go) || go == null) return;

            var entry = GetType(type);
            if (entry == null)return;

            DOTween.Kill(go, complete: false);
            _pauseOwners.Remove(type);
            
            if (entry.disappearTransition != null)
            {
                await entry.disappearTransition.PlayAsync(go, false, destroyCancellationToken);
            }

            if (go.activeSelf) go.SetActive(false);
        }

        private void RestoreOverlayChainFromTop()
        {
            if (!HasOpenPanels) return;
            var arr = _stack.ToArray();
            for (int i = 1; i < arr.Length; i++)
            {
                var t = arr[i];
                if (_stackTypeMap.TryGetValue(t, out var st) && st == StackType.PushStack) ShowPanel(t).Forget();
                else break;
            }
        }
        
        private async UniTask ClearStackAsync(bool invokeClosedEvent)
        {
            if (_isTransitioning) return;
      
            _isTransitioning = true;
            while (HasOpenPanels)
            {
                var t = _stack.Pop();
                await HidePanel(t);
            }

            _pauseOwners.Clear();
            if (invokeClosedEvent) OnAllPanelClosed?.Invoke();
            ApplyPauseState();
            RefreshTopAsync().Forget();
            _isTransitioning = false;
        }
 
        private bool RemoveFromStack(UIPanelType type)
        {
            if (!_stack.Contains(type)) return false;

            var buffer = new Stack<UIPanelType>();
            var removed = false;

            while (_stack.Count > 0)
            {
                var cur = _stack.Pop();
                if (cur == type)
                {
                    removed = true;
                    break;
                }

                buffer.Push(cur);
            }

            while (buffer.Count > 0) _stack.Push(buffer.Pop());
            return removed;
        }

        #endregion

        #region Blocker Helper
        
        /// <summary>
        /// Refresh top panel
        /// </summary>
        private void RefreshTop()
        {
            if (!HasOpenPanels)
            {
                if (_blocker) _blocker.SetActive(false);
                return;
            }

            if (!_panelMap.TryGetValue(TopType, out var go) || go == null)
            {
                if (_blocker) _blocker.SetActive(false);
                return;
            }

            ShowBlockerUnderPanel(go);
            go.transform.SetAsLastSibling();
        }

        private void ShowBlockerUnderPanel(GameObject go)
        {
            var needBlock = _blockFlagMap.TryGetValue(TopType, out var b) && b && go.activeInHierarchy;

            if (needBlock && _blocker)
            {
                var bt = _blocker.transform;
                var pt = go.transform;
                if (bt.parent != pt.parent)
                    bt.SetParent(pt.parent, false);

                _blocker.SetActive(true);
                bt.SetAsLastSibling();
            }
            else if (_blocker)
            {
                _blocker.SetActive(false);
            }
        }

        private async UniTaskVoid RefreshTopAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, destroyCancellationToken);
            RefreshTop();
        }

        #endregion
        
        #region Push Type
        private async UniTask Open_PushStackAsync(UIPanelType type)
        {
            try
            {
                if (_stack.Contains(type) && TopType != type)
                {
                    while (HasOpenPanels && TopType != type)
                    {
                        var above = _stack.Pop();
                        await HidePanel(above);
                    }
                    RefreshTopAsync().Forget();
                    await ShowPanel(type);
                    return;
                }
                _stack.Push(type);
                RefreshTopAsync().Forget();
                await ShowPanel(type);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UIManager] Open_PushStackAsync {type} was cancelled");
            }
        }
        
        private async UniTask Open_PushNonActiveStackAsync(UIPanelType type)
        {
            try
            {
                foreach (var t in _stack.ToArray()) await HidePanel(t);
                RemoveFromStack(type);
                _stack.Push(type);
                RefreshTopAsync().Forget();
                await ShowPanel(type);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UIManager] Open_PushNonActiveStackAsync {type} was cancelled");
            }
        }
        
        private async UniTask Open_CloseAllAndPushAsync(UIPanelType type)
        {
            try
            {
                await ClearStackAsync(invokeClosedEvent: false);
                _stack.Push(type);
                RefreshTopAsync().Forget();
                await ShowPanel(type);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UIManager] Open_CloseAllAndPushAsync {type} was cancelled");
            }
        }

        #endregion
    }
}