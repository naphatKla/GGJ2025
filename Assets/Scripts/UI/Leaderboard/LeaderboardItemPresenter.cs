using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Player;

namespace UI.Leaderboard
{
    [RequireComponent(typeof(LoopScrollRect))]
    public class LeaderboardItemPresenter : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        private static readonly List<LeaderboardItemPresenter> s_instances = new();

        public static void RefreshAll()
        {
            foreach (var p in s_instances)
                if (p != null && p.isActiveAndEnabled) p.RefreshNow();
        }

        public static void RefreshAllDelayed(int delayMilliseconds)
        {
            RefreshAllDelayedAsync(delayMilliseconds).Forget();
        }

        // ---------- Inspector ----------
        [Header("UI Top")]
        public GameObject top1ItemPrefab;
        public GameObject top2ItemPrefab;
        public GameObject top3ItemPrefab;
        public GameObject defaultItemPrefab;

        [Header("UI")]
        public TMP_Text statusText;
        public TMP_Text currentRankText;
        public Button refreshButton;

        [Header("Options")]
        public int maxEntries = 100;
        public bool sortDescending = true;
        public bool useDefaultPrefab = true;

        [Header("Networking")]
        public float requestTimeoutSeconds = 15f;

        // ---------- Internal ----------
        private readonly List<LeaderboardItemModel> _items = new();

        //Top1/Top2/Top3/Default
        private readonly Stack<Transform> _poolTop1    = new();
        private readonly Stack<Transform> _poolTop2    = new();
        private readonly Stack<Transform> _poolTop3    = new();
        private readonly Stack<Transform> _poolDefault = new();

        private LoopScrollRect _ls;
        private bool _isFetching;
        private bool _refreshQueued;
        private bool _scrollToTopPending;


        void Awake()
        {
            _ls = GetComponent<LoopScrollRect>();
            _ls.prefabSource = this;
            _ls.dataSource   = this;
            _ls.totalCount   = 0;
            _ls.RefillCells();
        }

        void OnEnable()
        {
            if (!s_instances.Contains(this)) s_instances.Add(this);
            if (refreshButton)
            {
                refreshButton.onClick.RemoveAllListeners();
                refreshButton.onClick.AddListener(RefreshNow);
            }
        }

        void OnDisable()
        {
            s_instances.Remove(this);
        }

        void Start()
        {
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_isFetching)
            {
                _refreshQueued = true;
                return;
            }

