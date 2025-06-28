using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class PrestartState : IGameState
    {
        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            PoolingSystem.Instance.ClearAll();
            Timer.Instance.SetTimer(600);
            SpawnerStateController.Instance.SetupMapAndEnemy().Forget();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}