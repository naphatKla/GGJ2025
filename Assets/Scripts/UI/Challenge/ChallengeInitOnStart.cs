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
        private List<ChallengeDataSO> _allItems = new();
        private List<ChallengeDataSO> _items = new();
        
        public event Action<int, ChallengeDataSO> OnSelected;
        public IReadOnlyCollection<int> GetSelectedIndices() => c_SelectedIndices;
        public IReadOnlyList<ChallengeDataSO> GetSelectedChallenges() => c_SelectedObjects;
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        private string CurrentMapId => MapSelectionSender.Instance != null && MapSelectionSender.Instance.currentMapSelection != null
                ? MapSelectionSender.Instance.currentMapSelection.mapId
                : null;
        
        void Awake()
        {
            _allItems = new List<ChallengeDataSO>(challengeDataContainer.challengeList);
            RebuildVisibleItems(); 
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
            RebuildVisibleItems();
            LoadSelectionFromPlayerData();
            var ls = GetComponent<LoopScrollRect>();
            ls.RefreshCells();
        }
        
        private void RebuildVisibleItems()
        {
            _items.Clear();
            var mapId = CurrentMapId;

            foreach (var ch in _allItems)
            {
                if (ch == null) continue;
                if (ch.hideFromUI) continue;
                if (!ch.IsAvailableForMap(mapId)) continue;
                _items.Add(ch);
            }

            SortItems();
        }

        private void LoadSelectionFromPlayerData()
        {
            var p = Current;
            if (p == null || p.SelectedChallengesPerMap == null) return;

            string mapId = CurrentMapId;

            c_SelectedIndices.Clear();
            c_SelectedObjects.Clear();
            ChallengeManager.Instance?.ResetAllSelectedChallenge();

            if (!p.SelectedChallengesPerMap.TryGetValue(mapId, out var selectedIds) || selectedIds == null)
            {
                selectedIds = new HashSet<string>();
                p.SelectedChallengesPerMap[mapId] = selectedIds;
            }
            
            //Force confirm-selected challenges
            for (int i = 0; i < _items.Count; i++)
            {
                var ch = _items[i];
                if (ch == null || string.IsNullOrEmpty(ch.id)) continue;
                if (IsLockedByPlayer(ch)) continue;
                if (ch.IsConfirmSelectedForMap(mapId))
                    selectedIds.Add(ch.id);
            }

            //Build UI selection caches
            for (int i = 0; i < _items.Count; i++)
            {
                var ch = _items[i];
                if (ch == null || string.IsNullOrEmpty(ch.id)) continue;

                if (selectedIds.Contains(ch.id))
                {
                    c_SelectedIndices.Add(i);
                    c_SelectedObjects.Add(ch);
                    ChallengeManager.Instance?.SelectChallenge(ch);
                }
            }
            
            if (selectedIds.Count == 0) p.SelectedChallengesPerMap.Remove(mapId);
            var sender = MapSelectionSender.Instance.challengeData;
            if (scoreMultiply) scoreMultiply.text = "+" + sender.TotalPercent + "%";
            ActiveProfileService.Instance?.SaveNow();
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

                if (aUnlocked != bUnlocked)
                    return aUnlocked ? -1 : 1;

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
                if (challenge.IsConfirmSelectedForMap(mapId))
                    return;
                
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
