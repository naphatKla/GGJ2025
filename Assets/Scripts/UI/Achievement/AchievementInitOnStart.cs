using System;
using System.Collections.Generic;
using Achievements;
using Demo;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Achievement
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class AchievementInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public AchievementDatabase achievementDataContainer;

        [Space,Title("Debug")]
        [ShowInInspector] public int m_SelectedIndex = -1;
        [ShowInInspector] public AchievementEntry m_SelectedObject;
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<AchievementEntry> _items = new();
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<AchievementEntry>();
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
            var ls = GetComponent<LoopScrollRect>();
            ls.RefreshCells();
        }

        private List<AchievementEntry> LoadItems()
        {
            return achievementDataContainer.entries;
        }
        
        private bool IsLockedByPlayer(AchievementEntry achievementEntry)
        {
            if (Current == null || achievementEntry == null) return false;
            return !Current.UnlockedAchievements.Contains(achievementEntry.id);
        }
        
        public GameObject GetObject(int index)
        {
            var go = pool.Count == 0 ? Instantiate(item) : pool.Pop().gameObject;
            go.SetActive(true);
            
            var cb = go.GetComponent<ScrollIndexCallbackBase>();
            if (cb != null)
            {
                cb.onClick_InitOnStart.RemoveAllListeners();
                /*cb.onClick_InitOnStart.AddListener(() =>
                {
                    var achievementEntry = _items[index];
                    if (IsLockedByPlayer(achievementEntry)) return;

                    m_SelectedIndex = index;
                    m_SelectedObject = achievementEntry;
                    OnSelected?.Invoke(m_SelectedIndex, m_SelectedObject);
                    GetComponent<LoopScrollRect>().RefreshCells();
                });*/
            }
            return go;
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
            var vh = transform.GetComponent<AchievementViewholder>();
            if (vh != null)
            {
                var content = _items[idx];
                vh.SetPrefabName("Achievement");   
                vh.ScrollCellIndex(idx, content);
                //vh.SetClickedColor(idx == m_SelectedIndex);*/
                vh.IsLocked = IsLockedByPlayer(content);
                vh.UpdateViewholder(_items[idx], Current);
            }
            else
            {
                transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
