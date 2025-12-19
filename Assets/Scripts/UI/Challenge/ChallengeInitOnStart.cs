using System;
using System.Collections.Generic;
using Challenge;
using Demo;
using Interface;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UI.DotNotify;
using UI.MapSelection;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Challenge
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class ChallengeInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource, IRefreshUI
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public ChallengeContainer challengeDataContainer;
        
        [Space, Title("Display Status")] 
        public TMP_Text scoreMultiply;
        
        [Space,Title("Debug")]
        [ShowInInspector] public HashSet<int> c_SelectedIndices = new();
        [ShowInInspector] public List<ChallengeDataSO> c_SelectedObjects = new();
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<ChallengeDataSO> _items = new();
        
        public event Action<int, ChallengeDataSO> OnSelected;
        public IReadOnlyCollection<int> GetSelectedIndices() => c_SelectedIndices;
        public IReadOnlyList<ChallengeDataSO> GetSelectedChallenges() => c_SelectedObjects;
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<ChallengeDataSO>();
            totalCount = _items.Count;
        }
        
        void Start()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.prefabSource = this;
            ls.dataSource = this;
            ls.totalCount = totalCount;
            ls.RefillCells();
            ls.RefreshCells();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public void RefreshUI()
        {
            SortItems();
            LoadSelectionFromPlayerData();
            var ls = GetComponent<LoopScrollRect>();
            ls.RefreshCells();
        }

        private void LoadSelectionFromPlayerData()
        {
            var p = Current;
            if (p == null || p.SelectedChallengesPerMap == null) return;
            string mapId = Current.selectedMapIds;

            c_SelectedIndices.Clear();
            c_SelectedObjects.Clear();

            ChallengeManager.Instance?.ResetAllSelectedChallenge();

            if (!p.SelectedChallengesPerMap.TryGetValue(mapId, out var selectedIds))
                selectedIds = null;

            for (int i = 0; i < _items.Count; i++)
            {
                var ch = _items[i];
                if (ch == null || string.IsNullOrEmpty(ch.id)) continue;

                if (selectedIds != null && selectedIds.Contains(ch.id))
                {
                    c_SelectedIndices.Add(i);
                    c_SelectedObjects.Add(ch);
                    ChallengeManager.Instance?.SelectChallenge(ch);
                }
            }

            var sender = MapSelectionSender.Instance.challengeData;
            if (scoreMultiply) scoreMultiply.text = "+" + sender.TotalPercent + "%";
        }
        
        private List<ChallengeDataSO> LoadItems()
        {
            _items = new List<ChallengeDataSO>(challengeDataContainer.challengeList);
            SortItems();
            return _items;
        }
        
        private void SortItems()
        {
            if (Current == null || _items == null) return;

            _items.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                bool aUnlocked = Current.UnlockedChallenges.Contains(a.id);
                bool bUnlocked = Current.UnlockedChallenges.Contains(b.id);

                // Unlocked ขึ้นก่อน
                if (aUnlocked != bUnlocked)
                    return aUnlocked ? -1 : 1;

                // ถ้าสถานะเหมือนกัน → เรียงตาม id
                return string.Compare(a.id, b.id, StringComparison.Ordinal);
            });
        }
        
        public GameObject GetObject(int index)
        {
            var go = pool.Count == 0 ? Instantiate(item) : pool.Pop().gameObject;
            go.SetActive(true);
            
            var cb = go.GetComponent<ScrollIndexCallbackBase>();
            if (cb != null)
            {
                cb.onClick_InitOnStart.RemoveAllListeners();
                cb.onClick_InitOnStart.AddListener(() =>
                {
                    OnItemClicked(index);
                });
            }
            return go;
        }

        private void OnItemClicked(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            var challenge = _items[index];
            var player = Current;
            if (challenge == null || string.IsNullOrEmpty(challenge.id)) return;
            if (IsLockedByPlayer(challenge)) return;
            string mapId = MapSelectionSender.Instance.currentMapSelection.mapId;

            RedDotService.Instance.Remove("Challenge:" + challenge.id);

            if (!player.SelectedChallengesPerMap.TryGetValue(mapId, out var set))
            {
                set = new HashSet<string>();
                player.SelectedChallengesPerMap[mapId] = set;
            }

            if (set.Contains(challenge.id))
            {
                // Unselect
                set.Remove(challenge.id);
                c_SelectedIndices.Remove(index);
                c_SelectedObjects.Remove(challenge);
                ChallengeManager.Instance?.DeselectChallenge(challenge);
            }
            else
            {
                // Select
                set.Add(challenge.id);
                c_SelectedIndices.Add(index);
                if (!c_SelectedObjects.Contains(challenge))
                    c_SelectedObjects.Add(challenge);
                ChallengeManager.Instance?.SelectChallenge(challenge);
            }

            if (set.Count == 0)
                player.SelectedChallengesPerMap.Remove(mapId);

            var sender = MapSelectionSender.Instance.challengeData;
            if (scoreMultiply) scoreMultiply.text = "+" + sender.TotalPercent + "%";

            OnSelected?.Invoke(index, challenge);
            ActiveProfileService.Instance?.SaveNow();
            GetComponent<LoopScrollRect>().RefreshCells();
        }

        
        private bool IsLockedByPlayer(ChallengeDataSO challenge)
        {
            if (Current == null || challenge == null) return false;
            return !Current.UnlockedChallenges.Contains(challenge.id);
        }

        public void ReturnObject(Transform trans)
        {
            trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            pool.Push(trans);
        }

        public void ProvideData(Transform transform, int idx)
        {
            var vh = transform.GetComponent<ChallengeSelectViewholder>();
            if (vh != null)
            {
                var content = _items[idx];
                vh.SetPrefabName("Challenge"); 
                vh.IsLocked = IsLockedByPlayer(content);
                vh.ScrollCellIndex(idx, content);
                vh.UpdateViewholder(content);
                bool isSelected = c_SelectedIndices.Contains(idx);
                vh.SetClickedColor(isSelected);
            }
            else
            {
                transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
