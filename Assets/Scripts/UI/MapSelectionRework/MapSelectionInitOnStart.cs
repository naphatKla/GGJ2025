using System;
using System.Collections.Generic;
using Demo;
using DG.Tweening;
using GameControl.SO;
using Interface;
using Player;
using Sirenix.OdinInspector;
using TMPro;
using UI.Challenge;
using UI.DotNotify;
using UI.Manager;
using UI.MapSelection;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MapSelectionRework
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class MapSelectionInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource, IRefreshUI
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        [Space,Title("Container")]
        [SerializeField] public MapSelectionDataContainer mapSelectionDataContainer;
        
        [Space,Title("Challenge")]
        [SerializeField] public GameObject challengeObjectList;
        [SerializeField] public ChallengeInitOnStart challengeScript;

        [Space, Title("Display Status")] 
        public Button mapButton;
        public TMP_Text mapButtonText;
        public Image imageDisplay;
        public TMP_Text mapNameText;
        public TMP_Text runText;
        public TMP_Text highestScoreText;
        
        [Space,Title("Debug")]
        [ShowInInspector] public int m_SelectedIndex = -1;
        [ShowInInspector] public MapDataSO m_SelectedObject;
        
        public bool mapConfirm;
        Stack<Transform> pool = new Stack<Transform>();
        private List<MapDataSO> _items = new();
        public event Action<int, MapDataSO> OnSelected;
        private PlayerData Current => ActiveProfileService.Instance != null ? ActiveProfileService.Instance.CurrentProfile : null;
        
        private Sequence _challengeSeq;
        private RectTransform _challengeRect;
        private CanvasGroup _challengeCanvas;
        private Vector2 _challengeoriginPos;
        
        void Awake()
        {
            _items = LoadItems() ?? new List<MapDataSO>();
            totalCount = _items.Count;
            _challengeRect = challengeObjectList.GetComponent<RectTransform>();
            _challengeCanvas = challengeObjectList.GetComponent<CanvasGroup>();
            _challengeoriginPos = _challengeRect.anchoredPosition;
        }
        
        void Start()
        {
            var ls = GetComponent<LoopScrollRect>();
            ls.prefabSource = this;
            ls.dataSource = this;
            ls.totalCount = totalCount;
            ls.RefillCells();
            ls.RefreshCells();
            
            mapButtonText.text = "SELECT CLASS";
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(() => AssignClassButton());
        }
        
        private void OnEnable()
        {
            if (Current != null) RefreshUI();
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public void RefreshUI()
        {
            SelectFromID(Current.selectedMapIds);
            MapSelectionSender.Instance.currentmapSelectionDataContainer = mapSelectionDataContainer;
        }

        /*private void ConfirmMap()
        {
            mapConfirm = true;
            mapButtonText.text = "SELECT CLASS";
            
            ShowChallenge();
            
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(() => AssignClassButton());
        }
        
        private void UnConfirmMap(bool forceHide)
        {
            mapConfirm = false;
            mapButtonText.text = "CONFIRM MAP";
            
            HideChallenge(forceHide);
            
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(() => ConfirmMap());
        }

        private void ShowChallenge()
        {
            _challengeSeq?.Kill();

            challengeObjectList.SetActive(true);

            _challengeRect.anchoredPosition = _challengeoriginPos + Vector2.left * 200f;
            _challengeCanvas.alpha = 0f;

            _challengeSeq = DOTween.Sequence()
                .Append(_challengeRect.DOAnchorPos(_challengeoriginPos, 0.45f)
                    .SetEase(Ease.OutCubic))
                .Join(_challengeCanvas.DOFade(1f, 0.35f))
                .SetUpdate(true);
        }
        
        private void HideChallenge(bool forceHide)
        {
            if (forceHide)
            {
                _challengeSeq?.Kill();
                challengeObjectList.SetActive(false);
                return;
            }
            _challengeSeq?.Kill();

            _challengeSeq = DOTween.Sequence()
                .Append(_challengeRect.DOAnchorPos(_challengeoriginPos + Vector2.left * 200f, 0.35f)
                    .SetEase(Ease.InCubic))
                .Join(_challengeCanvas.DOFade(0f, 0.25f))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    challengeObjectList.SetActive(false);
                });
        }*/
        
        private void AssignClassButton()
        {
            if (m_SelectedObject == null || Current == null)
            {
                NotificationManager.Instance?.PlayNotification("notify_warn", "Lock, Please unlock to continue.", 2f, NotificationType.Normal);
                return;
            }
            UIManager.Instance.OpenClassSelectionPanel();
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
                    RedDotService.Instance.Remove("Map:"+map.mapId);
                    if (IsLockedByPlayer(map)) return;
                    m_SelectedIndex = index;
                    m_SelectedObject = map;
                    OnSelected?.Invoke(m_SelectedIndex, m_SelectedObject);
                    GetComponent<LoopScrollRect>().RefreshCells();
                    UpdateDisplayStats(_items[index], Current);
                    
                    //if (Current.selectedMapIds != m_SelectedObject.mapId) UnConfirmMap(false);
                    Current.selectedMapIds = map.mapId;
                    MapSelectionSender.Instance.currentMapSelectionIndex = index;
                    MapSelectionSender.Instance.currentMapSelection = map;
                    challengeScript.RefreshUI();
                });
            }
            return go;
        }

        private bool IsLockedByPlayer(MapDataSO map)
        {
            if (Current == null || map == null) return false;
            return !Current.UnlockedMaps.Contains(map.mapId);
        }
        
        public void ProvideData(Transform transform, int idx)
        {
            var vh = transform.GetComponent<MapSelectionIndexViewholder>();
            if (vh != null)
            {
                var content = _items[idx];
                vh.SetPrefabName("Map");   
                vh.IsLocked = IsLockedByPlayer(content);
                vh.ScrollCellIndex(idx, content);
                vh.SetClickedColor(idx == m_SelectedIndex);
                vh.UpdateViewholder(_items[idx]);
                if (idx == m_SelectedIndex) UpdateDisplayStats(_items[idx], Current);
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
                m_SelectedIndex = -1;
                m_SelectedObject = null;
                GetComponent<LoopScrollRect>().RefreshCells();
                return;
            }

            m_SelectedIndex = index;
            m_SelectedObject = _items[index];
            OnSelected?.Invoke(m_SelectedIndex, m_SelectedObject);
            UpdateDisplayStats(_items[index], Current);
            GetComponent<LoopScrollRect>().RefreshCells();
            MapSelectionSender.Instance.currentMapSelectionIndex = index;
            MapSelectionSender.Instance.currentMapSelection = _items[index];
            Current.selectedMapIds = _items[index].mapId;
        }
        
        public void SelectFromID(string mapId)
        {
            if (_items == null || _items.Count == 0) return;
            if (string.IsNullOrEmpty(mapId)) return;
            int index = _items.FindIndex(m => m != null && m.mapId == mapId);
            if (index < 0) return;
            if (IsLockedByPlayer(_items[index])) return;

            m_SelectedIndex = index;
            m_SelectedObject = _items[index];

            OnSelected?.Invoke(m_SelectedIndex, m_SelectedObject);

            UpdateDisplayStats(_items[index], Current);
            GetComponent<LoopScrollRect>().RefreshCells();

            MapSelectionSender.Instance.currentMapSelectionIndex = index;
            MapSelectionSender.Instance.currentMapSelection = _items[index];

            Current.selectedMapIds = mapId;
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
        
        public void UpdateDisplayStats(MapDataSO mapData, PlayerData playerData)
        {
            if (mapData == null || playerData == null) return;

            if (imageDisplay != null) imageDisplay.sprite = mapData.image;
            if (mapNameText != null) mapNameText.text = mapData.mapName.ToUpper();
            if (runText != null)
            {
                if (playerData.GetMapTimesPlayed(mapData.mapId) == 0) 
                    runText.text = "NOT RECORD";
                else
                    runText.text = playerData.GetMapTimesPlayed(mapData.mapId).ToString();
            }

            if (highestScoreText != null)
            {
                if (playerData.GetMapHighestScore(mapData.mapId) == 0) 
                    highestScoreText.text = "NOT RECORD";
                else 
                    highestScoreText.text = playerData.GetMapHighestScore(mapData.mapId).ToString();
            }
        }

        [Button]
        public void UnlockMap(int index)
        {
            if (index < 0 || index >= _items.Count || Current == null) return;
            var id = _items[index].mapId;
            if (Current.UnlockedMaps.Add(id))
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
            if (Current.UnlockedMaps.Remove(id))
            {
                SaveAndRefresh();
            }
        }

        private void SaveAndRefresh()
        {
            ActiveProfileService.Instance.SaveNow();
            GetComponent<LoopScrollRect>().RefreshCells();
        }
    }

}
