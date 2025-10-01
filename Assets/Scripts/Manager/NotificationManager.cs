using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Manager.SoundManager;
using ProjectExtensions;
using Sirenix.OdinInspector;
using UI.Notification;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.Manager
{
    public class NotificationManager : NonAutoCreateSingleton<NotificationManager>
    {
        [Serializable]
        public class PrefabById
        {
            public string notifyID;
            public NotificationViewholderBase prefab;
            public Transform notificationParent;
        }

        [Serializable]
        public class NotifyEntry
        {
            public string id;
            public NotificationViewholderBase view;
            public int count;
            public float lifeTime;
            public string latestText;
            public CancellationTokenSource cts;
        }

        [Header("Setup")]
        [SerializeField] private List<PrefabById> prefabMap = new();
        [SerializeField] private NotificationViewholderBase fallbackPrefab;

        [Header("Behavior")]
        [SerializeField] private bool refreshTimeOnRepeat = true;
        [SerializeField] private bool bumpToTopOnRepeat   = true;
        [SerializeField] private bool updateTextOnRepeat  = true;
        [SerializeField] private int  maxVisible          = 20;

        private readonly Dictionary<string, NotifyEntry> _activeById = new();
        private readonly List<NotifyEntry> _activeList = new();
        
        private readonly Dictionary<NotificationViewholderBase, Stack<NotificationViewholderBase>> _pool = new();

        private void OnDisable()
        {
            ClearAll(true);
        }

        #region Public API

        /// <summary>
        /// Call Notification
        /// </summary>
        public void PlayNotification(string notifyID, string text, float lifeTimeSeconds, string variable = null)
        {
            if (string.IsNullOrEmpty(notifyID)) notifyID = "_default";
            
            SoundManager.Instance.PlayUI(SoundName.UI.Gameplay_ActionTextNotification);
            var key = MakeKey(notifyID, text);

            if (_activeById.TryGetValue(key, out var entry))
            {
                entry.count = Mathf.Clamp(entry.count + 1, 1, 9999);

                if (updateTextOnRepeat)
                {
                    entry.latestText = text;
                    entry.view.Bind(entry.latestText, entry.count);
                }
                else
                {
                    entry.view.SetCount(entry.count);
                }

                if (refreshTimeOnRepeat)
                {
                    entry.lifeTime = lifeTimeSeconds;
                    RestartLifetime(entry);
                }

                if (bumpToTopOnRepeat)
                    entry.view.transform.SetAsFirstSibling();

                entry.view.PlayBumpAsync().Forget();
                return;
            }
            
            var prefab = FindPrefab(notifyID);
            var view = Spawn(prefab);
            if (view == null)
            {
                Debug.LogWarning("[NotificationManager] No prefab resolved.");
                return;
            }

            view.transform.SetParent(GetParent(notifyID), false);
            view.transform.SetAsFirstSibling();

            var newEntry = new NotifyEntry
            {
                id         = notifyID,
                view       = view,
                count      = 1,
                lifeTime   = lifeTimeSeconds,
                latestText = text,
                cts        = new CancellationTokenSource()
            };

            view.Bind(text, 1);
            view.PlayInAsync(variable:variable).Forget();
            
            _activeById.Add(key, newEntry);
            _activeList.Add(newEntry);

            StartLifetimeTimer(newEntry).Forget();
            TrimIfExceedMax();
        }

        /// <summary>Clear all </summary>
        public void ClearAll(bool instant = false)
        {
            ClearAllAsync(instant).Forget();
        }

        public async UniTask ClearAllAsync(bool instant = false)
        {
            var snapshot = new List<NotifyEntry>(_activeList);

            _activeById.Clear();
            _activeList.Clear();

            foreach (var e in snapshot)
            {
                e.cts?.Cancel();
                e.cts?.Dispose();
                e.cts = null;

                if (instant)
                {
                    DespawnImmediate(e.view);
                }
                else
                {
                    await SafePlayOutAndDespawn(e.view);
                }
            }
        }

        #endregion

        #region Internals
        
        private static string MakeKey(string id, string text) => $"{id}:::{text}";

        private NotificationViewholderBase FindPrefab(string id)
        {
            foreach (var p in prefabMap)
            {
                if (p != null && p.notifyID == id && p.prefab != null)
                    return p.prefab;
            }
            return fallbackPrefab != null
                ? fallbackPrefab
                : (prefabMap.Count > 0 ? prefabMap[0].prefab : null);
        }

        private NotificationViewholderBase Spawn(NotificationViewholderBase prefab)
        {
            if (prefab == null) return null;

            if (!_pool.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<NotificationViewholderBase>();
                _pool[prefab] = stack;
            }

            NotificationViewholderBase view = null;
            if (stack.Count > 0) 
                view = stack.Pop();
            else 
                view = Instantiate(prefab);

            view.gameObject.SetActive(true);
            return view;
        }
        
        private Transform GetParent(string id)
        {
            foreach (var p in prefabMap)
            {
                if (p != null && p.notifyID == id && p.prefab != null)
                    return p.notificationParent;
            }
            return null;
        }

        private async UniTaskVoid StartLifetimeTimer(NotifyEntry e)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(e.lifeTime),
                    DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, e.cts.Token);
                
                await SafePlayOutAndDespawn(e.view);
                RemoveEntry(e);
            }
            catch (OperationCanceledException) {}
            catch (Exception ex) { Debug.LogException(ex); }
        }

        private void RestartLifetime(NotifyEntry e)
        {
            e.cts?.Cancel();
            e.cts?.Dispose();
            e.cts = new CancellationTokenSource();
            StartLifetimeTimer(e).Forget();
        }

        private async UniTask SafePlayOutAndDespawn(NotificationViewholderBase view)
        {
            if (!view) return;
            await view.PlayOutAsync();
            DespawnImmediate(view);
        }

        private void DespawnImmediate(NotificationViewholderBase view)
        {
            if (!view) return;

            NotificationViewholderBase key = null;
            foreach (var kv in _pool)
            {
                if (kv.Key && kv.Key.name == view.name.Replace("(Clone)", "").Trim())
                {
                    key = kv.Key;
                    break;
                }
            }

            if (key == null && _pool.Count > 0)
                foreach (var kv in _pool) { key = kv.Key; break; }

            view.gameObject.SetActive(false);
            if (key != null) _pool[key].Push(view);
            else Destroy(view.gameObject);
        }

        private void RemoveEntry(NotifyEntry e)
        {
            _activeList.Remove(e);
            _activeById.Remove(MakeKey(e.id, e.latestText));
        }

        private void TrimIfExceedMax()
        {
            if (maxVisible <= 0) return;

            while (_activeList.Count > maxVisible)
            {
                var last = _activeList[_activeList.Count - 1];
                last.cts?.Cancel();
                last.cts?.Dispose();
                last.cts = null;

                SafePlayOutAndDespawn(last.view).Forget();
                RemoveEntry(last);
            }
        }

        #endregion

        #region Test

