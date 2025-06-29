using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class StartState : IGameState
    {
        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.SpawningState());
            GameTimer.Instance.StartTimer();
            GameTimer.Instance.OnTimerEnded += HandleTimerEnded;
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
        
        private void HandleTimerEnded()
        {
            GameStateController.Instance.SetState(new EndState());
        }
    }
}