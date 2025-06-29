using Cysharp.Threading.Tasks;
using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class EndState : IGameState
    {
        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            PoolingSystem.Instance.ClearAll();
            GameTimer.Instance.StopTimer();
            WaitBeforeSummary().Forget();
        }

        public void Update(GameStateController controller)
        { }

        public void Exit(GameStateController controller) { }
        
        private async UniTaskVoid WaitBeforeSummary()
        {
            await UniTask.WaitForSeconds(5f);
            GameStateController.Instance.SetState(new SummaryState());
        }
    }
}