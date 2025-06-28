using System.Collections.Generic;
using GameControl.GameState;
using GameControl.Interface;
using MoreMountains.Tools;
using UnityEngine;
using Sirenix.OdinInspector;

namespace GameControl.Controller
{
    public class GameStateController : MMSingleton<GameStateController>
    {
        private IGameState _currentState;
        
        private PrestartState _prestartState;
        private StartState _startState;
        private EndState _endState;
        private SummaryState _summaryState;

        [ShowInInspector, ReadOnly]
        private string _currentStateName;
        
        [ShowInInspector, ReadOnly]
        private SO.MapDataSO _currentMapData;

        [SerializeField] private List<SO.MapDataSO> mapDataList;
        [Tooltip("The index of the current map in the mapData list.")]
        [SerializeField] private int currentMapIndex;
        
        public SO.MapDataSO CurrentMap => 
            mapDataList.Count > 0 && currentMapIndex < mapDataList.Count
                ? mapDataList[currentMapIndex]
                : null;
        
        private void Awake()
        {
            _prestartState = new PrestartState();
            _startState = new StartState();
            _endState = new EndState();
            _summaryState = new SummaryState();
            _currentMapData = CurrentMap;
        }

        
        private void Start()
        {
            SetState(_prestartState);
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
            if (mapIndex < 0 || mapIndex >= mapDataList.Count) return;
            currentMapIndex = mapIndex;
            SetState(_prestartState);
        }
        
        [FoldoutGroup("Map Button"), Button(ButtonSizes.Large), GUIColor(0, 1, 1)]
        public void NextMap()
        {
            if (mapDataList.Count == 0 || currentMapIndex >= mapDataList.Count - 1) return;
            currentMapIndex++;
            SetState(_prestartState);
        }
    }
}