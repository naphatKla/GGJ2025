using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class StartState : IGameState
    {
        public void OnEnable(GameStateController controller)
        {
            GameTimer.Instance.OnTimerEnded += HandleTimerEnded;
        }

        public void OnDisable(GameStateController controller)
        {
            GameTimer.Instance.OnTimerEnded -= HandleTimerEnded;
        }

        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.SpawningState());
            GameTimer.Instance.StartTimer();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller)
        {
            GameTimer.Instance.OnTimerEnded -= HandleTimerEnded;
        }
        
        private void HandleTimerEnded()
        {
            GameStateController.Instance.SetState(new EndState());
        }
    }
}