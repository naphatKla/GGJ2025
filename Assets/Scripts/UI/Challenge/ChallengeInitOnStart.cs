using System;
using System.Collections.Generic;
using Challenge;
using Demo;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Challenge
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class ChallengeInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public ChallengeContainer challengeDataContainer;
        
        [Space,Title("Debug")]
        [ShowInInspector] public HashSet<int> c_SelectedIndices = new();
        [ShowInInspector] public List<ChallengeDataSO> c_SelectedObjects = new();
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<ChallengeDataSO> _items = new();
        
        public event Action<int, ChallengeDataSO> OnSelected;
        public IReadOnlyCollection<int> GetSelectedIndices() => c_SelectedIndices;
        public IReadOnlyList<ChallengeDataSO> GetSelectedChallenges() => c_SelectedObjects;
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.Current : null;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<ChallengeDataSO>();
            totalCount = _items.Count;
            
            LoadSelectionFromPlayerData();
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

        private void LoadSelectionFromPlayerData()
        {
            var p = Current;
            if (p == null || p.SelectedChallenges == null) return;

            c_SelectedIndices.Clear();
            c_SelectedObjects.Clear();

            for (var i = 0; i < _items.Count; i++)
            {
                var ch = _items[i];
                if (ch == null || string.IsNullOrEmpty(ch.id)) continue;

                if (p.SelectedChallenges.Contains(ch.id))
                {
                    c_SelectedIndices.Add(i);
                    c_SelectedObjects.Add(ch);
                    if (ChallengeManager.Instance != null)
                        ChallengeManager.Instance.SelectChallenge(ch);
                }
            }
        }
        
        private List<ChallengeDataSO> LoadItems()
        {
            return challengeDataContainer.challengeList;
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
            if (index < 0 || index >= _items.Count)
                return;
            
            var challenge = _items[index];
            var player = Current;

            if (challenge == null || string.IsNullOrEmpty(challenge.id)) return;
            
            // if (IsLockedByPlayer(challenge)) return;
            
            if (c_SelectedIndices.Contains(index))
            {
                c_SelectedIndices.Remove(index);
                c_SelectedObjects.Remove(challenge);
                if (player?.SelectedChallenges != null)
                    player.SelectedChallenges.Remove(challenge.id);
                
                if (ChallengeManager.Instance != null)
                    ChallengeManager.Instance.DeselectChallenge(challenge);
            }
            else
            {
                c_SelectedIndices.Add(index);
                if (!c_SelectedObjects.Contains(challenge))
                    c_SelectedObjects.Add(challenge);
                
                if (player?.SelectedChallenges != null)
                    player.SelectedChallenges.Add(challenge.id);
                
                if (ChallengeManager.Instance != null)
                    ChallengeManager.Instance.SelectChallenge(challenge);
            }
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
                //vh.IsLocked = IsLockedByPlayer(content);
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
