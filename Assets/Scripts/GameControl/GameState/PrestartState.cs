using Cysharp.Threading.Tasks;
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
            GameTimer.Instance.SetTimer(600);
            SpawnerStateController.Instance.SetupMapAndEnemy().Forget();
            
            StartStage().Forget();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }

        private async UniTaskVoid StartStage()
        {
            await GameTimer.Instance.StartCountdownAsync(5f);
            GameStateController.Instance.SetState(new StartState());
        }
    }
}