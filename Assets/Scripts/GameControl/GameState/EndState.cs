using Cysharp.Threading.Tasks;
using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace GameControl.GameState
{
    public class EndState : IGameState
    {
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            SpawnerStateController.Instance.ClearEnemy();
            SpawnerStateController.Instance.ClearItem();
            GameTimer.Instance.StopTimer();
            GameTimer.Instance.ClearAllTriggers();
            WaitBeforeSummary().Forget();
        }

        public void Update(GameStateController controller)
        { }

        public void Exit(GameStateController controller) { }
        
        private async UniTaskVoid WaitBeforeSummary()
        {
            await UniTask.WaitForSeconds(0.5f);
            GameStateController.Instance.SetState(new SummaryState());
        }
    }
}