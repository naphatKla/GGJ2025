using GameControl.GameState;
using GameControl.Interface;
using MoreMountains.Tools;
using UnityEngine;
using Sirenix.OdinInspector;

namespace GameControl
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
        
        private void Awake()
        {
            _prestartState = new PrestartState();
            _startState = new StartState();
            _endState = new EndState();
            _summaryState = new SummaryState();
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
        
        [Button("PreStart" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void DebugPreStart()
        {
            SetState(_prestartState);
        }

        [Button("Start Game" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void DebugStartGame()
        {
            SetState(_startState);
        }

        [Button("End Game" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugEndGame()
        {
            SetState(_endState);
        }

        [Button("Summary" , ButtonSizes.Large), GUIColor(0, 1, 1)]
        private void DebugSummary()
        {
            SetState(_summaryState);
        }
    }
}