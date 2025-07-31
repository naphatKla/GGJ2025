using System;
using System.Collections.Generic;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.GameState;
using GameControl.Interface;
using MoreMountains.Tools;
using UnityEngine;
using Sirenix.OdinInspector;

namespace GameControl.Controller
{
    public enum EndResult
    {
        None,
        Completed,
        Failed,
    }
    
    [RequireComponent(typeof(SpawnerStateController), typeof(GameTimer), typeof(PoolingSystem))]
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
        
        public EndResult gameResult;
        public IGameState CurrentState => _currentState;
        
        private void Awake()
        {
            _prestartState = new PrestartState();
            _startState = new StartState();
            _endState = new EndState();
            _summaryState = new SummaryState();
            _currentMapData = CurrentMap;
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
        
        public async UniTask LerpTimeScaleAsync(float target, float duration)
        {
            float start = Time.timeScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(start, target, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            Time.timeScale = target;
        }

        public void RestartMap()
        {
            SetState(_prestartState);
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