using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using ProjectExtensions;
using Sirenix.OdinInspector;
using UI.Transition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public enum TimeScaleType
    {
        Scaled = 0,
        Unscaled = 1
    }

    [Serializable]
    public class PopupEntry
    {
        [FoldoutGroup("$popupId")] public string popupId;

        [FoldoutGroup("$popupId")] [Title("Prefab")]
        public GameObject prefab;

        [FoldoutGroup("$popupId")] [Tooltip("ถ้า <= 0 จะไม่ auto-hide จนกว่าจะเรียก HidePopup เอง")]
        public float defaultDuration = 2f;

        [Title("Transition")] [FoldoutGroup("$popupId")]
        public TransitionBase appearTransition;

        [FoldoutGroup("$popupId")] public TransitionBase disappearTransition;

        [FoldoutGroup("$popupId")] [Tooltip("ถ้ากำลังแสดงอยู่ แล้วถูกเรียกซ้ำให้รีสตาร์ทเวลา")]
        public bool restartIfAlreadyVisible = true;

        [FoldoutGroup("$popupId")] [Tooltip("ถ้าเปิดไว้ Popup นี้จะแสดงซ้อนกับตัวอื่นได้ทันที (ไม่เข้าคิว)")]
        public bool allowOverlayStack;

        [FoldoutGroup("$popupId")]
        [Tooltip("ชนิดเวลาที่ใช้กับตัวนับ auto-hide: Scaled จะอิง Time.timeScale, Unscaled จะไม่อิง")]
        public TimeScaleType timeScale = TimeScaleType.Scaled;
    }

    public class PopupUIManager : NonAutoCreateSingleton<PopupUIManager>
    {
        [Header("Registry")] [SerializeField] private List<PopupEntry> popupEntries = new();

        [Header("Container (optional)")] [SerializeField]
        private Transform popupContainer;

        // Events
        public event Action<string> OnPopupShown;
        public event Action<string> OnPopupHidden;

        // Maps / State
        private readonly Dictionary<string, PopupEntry> _entryMap = new();
        private readonly Dictionary<string, GameObject> _instances = new();
        private readonly Dictionary<string, CancellationTokenSource> _timers = new();
        private CancellationTokenSource _scopeCts;
        private bool _isQuitting;

        // ===== Queue/Stack System =====
        private struct PopupRequest
        {
            public string id;
            public float duration;
            public bool playTransition;
            public Action<GameObject> setup;
        }

        private readonly Queue<PopupRequest> _queue = new();
        private bool _isProcessingQueue;
        private string _exclusiveActiveId;
        public int PendingCount => _queue.Count;
        public bool HasActiveExclusive => !string.IsNullOrEmpty(_exclusiveActiveId);

        protected override void Awake()
        {
            base.Awake();
            _scopeCts = new CancellationTokenSource();
            BuildRegistry();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        protected override void OnDestroy()
        {
            _scopeCts?.Cancel();
            _scopeCts?.Dispose();
            _scopeCts = null;
            ForceClearAllNow();

            base.OnDestroy();
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            _scopeCts?.Cancel();
            _scopeCts?.Dispose();
            _scopeCts = new CancellationTokenSource();
            ForceClearAllNow();
        }

        private void ForceClearAllNow()
        {
            foreach (var kv in _timers)
            {
                kv.Value.Cancel();
                kv.Value.Dispose();
            }

            _timers.Clear();

            foreach (var kv in _instances)
            {
                var go = kv.Value;
                if (!go) continue;
                DOTween.Kill(go, false);
                go.SetActive(false);
            }

            var dead = new List<string>();
            foreach (var kv in _instances)
                if (!kv.Value)
                    dead.Add(kv.Key);
            foreach (var k in dead) _instances.Remove(k);

            _queue.Clear();
            _exclusiveActiveId = null;
            _isProcessingQueue = false;
        }

        private void BuildRegistry()
        {
            _entryMap.Clear();
            foreach (var e in popupEntries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.popupId) || e.prefab == null) continue;
                if (_entryMap.ContainsKey(e.popupId)) continue;
                _entryMap.Add(e.popupId, e);
            }
        }

        private Transform GetContainer()
        {
            if (popupContainer) return popupContainer;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas) return canvas.transform;
            var anyCanvas = FindFirstObjectByType<Canvas>();
            return anyCanvas ? anyCanvas.transform : transform;
        }

        private GameObject GetOrCreateInstance(PopupEntry e)
        {
            if (_instances.TryGetValue(e.popupId, out var inst) && inst) return inst;
            var parent = GetContainer();
            var go = Instantiate(e.prefab, parent);
            go.SetActive(false);
            _instances[e.popupId] = go;
            return go;
        }

        private void KillTimer(string id)
        {
            if (_timers.TryGetValue(id, out var cts) && cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _timers.Remove(id);
        }

        public bool IsPopupVisible(string popupId)
        {
            return _instances.TryGetValue(popupId, out var go) && go && go.activeSelf;
        }

        public void ShowPopup(string popupId, float durationSec = -1f, bool bypassStack = false,
            bool playTransition = true)
        {
            ShowPopupWithSetup(popupId, null, durationSec, bypassStack, playTransition);
        }

        public void ShowPopupWithSetup(string popupId, Action<GameObject> setup, float durationSec = -1f,
            bool bypassStack = false, bool playTransition = true)
        {
            if (string.IsNullOrWhiteSpace(popupId) || !_entryMap.TryGetValue(popupId, out var entry)) return;

            if (bypassStack || entry.allowOverlayStack)
            {
                ShowNowAsync(entry, setup, durationSec, playTransition).Forget();
                return;
            }

            _queue.Enqueue(new PopupRequest
            {
                id = popupId,
                duration = durationSec,
                playTransition = playTransition,
                setup = setup
            });

            if (!_isProcessingQueue)
                ProcessQueueAsync().Forget();
        }

        public void ShowPopup<TView>(string popupId, Action<TView> setup, float durationSec = -1f,
            bool bypassStack = false, bool playTransition = true)
            where TView : Component
        {
            Action<GameObject> wrapper = null;
            if (setup != null)
                wrapper = go =>
                {
                    var view = go ? go.GetComponentInChildren<TView>(true) : null;
                    if (view) setup(view);
                };

            ShowPopupWithSetup(popupId, wrapper, durationSec, bypassStack, playTransition);
        }

        public async UniTask HidePopup(string popupId, bool playTransition = true)
        {
            if (string.IsNullOrWhiteSpace(popupId) || !_entryMap.TryGetValue(popupId, out var entry)) return;
            await HidePopupInternal(entry, playTransition);
        }

        public async UniTask HideAllPopups(bool playTransition = true)
        {
            ClearQueue();

            foreach (var kvp in _entryMap) await HidePopupInternal(kvp.Value, playTransition);
        }

        public void ClearQueue()
        {
            _queue.Clear();
        }

        // ================== Internals ==================

        private async UniTask ShowNowAsync(PopupEntry entry, Action<GameObject> setup, float durationSec,
            bool playTransition)
        {
            var go = GetOrCreateInstance(entry);
            if (!go) return;
            setup?.Invoke(go);

            var alreadyVisible = go.activeSelf;
          
            if (alreadyVisible)
            {
                if (entry.restartIfAlreadyVisible)
                {
                    KillTimer(entry.popupId);
                }
                else
                {
                    go.transform.SetAsLastSibling();
                    return;
                }
            }

            go.transform.SetAsLastSibling();

            if (!go.activeSelf) go.SetActive(true);

            if (playTransition && entry.appearTransition != null)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    destroyCancellationToken, go.GetCancellationTokenOnDestroy());
                try
                {
                    await entry.appearTransition.PlayAsync(go, true, linked.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }

            OnPopupShown?.Invoke(entry.popupId);

            var dur = durationSec > -1f ? durationSec : entry.defaultDuration;
            if (dur > 0f)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    destroyCancellationToken, go.GetCancellationTokenOnDestroy());
                _timers[entry.popupId] = cts;

                AutoHideAsync(entry, dur, cts.Token).Forget();
            }
        }

        private async UniTask AutoHideAsync(PopupEntry entry, float dur, CancellationToken token)
        {
            var delayType = entry.timeScale == TimeScaleType.Unscaled
                ? DelayType.UnscaledDeltaTime
                : DelayType.DeltaTime;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(dur), delayType, cancellationToken: token);
                if (!token.IsCancellationRequested) await HidePopupInternal(entry, true);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                KillTimer(entry.popupId);
            }
        }

        private async UniTask HidePopupInternal(PopupEntry entry, bool playTransition)
        {
            var id = entry.popupId;
            if (!_instances.TryGetValue(id, out var go) || !go || !go.activeSelf) return;

            KillTimer(id);
            DOTween.Kill(go);

            if (playTransition && entry.disappearTransition != null)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, go.GetCancellationTokenOnDestroy());
                try
                {
                    await entry.disappearTransition.PlayAsync(go, false, linked.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (go) go.SetActive(false);
            OnPopupHidden?.Invoke(id);
        }

        private async UniTask ProcessQueueAsync()
        {
            if (_isProcessingQueue) return;
            _isProcessingQueue = true;

            try
            {
                while (_queue.Count > 0)
                {
                    var req = _queue.Dequeue();
                    if (!_entryMap.TryGetValue(req.id, out var entry)) continue;

                    if (entry.allowOverlayStack)
                    {
                        ShowNowAsync(entry, req.setup, req.duration, req.playTransition).Forget();
                        continue;
                    }

                    _exclusiveActiveId = req.id;

                    await ShowNowAsync(entry, req.setup, req.duration, req.playTransition);
                    await UniTask.WaitUntil(() => !IsPopupVisible(req.id), cancellationToken: destroyCancellationToken);

                    _exclusiveActiveId = null;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _exclusiveActiveId = null;
                _isProcessingQueue = false;
            }
        }
    }
}