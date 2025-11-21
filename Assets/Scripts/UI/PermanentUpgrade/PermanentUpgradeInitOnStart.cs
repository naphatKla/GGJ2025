using System;
using System.Collections.Generic;
using Demo;
using GameControl.SO;
using PermanentUpgrade;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PermanentUpgrade
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class PermanentUpgradeInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public PermanentUpgradeConfig permanentConfig;

        [Space, Title("Display Status")] 
        [SerializeField] public PermanentUpgradeDisplay permanentDisplay;
        
        [Space,Title("Debug")]
        [ShowInInspector] public int p_SelectedIndex = -1;
        [ShowInInspector] public PermanentUpgradeEntry p_SelectedObject;
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<PermanentUpgradeEntry> _items = new();
        private PlayerData CurrentProfile => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<PermanentUpgradeEntry>();
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
            SelectIndexImmediate(0);
            ActiveProfileService.Instance.OnPermanentUpgrade += UpdateUI;
        }

        private void OnDisable()
        {
            if (ActiveProfileService.Current != null) ActiveProfileService.Current.OnPermanentUpgrade -= UpdateUI;
        }

        private void UpdateUI()
        {
            GetComponent<LoopScrollRect>().RefreshCells();
        }
        
        private List<PermanentUpgradeEntry> LoadItems()
        {
            return permanentConfig.GetAllEntry();
        }
        
        private bool IsLockedByPlayer(PermanentUpgradeEntry entry)
        {
            if (CurrentProfile == null || entry == null) return false;
            return !CurrentProfile.UnlockedPermanentUpgrade.Contains(entry.type);
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
                    var pEntry = _items[index];
                    if (IsLockedByPlayer(pEntry)) return;

                    p_SelectedIndex = index;
                    p_SelectedObject = pEntry;
                    GetComponent<LoopScrollRect>().RefreshCells();
                    UpdatePermanentRightDisplay(index);
                });
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
            var vh = transform.GetComponent<PermanentUpgradeViewholder>();
            if (vh != null)
            {
                var content = _items[idx];
                vh.SetPrefabName("PermanentUpgrade");   
                vh.IsLocked = IsLockedByPlayer(content);
                vh.ScrollCellIndex(idx, content);
                vh.SetClickedColor(idx == p_SelectedIndex);
                vh.UpdateViewholder(_items[idx], CurrentProfile);
                if (idx == p_SelectedIndex) UpdatePermanentRightDisplay(idx);
            }
            else
            {
                transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
            }
        }
        
        public void SelectIndexImmediate(int index)
        {
            if (_items == null || _items.Count == 0) return;
            if (IsLockedByPlayer(_items[index])) return;
            index = Mathf.Clamp(index, 0, _items.Count - 1);
            if (index < 0)
            {
                p_SelectedIndex = -1;
                p_SelectedObject = null;
                GetComponent<LoopScrollRect>().RefreshCells();
                return;
            }

            p_SelectedIndex = index;
            p_SelectedObject = _items[index];
            if (index == p_SelectedIndex) UpdatePermanentRightDisplay(index);
            GetComponent<LoopScrollRect>().RefreshCells();
        }

        public void UpdatePermanentRightDisplay(int index)
        {
            permanentDisplay.UpdatePermanentDisplay(permanentConfig,_items[index], CurrentProfile);
            permanentDisplay.upgradeButton.onClick.RemoveAllListeners();
            permanentDisplay.upgradeButton.onClick.AddListener(() =>
            {
                var click = CurrentProfile.TryUpgrade(_items[index].type,permanentConfig);
                if (click)
                {
                    NotificationManager.Instance?.PlayNotification("notify_warnrb", "Upgrade Successfully!", 2f);
                }
                else
                {
                    NotificationManager.Instance?.PlayNotification("notify_warnrb", "Not enough coin!", 2f);
                }
                SaveAndRefresh();
            });
        }
        
        private void SaveAndRefresh()
        {
            ActiveProfileService.Instance.SaveNow();
            GetComponent<LoopScrollRect>().RefreshCells();
        }
    }
}