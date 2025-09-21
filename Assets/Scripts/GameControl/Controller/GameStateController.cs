using System;
using System.Collections.Generic;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.GameState;
using GameControl.Interface;
using GameControl.SO;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using Sirenix.OdinInspector;
using UI.MapSelection;
using UnityEngine.SceneManagement;

namespace GameControl.Controller
{
    public enum EndResult
    {
        None,
        Completed,
        Failed,
    }
    
    [RequireComponent(typeof(SpawnerStateController), typeof(GameTimer))]
    public class GameStateController : MMSingleton<GameStateController>
    {
        private IGameState _currentState;
        
        private TutorialState _tutorialState;
        private PrestartState _prestartState;
        private StartState _startState;
        private EndState _endState;
        private SummaryState _summaryState;

        [ShowInInspector, ReadOnly]
        private string _currentStateName;
        
        [ShowInInspector, ReadOnly]
        private MapDataSO _currentMapDataRuntime;
        
        [SerializeField] private MapSelectionDataContainer mapContainer;
        [Tooltip("The index of the current map in the mapData list.")]
        [SerializeField] private int currentMapIndex;
        private List<MapDataSO> MapDataList =>
            mapContainer != null && mapContainer.mapSelectionList != null
                ? mapContainer.mapSelectionList
                : new List<MapDataSO>();
        
        public MapDataSO CurrentMap => _currentMapDataRuntime;
        
        public EndResult gameResult;
        public IGameState CurrentState => _currentState;
        
        protected override void Awake()
        {
            base.Awake();
            _tutorialState = new TutorialState();
            _prestartState = new PrestartState();
            _startState = new StartState();
            _endState = new EndState();
            _summaryState = new SummaryState();
            
            if (MapDataList.Count > 0)
            {
                currentMapIndex = Mathf.Clamp(currentMapIndex, 0, MapDataList.Count - 1);
                AssignMapRuntime(MapDataList[currentMapIndex]);
            }
            else
            {
                _currentMapDataRuntime = null;
            }
        }

        private MapDataSO MakeRuntimeCopy(MapDataSO src)
        {
            if (src == null) return null;
            var copy = Instantiate(src);
            copy.name = src.name + " (Runtime)";
            copy.hideFlags = HideFlags.DontSave;
            return copy;
        }

        private void AssignMapRuntime(MapDataSO asset)
        {
            if (_currentMapDataRuntime != null) Destroy(_currentMapDataRuntime);
            _currentMapDataRuntime = MakeRuntimeCopy(asset);
        }
        
        private void OnEnable()
        {
            _currentState?.OnEnable(this);
        }
        
        private void OnDisable()
        {
            _currentState?.OnDisable(this);
        }

        private void Start()
        {
            SetupGameSelection().Forget();
        }

        private void Update()
        {
            _currentState?.Update(this);
        }

        public void SetState(IGameState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
            _currentStateName = _currentState?.GetType().Name;
        }

        private async UniTask SetupGameSelection()
        {
            await UniTask.WaitUntil(() => MapSelectionSender.Instance != null);
            currentMapIndex = MapSelectionSender.Instance.currentMapSelectionIndex;
            if (MapDataList.Count == 0) { _currentMapDataRuntime = null; return; }
            currentMapIndex = Mathf.Clamp(currentMapIndex, 0, MapDataList.Count - 1);
            AssignMapRuntime(MapDataList[currentMapIndex]);
            SetState(_tutorialState);
        }

        public void RestartMap()
        {
            SpawnerStateController.Instance.ClearPatternAsync();
            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            //SetState(_prestartState);
        }
        
        [FoldoutGroup("Game State")]
        [Button("Restart" , ButtonSizes.Large), GUIColor(1, 1, 1)]
        private void DebugRestart()
        {
            RestartMap();
        }
        
        [FoldoutGroup("Game State")]
        [Button("PreStart" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void DebugPreStart()
        {
            SetState(_prestartState);
        }

        [FoldoutGroup("Game State")]
        [Button("Start Game" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void DebugStartGame()
        {
            SetState(_startState);
        }

        [FoldoutGroup("Game State")]
        [Button("End Game" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugEndGame()
        {
            gameResult = EndResult.Completed;
            SetState(_endState);
        }

        [FoldoutGroup("Game State")]
        [Button("Summary" , ButtonSizes.Large), GUIColor(0, 1, 1)]
        private void DebugSummary()
        {
            SetState(_summaryState);
        }
        
        [FoldoutGroup("Map Button"), Button(ButtonSizes.Large), GUIColor(0, 1, 1)]
        public void SetMap(int mapIndex)
        {
            if (MapDataList.Count == 0) return;
            if (mapIndex < 0 || mapIndex >= MapDataList.Count) return;

            currentMapIndex = mapIndex;
            MapSelectionSender.Instance.currentMapSelectionIndex = currentMapIndex;

            AssignMapRuntime(MapDataList[currentMapIndex]);
            SetState(_prestartState);
        }
        
        [FoldoutGroup("Map Button"), Button(ButtonSizes.Large), GUIColor(0, 1, 1)]
        public void NextMap()
        {
            if (MapDataList.Count == 0 || currentMapIndex >= MapDataList.Count - 1) return;
            currentMapIndex++;
            MapSelectionSender.Instance.currentMapSelectionIndex = currentMapIndex;
            AssignMapRuntime(MapDataList[currentMapIndex]);
            SetState(_prestartState);
        }
        
        private void OnDestroy()
        {
            if (_currentMapDataRuntime != null) Destroy(_currentMapDataRuntime);
        }
    }
}