            Debug.Log("[Leaderboard] Refreshing");
            FetchEntriesAsync().Forget();
        }

        private static async UniTaskVoid RefreshAllDelayedAsync(int delayMilliseconds)
        {
            await UniTask.Delay(Mathf.Max(0, delayMilliseconds));
            RefreshAll();
        }

        private async UniTaskVoid FetchEntriesAsync()
        {
            _isFetching = true;
            SetStatus("Loading leaderboard...");

            try
            {
                var profileService = ActiveProfileService.Instance;
                var profile = profileService?.CurrentProfile ?? profileService?.LoadCurrent();
                var snapshot = await PlayFabLeaderboardService.GetTopScoresAsync(profile, maxEntries);

                if (snapshot.Entries.Count == 0 && profile != null && profile.HighestScore > 0)
                {
                    Debug.Log($"[Leaderboard] No PlayFab rows found. Backfilling local highest score: {profile.HighestScore}");
                    await PlayFabLeaderboardService.SubmitHighestScoreAsync(profile, profile.HighestScore);
                    snapshot = await PlayFabLeaderboardService.GetTopScoresAsync(profile, maxEntries);
                }

                Debug.Log($"[Leaderboard] PlayFab fetch success: {snapshot.Entries.Count}/{snapshot.EntryCount} rows");
                var list = new List<LeaderboardItemModel>(snapshot.Entries.Count);

                foreach (var entry in snapshot.Entries)
                    list.Add(new LeaderboardItemModel(entry.DisplayName, entry.Score));

                if (!sortDescending)
                    list.Reverse();

                ResetItems(list, refill: true);

                if (currentRankText)
                {
                    var maxShown = Mathf.Clamp(maxEntries, 1, 100);
                    currentRankText.text = snapshot.CurrentPlayerRank > 0
                        ? $"YOUR RANK #{snapshot.CurrentPlayerRank}"
                        : $"NOT IN TOP {maxShown}";
                }

                SetStatus("");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Leaderboard] PlayFab fetch failed: {ex.Message}");
                SetStatus("Failed to load leaderboard");
                ResetItems(new List<LeaderboardItemModel>(), refill: true);
            }
            finally
            {
                _isFetching = false;

                if (_refreshQueued && isActiveAndEnabled)
                {
                    _refreshQueued = false;
                    RefreshNow();
                }
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText) statusText.text = msg;
        }

        // ---------- PrefabSource ----------
        public GameObject GetObject(int index)
        {
            var kind = GetKindForIndex(index);
            Transform tr = null;
            switch (kind)
            {
                case ItemPrefabKind.Top1:
                    if (_poolTop1.Count > 0) tr = _poolTop1.Pop();
                    break;
                case ItemPrefabKind.Top2:
                    if (_poolTop2.Count > 0) tr = _poolTop2.Pop();
                    break;
                case ItemPrefabKind.Top3:
                    if (_poolTop3.Count > 0) tr = _poolTop3.Pop();
                    break;
                default:
                    if (_poolDefault.Count > 0) tr = _poolDefault.Pop();
                    break;
            }

            if (tr != null)
            {
                tr.gameObject.SetActive(true);
                return tr.gameObject;
            }
            
            var prefab = GetPrefabByKind(kind);
            if (prefab == null) prefab = defaultItemPrefab;

            var go = Instantiate(prefab);
            
            var tag = go.GetComponent<LeaderboardCellTag>();
            if (tag == null)
            {
                tag = go.AddComponent<LeaderboardCellTag>();
                tag.kind = kind;
            }

            return go;
        }

        public void ReturnObject(Transform trans)
        {
            var view = trans.GetComponent<LeaderboardItemViewholder>();
            if (view != null) view.OnCellReturn();

            var tag = trans.GetComponent<LeaderboardCellTag>();
            var kind = tag != null ? tag.kind : ItemPrefabKind.Default;

            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);

            switch (kind)
            {
                case ItemPrefabKind.Top1: _poolTop1.Push(trans); break;
                case ItemPrefabKind.Top2: _poolTop2.Push(trans); break;
                case ItemPrefabKind.Top3: _poolTop3.Push(trans); break;
                default: _poolDefault.Push(trans); break;
            }
        }

        // ---------- LoopScroll: Data Source ----------
        public void ProvideData(Transform cell, int idx)
        {
            if (idx < 0 || idx >= _items.Count) return;
            var view = cell.GetComponent<LeaderboardItemViewholder>();
            if (view == null) return;
            view.SetData(_items[idx], idx);
        }

        // ---------- Public item ops ----------
        public void UpdateItem(int index, LeaderboardItemModel newData)
        {
            if (index < 0 || index >= _items.Count) return;
            _items[index] = newData;
            _ls.RefreshCells();
        }

        public void AddItems(IEnumerable<LeaderboardItemModel> more)
        {
            _items.AddRange(more);
            _ls.totalCount = _items.Count;
            _ls.RefillCells();
        }

        public void ResetItems(List<LeaderboardItemModel> newList, bool refill = true)
        {
            _items.Clear();
            _items.AddRange(newList);

            _ls.StopMovement();
            _ls.ClearCells(); 
            _ls.totalCount = _items.Count;

            if (refill)
            {
                _ls.RefillCells(); 
                _ls.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_ls.content);
            }
            else
            {
                _ls.RefreshCells();
            }
        }


        // ---------- Helpers ----------
        private ItemPrefabKind GetKindForIndex(int index)
        {
            if (useDefaultPrefab) return ItemPrefabKind.Default;
            if (index == 0 && top1ItemPrefab) return ItemPrefabKind.Top1;
            if (index == 1 && top2ItemPrefab) return ItemPrefabKind.Top2;
            if (index == 2 && top3ItemPrefab) return ItemPrefabKind.Top3;
            return ItemPrefabKind.Default;
        }

        private GameObject GetPrefabByKind(ItemPrefabKind kind)
        {
            switch (kind)
            {
                case ItemPrefabKind.Default: return defaultItemPrefab;
                case ItemPrefabKind.Top1:   return top1ItemPrefab;
                case ItemPrefabKind.Top2:   return top2ItemPrefab;
                case ItemPrefabKind.Top3:   return top3ItemPrefab;
                default:                    return defaultItemPrefab;
            }
        }
    }
}
