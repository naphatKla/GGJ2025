using GameControl;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class EndState : IGameState
    {
        public void Enter(GameStateController controller) { }

        public void Update(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            PoolingSystem.Instance.ClearAll();
            Timer.Instance.StopTimer();
        }

        public void Exit(GameStateController controller) { }
    }
}