#if UNITY_EDITOR
// ===== Editor Test : Random Text every call =====
        [FoldoutGroup("EditorTest")] [SerializeField]
        private bool edRT_PickKnownId = true;

        [FoldoutGroup("EditorTest")] [ValueDropdown(nameof(__CollectIdsForRandomText))] [SerializeField]
        private string edRT_FixedId = "_default";

        [FoldoutGroup("EditorTest")] [SerializeField]
        private bool edRT_AlwaysUseFixedId;

        [FoldoutGroup("EditorTest")] [SerializeField] [MinValue(0.25f)]
        private float edRT_Lifetime = 2f;

        [FoldoutGroup("EditorTest")] [SerializeField] [MinValue(1)]
        private int edRT_Count = 10;

        [FoldoutGroup("EditorTest")] [SerializeField] [MinValue(0f)]
        private float edRT_StepDelay = 0.05f;

        [FoldoutGroup("EditorTest")] [SerializeField]
        private string edRT_TextPrefix = "Rand";

        [FoldoutGroup("EditorTest")]
        [Button("Play Random Text (Single)")]
        [DisableInEditorMode]
        private void ED_PlayRandomText_Single()
        {
            var id = ResolveRandomTestId();
            var text = $"{edRT_TextPrefix} {__RandToken()}";
            PlayNotification(id, text, edRT_Lifetime);
        }

        [FoldoutGroup("EditorTest")]
        [Button("Play Random Text (Burst)")]
        [DisableInEditorMode]
        private void ED_PlayRandomText_Burst()
        {
            ED_PlayRandomText_BurstAsync().Forget();
        }
        
        [FoldoutGroup("EditorTest")]
        [Button("Play Notification")]
        [DisableInEditorMode]
        private void ED_PlayText_Single()
        {
            var id = ResolveRandomTestId();
            var text = $"{edRT_TextPrefix}";
            PlayNotification(id, text, edRT_Lifetime);
        }

        private async UniTask ED_PlayRandomText_BurstAsync()
        {
            for (var i = 0; i < edRT_Count; i++)
            {
                var id = ResolveRandomTestId();
                var text = $"{edRT_TextPrefix} {__RandToken()}";
                PlayNotification(id, text, edRT_Lifetime);

                if (edRT_StepDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(edRT_StepDelay), DelayType.UnscaledDeltaTime);
            }
        }

        private string ResolveRandomTestId()
        {
            if (edRT_PickKnownId && prefabMap != null && prefabMap.Count > 0)
            {
                if (edRT_AlwaysUseFixedId && !string.IsNullOrEmpty(edRT_FixedId))
                    return edRT_FixedId;

                var p = prefabMap[Random.Range(0, prefabMap.Count)];
                if (p != null && !string.IsNullOrEmpty(p.notifyID))
                    return p.notifyID;
            }

            return $"rand_{Random.Range(100000, 999999)}";
        }

        private IEnumerable<string> __CollectIdsForRandomText()
        {
            foreach (var p in prefabMap)
                if (p != null && !string.IsNullOrEmpty(p.notifyID))
                    yield return p.notifyID;
            yield return "_default";
        }
        
        private static string __RandToken(int len = 6)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz0123456789";
            var rng = new System.Random(Guid.NewGuid().GetHashCode());
            var buff = new char[len];
            for (var i = 0; i < len; i++) buff[i] = chars[rng.Next(chars.Length)];
            return new string(buff);
        }
#endif

        #endregion
    }
}
