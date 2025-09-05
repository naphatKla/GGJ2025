using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ProjectExtensions;
using Sirenix.OdinInspector;
using UI.ConfirmButton;
using UI.Transition;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        MainMenu = 11
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
    
    [Serializable]
    public class ConfirmPanelEntry
    {
        [FoldoutGroup("$confirmID")]
        public string confirmID;
        [FoldoutGroup("$confirmID")][Title("Confirm Button")]
        public ConfirmButtonViewholder confirmButtonUI;
        
        [FoldoutGroup("$confirmID")][Tooltip("Open this panel will make game TimeScale=0")]
        public bool pauseGameWhileOpen = false;
        [FoldoutGroup("$confirmID")][Tooltip("Block all input behide this panel")]
        public bool blockInputBehind;
        
        [Title("Transition")] 
        [FoldoutGroup("$confirmID")] public TransitionBase appearTransition;
        [FoldoutGroup("$confirmID")] public TransitionBase disappearTransition;
    }

    public class UIManager : NonAutoCreateSingleton<UIManager>
    {
        [Header("Scene Names")] [SerializeField]
        private string menuScene;

        [SerializeField] private string gamePlayScene;
        [SerializeField] private string endCreditsScene;
      
        [Header("UI Panels (registry)")] [SerializeField]
        private List<UIPanelEntry> panelEntries = new();
        
        [Header("Confirm UI Panels (registry)")] [SerializeField]
        private List<ConfirmPanelEntry> uiEntries = new();
        
        // === Blocker ===
        [Space]
        [SerializeField] private GameObject inputBlockerPrefab;
        private GameObject _blocker;
        [Space]
        [Header("Confirm Container")]
        [SerializeField] private Transform confirmContainer;


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
        private readonly Dictionary<string, ConfirmPanelEntry> _confirmMap = new();
        private readonly HashSet<UIPanelType> _pauseOwners = new();
        
        private readonly Dictionary<string, ConfirmButtonViewholder> _confirmInstances = new();
        
        private ConfirmPanelEntry _activeConfirm;
        private CancellationTokenSource _confirmCts;
        private bool _isPauseApplied;
        private bool _isTransitioning;
        private bool _allLoad;

        // === Shortcuts ===
        public bool AllLoad => _allLoad;
        private bool HasOpenPanels => _stack.Count > 0;
        private UIPanelType TopType => _stack.Count > 0 ? _stack.Peek() : UIPanelType.None;

            #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();
            BuildPanelRegistryAndHideAll();
            BuildConfirmRegistryAndHideAll();
            _pauseOwners.Clear();
            _isPauseApplied = false;
            ApplyPauseState();
            
            _allLoad = false;
        }
        
        private async void Start()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (!_allLoad) _allLoad = true;
        }
        

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePausePanelByEsc();
        }
        
        private void OnEnable()
        {
            //SceneManager.sceneLoaded += OnSceneChange;
        }

        protected override void OnDestroy()
        {
            _confirmCts?.Cancel();
            _confirmCts?.Dispose();
            _confirmCts = null;

            _activeConfirm = null;
            _confirmInstances.Clear();

            _pauseOwners.Clear();
            _isPauseApplied = false;
            _allLoad = true;
            
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1f, 0, false, 100f, true);
            base.OnDestroy();
        }

        /*private void OnSceneChange(Scene scene, LoadSceneMode mode)
        {
            _confirmCts?.Cancel();
            _confirmCts?.Dispose();
            _confirmCts = null;

            _activeConfirm = null;
            _confirmInstances.Clear();

            _pauseOwners.Clear();
            _isPauseApplied = false;
            _allLoad = true;
            Debug.Log("Scene Reset");
            //MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1, 0, false, 0f, false);
            SceneManager.sceneLoaded -= OnSceneChange;
        }*/
        
        private UniTask WaitSceneReadyAsync()
        {
            return _allLoad 
                ? UniTask.CompletedTask 
                : UniTask.WaitUntil(() => _allLoad, cancellationToken: destroyCancellationToken);
        }

        #endregion

        #region Public API

        public async UniTaskVoid OpenPanel(UIPanelType type)
        {
            if (_isTransitioning) return;
            if (!TryGetPanel(type, out _)) return;
            
            if (TopType == type)
            {
                await ClosePanelAsync();
                return;
            }
            
            bool prePaused = TryPreApplyPause(type);
            
            _isTransitioning = true;
            try
            {
                await WaitSceneReadyAsync();
                
                var before = _stack.Count;
                var mode = GetStackTypeFor(type);

                switch (mode)
                {
                    case StackType.PushStack: await Open_PushStackAsync(type); break;
                    case StackType.ShowOnlyPushStack: await Open_PushNonActiveStackAsync(type); break;
                    case StackType.CloseAllAndPush: await Open_CloseAllAndPushAsync(type); break;
                }

                var isFirst = before == 0 && _stack.Count > 0;
                if (isFirst) OnAnyUIOpenFirst?.Invoke();

                OnAnyPanelOpen?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UIManager] OpenPanel {type} was cancelled");
            }
            finally
            {
                _isTransitioning = false;
                if (prePaused && !_stack.Contains(type) && !IsPanelOpen(type))
                {
                    _pauseOwners.Remove(type);
                }
                ApplyPauseState();
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
                    await ShowPanel(TopType, false);
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
                ApplyPauseState();
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
                ApplyPauseState();
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

        public async void ClosePanel()
        {
            await ClosePanelAsync();
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
                MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0f, 0, false, 1f, true);
                _isPauseApplied = true;
                return;
            }

            if (shouldPause || !_isPauseApplied) return;
            
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1f, 0, false, 100f, true);
            _isPauseApplied = false;
        }
        
        private bool TryPreApplyPause(UIPanelType type)
        {
            if (_pauseFlagMap.TryGetValue(type, out var wantsPause) && wantsPause)
            {
                _pauseOwners.Add(type);
                ApplyPauseState();
                return true;
            }
            return false;
        }
        
        public void TogglePausePanelByEsc()
        {
            if (TopType == UIPanelType.Pause)
            {
                ClosePanelAsync().Forget();
                return;
            }

            OpenPanel(UIPanelType.Pause).Forget();
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

        public void OpenSaveGamePanel() => OpenPanel(UIPanelType.SaveGame).Forget();
        public void OpenMapSelectPanel() => OpenPanel(UIPanelType.MapSelect).Forget();
        public void OpenGameModePanel() => OpenPanel(UIPanelType.GameMode).Forget();
        public void OpenQuitPanel() => OpenPanel(UIPanelType.QuitPanel).Forget();
        public void OpenTutorialPanel() => OpenPanel(UIPanelType.TutorialPanel).Forget();
        public void OpenResultMenu() => OpenPanel(UIPanelType.MapResult).Forget();
        public void OpenSettingsPanel() => OpenPanel(UIPanelType.Setting);
        
        #endregion

        #region Internals

        private void BuildPanelRegistryAndHideAll()
        {
            _panelMap.Clear();
            _stackTypeMap.Clear();
            _pauseFlagMap.Clear();
            _blockFlagMap.Clear();
            _pauseOwners.Clear();

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
        
        private void BuildConfirmRegistryAndHideAll()
        {
            _confirmMap.Clear();
            foreach (var e in uiEntries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.confirmID) || e.confirmButtonUI == null) continue;
                if (!_confirmMap.ContainsKey(e.confirmID))
                    _confirmMap.Add(e.confirmID, e);
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
        
        private async UniTask ShowPanel(UIPanelType type, bool playTransition = true)
        {
            if (type == UIPanelType.None) return;
            if (!_panelMap.TryGetValue(type, out var go) || go == null)return;
   
            var entry = GetType(type);
            if (entry == null)return;

            if (!go.activeSelf) go.SetActive(true);
            if (_pauseFlagMap.TryGetValue(type, out var p) && p)
                _pauseOwners.Add(type);

            if (playTransition && entry.appearTransition != null)
            {
                await entry.appearTransition.PlayAsync(go, true, destroyCancellationToken);
            }
        }

        private async UniTask HidePanel(UIPanelType type, bool playTransition = true)
        {
            if (type == UIPanelType.None) return;
            if (!_panelMap.TryGetValue(type, out var go) || go == null) return;

            var entry = GetType(type);
            if (entry == null)return;

            DOTween.Kill(go, complete: false);
            _pauseOwners.Remove(type);
            
            if (playTransition && entry.disappearTransition != null)
            {
                await entry.disappearTransition.PlayAsync(go, false, destroyCancellationToken);
            }

            if (go != null && go.activeSelf) go.SetActive(false);
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
            try
            {
                while (HasOpenPanels)
                {
                    var t = _stack.Pop();
                    await HidePanel(t, playTransition: true);
                }

                _pauseOwners.Clear();
                if (invokeClosedEvent) OnAllPanelClosed?.Invoke();
                ApplyPauseState();
                RefreshTopAsync().Forget();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UIManager] ClearStackAsync was cancelled");
            }
            finally
            {
                _pauseOwners.Clear();
                _isTransitioning = false;
                ApplyPauseState();
                RefreshTopAsync().Forget();
                if (MMTimeManager.Instance != null) 
                    MMTimeScaleEvent.Trigger(MMTimeScaleMethods.Reset, 1f, 0, false, 100f, true);
                Time.timeScale = 1;
            }
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

        #region Confirm Button

        private Transform GetConfirmParent()
        {
            if (confirmContainer) return confirmContainer;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas) return canvas.transform;
            var anyCanvas = FindFirstObjectByType<Canvas>();
            return anyCanvas ? anyCanvas.transform : transform;
        }
        
        private ConfirmButtonViewholder GetOrCreateConfirmInstance(ConfirmPanelEntry e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.confirmID) || !e.confirmButtonUI) return null;

            if (_confirmInstances.TryGetValue(e.confirmID, out var inst) && inst)
                return inst;

            var parent = GetConfirmParent();
            var newInst = Instantiate(e.confirmButtonUI, parent);
            newInst.gameObject.SetActive(false);
            _confirmInstances[e.confirmID] = newInst;
            return newInst;
        }
        
        private void ForceCloseActiveConfirmInstant()
        {
            _confirmCts?.Cancel();
            _confirmCts?.Dispose();
            _confirmCts = null;

            if (_activeConfirm != null)
            {
                if (_confirmInstances.TryGetValue(_activeConfirm.confirmID, out var inst) && inst)
                {
                    var go = inst.gameObject;
                    if (go)
                    {
                        DOTween.Kill(go, complete: false);
                        go.SetActive(false);
                    }
                }

                if (!HasOpenPanels && _blocker) _blocker.SetActive(false);
                _pauseOwners.Remove(UIPanelType.None);
                ApplyPauseState();
                _activeConfirm = null;
            }
        }
        
        private static void SafeClearAndAdd(Button btn, UnityAction onClick)
        {
            if (!btn) return;
#if UNITY_EDITOR
            btn.onClick.RemoveAllListeners();
#else
    btn.onClick.RemoveAllListeners();
#endif
            if (onClick != null) btn.onClick.AddListener(onClick);
        }

        private async UniTask ShowConfirmEntryAsync(ConfirmPanelEntry e, bool playTransition = true)
        {
            var vh = GetOrCreateConfirmInstance(e);
            if (!vh) return;
            var go = vh.gameObject;
            if (!go) return;

            if (_blocker)
            {
                var bt = _blocker.transform;
                var pt = go.transform;
                if (bt.parent != pt.parent) bt.SetParent(pt.parent, false);
                _blocker.SetActive(e.blockInputBehind);
                if (e.blockInputBehind) _blocker.transform.SetAsLastSibling();
            }

            if (e.pauseGameWhileOpen) _pauseOwners.Add(UIPanelType.None);
            ApplyPauseState();

            if (!go.activeSelf) go.SetActive(true);
            go.transform.SetAsLastSibling();

            if (playTransition && e.appearTransition != null)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    destroyCancellationToken, go.GetCancellationTokenOnDestroy());
                try
                {
                    await e.appearTransition.PlayAsync(go, true, linked.Token);
                }
                catch (OperationCanceledException) { }
            }
        }

        private async UniTask HideConfirmEntryAsync(ConfirmPanelEntry e, bool playTransition = true)
        {
            _confirmInstances.TryGetValue(e.confirmID, out var vh);
            if (!vh) return;
            var go = vh.gameObject;
            if (!go) return;
            try
            {
                DOTween.Kill(go);
                if (playTransition && e.disappearTransition != null)
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                        destroyCancellationToken, go.GetCancellationTokenOnDestroy());
                    try
                    {
                        await e.disappearTransition.PlayAsync(go, false, linked.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                if (go) go.SetActive(false);
            }
            finally
            {
                if (!HasOpenPanels && _blocker) _blocker.SetActive(false);
                _pauseOwners.Remove(UIPanelType.None);
                ApplyPauseState();
            }
        }

        private async UniTask CloseActiveConfirm()
        {
            _confirmCts?.Cancel();
            _confirmCts?.Dispose();
            _confirmCts = null;

            if (_activeConfirm != null)
            {
                try { await HideConfirmEntryAsync(_activeConfirm, playTransition: true); }
                catch (OperationCanceledException) { }
                _activeConfirm = null;
            }
        }

        /// <summary>
        ///  Confirm id:
        ///  onYes/onNo can be null
        ///  durationSec <= 0 no auto-cancel
        ///  close current confirm if it showing
        /// </summary>
        public async UniTaskVoid ShowConfirmButton(string confirmId, Action onYes, Action onNo, float durationSec = 0f)
        {
            if (string.IsNullOrWhiteSpace(confirmId) || !_confirmMap.TryGetValue(confirmId, out var entry)) return;

            _confirmCts?.Cancel();
            _confirmCts?.Dispose();
            _confirmCts = null;

            if (_activeConfirm != null)
            {
                ForceCloseActiveConfirmInstant();
            }

            var vh = GetOrCreateConfirmInstance(entry);
            if (!vh || !vh.yesButton || !vh.noButton) return;

            _activeConfirm = entry;
            _confirmCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = _confirmCts.Token;

            SafeClearAndAdd(vh.yesButton, () =>
            {
                if (token.IsCancellationRequested) return;
                ForceCloseActiveConfirmInstant();
                onYes?.Invoke();
            });

            SafeClearAndAdd(vh.noButton, () =>
            {
                if (token.IsCancellationRequested) return;
                ForceCloseActiveConfirmInstant();
                onNo?.Invoke();
            });

            try
            {
                await ShowConfirmEntryAsync(entry, true);

                if (durationSec > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(durationSec), cancellationToken: token);
                    if (!token.IsCancellationRequested)
                    {
                        ForceCloseActiveConfirmInstant();
                        onNo?.Invoke();
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
        
        #endregion
    }
}