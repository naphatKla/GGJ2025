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
            Timer.Instance.StartTimer();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}