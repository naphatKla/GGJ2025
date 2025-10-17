using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Dan.Main;
using Dan.Models;
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

        // ---------- Inspector ----------
        [Header("UI Top")]
        public GameObject top1ItemPrefab;
        public GameObject top2ItemPrefab;
        public GameObject top3ItemPrefab;
        public GameObject defaultItemPrefab;

        [Header("UI")]
        public TMP_Text statusText;
        public TMP_Text currentRankText;

        [Header("Options")]
        public int maxEntries = 100;
        public bool sortDescending = true;

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
            if (_isFetching) return;
            StartCoroutine(FetchEntriesRoutine());
        }

        private static string BuildSubmitName()
        {
            var profile = ActiveProfileService.Instance?.Current;
            return profile?.DisplayName;
        }

        private IEnumerator FetchEntriesRoutine()
        {
            _isFetching = true;
            SetStatus("Loading leaderboard...");

            bool done = false;
            bool success = false;
            string error = null;

            Leaderboards.ThailandGameShow.GetEntries(entries =>
            {
                try
                {
                    Array.Sort(entries, (a, b) => sortDescending
                        ? b.Score.CompareTo(a.Score)
                        : a.Score.CompareTo(b.Score));

                    int length = Mathf.Min(maxEntries, entries.Length);
                    var list = new List<LeaderboardItemModel>(length);
                    for (int i = 0; i < length; i++)
                        list.Add(new LeaderboardItemModel(entries[i].Username, entries[i].Score));

                    ResetItems(list, refill: true);

                    int myRank = -1;
                    
                    foreach (Entry entry in entries)
                    {
                        if (entry.IsMine())
                        {
                            myRank = entry.Rank;
                        }
                    }
                    
                    if (currentRankText)
                        currentRankText.text = (myRank > 0) ? $"YOUR RANK #{myRank}" : $"NOT IN TOP {maxEntries}";
                    success = true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally { done = true; }
            },
            err =>
            {
                error = err;
                done = true;
            });

            float timeout = Mathf.Max(1f, requestTimeoutSeconds);
            while (!done && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!done) error = "Timeout";

            if (success)
                SetStatus("");
            else
            {
                Debug.LogError($"[Leaderboard] fetch failed: {error}");
                SetStatus("Failed to load leaderboard");
                ResetItems(new List<LeaderboardItemModel>(), refill: true);
            }

            _isFetching = false;
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
                _ls.verticalNormalizedPosition = 1f;
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
            if (index == 0 && top1ItemPrefab) return ItemPrefabKind.Top1;
            if (index == 1 && top2ItemPrefab) return ItemPrefabKind.Top2;
            if (index == 2 && top3ItemPrefab) return ItemPrefabKind.Top3;
            return ItemPrefabKind.Default;
        }

        private GameObject GetPrefabByKind(ItemPrefabKind kind)
        {
            switch (kind)
            {
                case ItemPrefabKind.Top1:   return top1ItemPrefab;
                case ItemPrefabKind.Top2:   return top2ItemPrefab;
                case ItemPrefabKind.Top3:   return top3ItemPrefab;
                default:                    return defaultItemPrefab;
            }
        }
    }
}
