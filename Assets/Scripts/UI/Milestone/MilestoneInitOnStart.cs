using System.Collections;
using System.Collections.Generic;
using Challenge;
using Demo;
using Interface;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UI.Challenge;
using UI.MapSelection;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Milestone
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class MilestoneInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource, IRefreshUI
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;

        [Space, Title("Display")] 
        public TMP_Text textDisplay;
        public TMP_Text textCurrentSelect;
        public CanvasGroup canvasGroup;
        
        [Space,Title("Container")]
        [SerializeField] public MilestoneDataContainer milestoneDataContainer;
        
        [Space,Title("Challenge")]
        [SerializeField] public ChallengeInitOnStart challengeScript;
        
        [Space,Title("Debug")]
        [ShowInInspector] public int m_SelectedIndex = -1;
        [ShowInInspector] public ChallengeDataSO m_SelectedObject;
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<ChallengeDataSO> _items = new();
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        private string CurrentMapId => MapSelectionSender.Instance != null && MapSelectionSender.Instance.currentMapSelection != null
            ? MapSelectionSender.Instance.currentMapSelection.mapId
            : null;
        
        void Awake()
        {
            LoadItems();
        }
        
        void Start()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.prefabSource = this;
            ls.dataSource = this;
            RefreshUI();
        }
        
        private void LoadItems()
        {
            _items.Clear();
            totalCount = 0;

            if (milestoneDataContainer == null) return;

            var mapId = CurrentMapId;
            if (string.IsNullOrEmpty(mapId)) return;
            var match = milestoneDataContainer.milestoneList
                .Find(x => x != null && x.mapID == mapId);

            if (match == null || match.milestoneEntries == null)
            {
                canvasGroup.alpha = 0;
                return;
            }
            
            canvasGroup.alpha = 1;
            _items = new List<ChallengeDataSO>(match.milestoneEntries);
            totalCount = _items.Count;
        }
        
        public GameObject GetGameObject()
        {
            return gameObject;
        }
        
        private void ApplyToScrollRect()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.totalCount = totalCount;
            ls.RefillCells();
            ls.RefreshCells();
        }


        [Button]
        public void RefreshUI()
        {
            var mapId = CurrentMapId;
            if (string.IsNullOrEmpty(mapId))
            {
                _items.Clear();
                totalCount = 0;
                ApplyToScrollRect();
                return;
            }
            var mapState = Current.GetOrCreateMapStat(mapId);
            SelectIndex(mapState.SelectedLevelMilestone, mapState);
            LoadItems();
            ApplyToScrollRect();
        }
        
        private bool IsLockedByPlayer(int index, MapStat mapStat)
        {
            return mapStat.MaxLevelUnlockMilestone < index;
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
                    string mapId = CurrentMapId;
                    MapStat mapState = PlayerDataExtensions.GetOrCreateMapStat(Current , mapId);
                    
                    var milestone = _items[index];
                    if (IsLockedByPlayer(index, mapState)) return;
                    if (m_SelectedObject == milestone) return;
                    m_SelectedIndex = index;
                    m_SelectedObject = milestone;
                    GetComponent<LoopScrollRect>().RefreshCells();
                    mapState.SelectedLevelMilestone = index;
                    UpdateMilestoneObjective(milestone, mapState);
                    challengeScript.RefreshUI();
                    //challengeScript.RefreshUI();
                });
            }
            return go;
        }

        public void ReturnObject(Transform trans)
        {
            // Use `DestroyImmediate` here if you don't need Pool
            trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            pool.Push(trans);
        }

        public void ProvideData(Transform transform, int idx)
        {
            var vh = transform.GetComponent<MilestoneViewholder>();
            string mapId = CurrentMapId;
            MapStat mapState = PlayerDataExtensions.GetOrCreateMapStat(Current , mapId);
            
            if (vh != null)
            {
                var content = _items[idx];
                vh.SetPrefabName("Milestone");   
                vh.ScrollCellIndex(idx, content);
                vh.SetClickedColor(idx == m_SelectedIndex);
                vh.UpdateViewholder(idx,_items[idx], mapState);
            }
            else
            {
                transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void UpdateMilestoneObjective(ChallengeDataSO challengeDataSo, MapStat mapStat)
        {
            if (challengeDataSo == null) return;
            if (textDisplay != null) textDisplay.text = challengeDataSo.description;
            if (textCurrentSelect != null) textCurrentSelect.text = "Level " + mapStat.SelectedLevelMilestone;
        }

        public bool SelectIndex(int index, MapStat mapState)
        {
            if (_items == null || _items.Count == 0) return false;
            if (index < 0 || index >= _items.Count) return false;

            var mapId = CurrentMapId;
            if (string.IsNullOrEmpty(mapId)) return false;
            if (IsLockedByPlayer(index, mapState)) return false;

            var milestone = _items[index];
            if (milestone == null) return false;

            if (m_SelectedObject == milestone && m_SelectedIndex == index) return false;

            m_SelectedIndex = index;
            m_SelectedObject = milestone;
            mapState.SelectedLevelMilestone = index;
            var ls = GetComponent<LoopScrollRect>();
            ls.RefreshCells();
            UpdateMilestoneObjective(milestone, mapState);
            challengeScript.RefreshUI();
            return true;
        }
    }
}


