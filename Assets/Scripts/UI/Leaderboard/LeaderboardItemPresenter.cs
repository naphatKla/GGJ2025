using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Dan.Main;
using Player;
using TMPro;

namespace UI.Leaderboard
{
    [RequireComponent(typeof(LoopScrollRect))]
    public class LeaderboardItemPresenter : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        private static readonly List<LeaderboardItemPresenter> s_instances = new();

        /// <summary>รีเฟรชทุก LeaderboardItemPresenter ที่กำลัง active ในซีน</summary>
        public static void RefreshAll()
        {
            foreach (var p in s_instances)
                if (p != null && p.isActiveAndEnabled) p.RefreshNow();
        }

        /// <summary>รีเฟรชตัวแรกที่เจอ (สะดวกเรียกจากที่อื่นแบบ one-liner)</summary>
        public static void RefreshFirst()
        {
            if (s_instances.Count > 0)
            {
                var p = s_instances[0];
                if (p != null && p.isActiveAndEnabled) p.RefreshNow();
            }
        }

        // ---------- Inspector ----------
        [Header("UI")]
        public GameObject itemPrefab;
        public TMP_Text statusText;
        public TMP_Text currentRankText;

        [Header("Options")]
        public int maxEntries = 100;
        public bool sortDescending = true;

        [Header("Networking")]
        public float requestTimeoutSeconds = 15f;

        // ---------- Internal ----------
        private readonly List<LeaderboardItemModel> _items = new();
        private readonly Stack<Transform> _pool = new();
        private LoopScrollRect _ls;

        private bool _isFetching;
        

        void Awake()
        {
            _ls = GetComponent<LoopScrollRect>();
            _ls.prefabSource = this;
            _ls.dataSource   = this;
            _ls.totalCount = 0;
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
                    var mySubmitName = BuildSubmitName();
                    if (!string.IsNullOrEmpty(mySubmitName))
                    {
                        for (int i = 0; i < entries.Length; i++)
                        {
                            if (entries[i].Username == mySubmitName)
                            {
                                myRank = i + 1;
                                break;
                            }
                        }
                    }

                    if (currentRankText) currentRankText.text = (myRank > 0) ? $"YOUR RANK #{myRank}" : $"NOT IN TOP {maxEntries}";
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
        
        public GameObject GetObject(int index)
        {
            if (_pool.Count == 0) return Instantiate(itemPrefab);
            var tr = _pool.Pop();
            tr.gameObject.SetActive(true);
            return tr.gameObject;
        }

        public void ReturnObject(Transform trans)
        {
            var view = trans.GetComponent<LeaderboardItemViewholder>();
            if (view != null) view.OnCellReturn();
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            _pool.Push(trans);
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
            _ls.totalCount = _items.Count;
            if (refill) _ls.RefillCells(); else _ls.RefreshCells();
        }
    }
}
