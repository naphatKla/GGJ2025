using System;
using System.Collections.Generic;
using Demo;
using GameControl.SO;
using Player;
using Sirenix.OdinInspector;
using UI.MapSelection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.MapSelectionRework
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class MapSelectionInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public MapSelectionDataContainer mapSelectionDataContainer;

        [Space, Title("Display Status")] 
        public Button startButton;
        
        [Space,Title("Debug")]
        [ShowInInspector] public int m_SelectedIndex = -1;
        [ShowInInspector] public MapDataSO m_SelectedObject;
        
        Stack<Transform> pool = new Stack<Transform>();
        private List<MapDataSO> _items = new();
        public event Action<int, MapDataSO> OnSelected;
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.Current : null;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<MapDataSO>();
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
            
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => SceneManager.LoadScene("Gameplay"));
        }
        
        private void OnEnable()
        {
            SelectIndexImmediate(0);
            MapSelectionSender.Instance.currentmapSelectionDataContainer = mapSelectionDataContainer;
        }
        
        private (int times, int hi) GetStatsForMap(MapDataSO map)
        {
            if (map == null || Current == null) return (0, 0);
            return (
                Current.GetMapTimesPlayed(map.mapId),
                Current.GetMapHighestScore(map.mapId)
            );
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
                    var map = _items[index];
                    if (IsLockedByPlayer(map)) return;

                    m_SelectedIndex = index;
                    m_SelectedObject = map;
                    OnSelected?.Invoke(m_SelectedIndex, m_SelectedObject);
                    GetComponent<LoopScrollRect>().RefreshCells();
                    MapSelectionSender.Instance.currentMapSelectionIndex = index;
                });
            }
            return go;
        }
        
        private bool IsLockedByPlayer(MapDataSO map)
        {
            if (Current == null || map == null) return false;
            return Current.LockedMaps.Contains(map.mapId);
        }
        
        public void ProvideData(Transform transform, int idx)
        {
            var vh = transform.GetComponent<MapSelectionIndexViewholder>();
            if (vh != null)
            {
                var content = _items[idx];
                var (times, hi) = GetStatsForMap(content);
                
                vh.SetPrefabName("Map");   
                vh.IsLocked = IsLockedByPlayer(content);
                vh.ScrollCellIndex(idx, content);
                vh.SetClickedColor(idx == m_SelectedIndex);
                vh.UpdateViewholder(_items[idx]);
                UpdateDisplayStats(timesPlayed: times, highestScore: hi);
            }
            else
            {
                transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
            }
        }
        
        public void SelectIndexImmediate(int index)
        {
            if (_items == null || _items.Count == 0) return;

            index = Mathf.Clamp(index, 0, _items.Count - 1);
            if (IsLockedByPlayer(_items[index]))
                index = FindFirstUnlockedIndex();

            if (index < 0)
            {
                m_SelectedIndex = -1;
                m_SelectedObject = null;
                GetComponent<LoopScrollRect>().RefreshCells();
                return;
            }

            m_SelectedIndex = index;
            m_SelectedObject = _items[index];
            OnSelected?.Invoke(m_SelectedIndex, m_SelectedObject);
            GetComponent<LoopScrollRect>().RefreshCells();
            MapSelectionSender.Instance.currentMapSelectionIndex = index;
        }
        
        private int FindFirstUnlockedIndex()
        {
            for (int i = 0; i < _items.Count; i++)
                if (!IsLockedByPlayer(_items[i])) return i;
            return -1;
        }

        public void ReturnObject(Transform trans)
        {
            // Use `DestroyImmediate` here if you don't need Pool
            trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            pool.Push(trans);
        }
        
        private List<MapDataSO> LoadItems()
        {
            return mapSelectionDataContainer.mapSelectionList;
        }
        
        public void UpdateDisplayStats(int timesPlayed, int highestScore)
        {
            //timesPlayedText.text = $"Played: {timesPlayed}";
            //highestScoreText.text = $"Best: {highestScore:n0}";
        }

        [Button]
        public void UnlockMap(int index)
        {
            if (index < 0 || index >= _items.Count || Current == null) return;
            var id = _items[index].mapId;
            if (Current.LockedMaps.Remove(id))
            {
                if (m_SelectedIndex == -1) SelectIndexImmediate(index);
                SaveAndRefresh();
            }
        }
        
        [Button]
        public void LockMap(int index)
        {
            if (index < 0 || index >= _items.Count || Current == null) return;
            var id = _items[index].mapId;
            if (Current.LockedMaps.Add(id))
            {
                if (m_SelectedIndex == index)
                    SelectIndexImmediate(FindFirstUnlockedIndex());
                else
                    SaveAndRefresh();
            }
        }

        private void SaveAndRefresh()
        {
            ActiveProfileService.Instance?.SaveNow();
            GetComponent<LoopScrollRect>().RefreshCells();
        }
    }

}
