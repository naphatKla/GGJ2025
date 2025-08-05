using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class PrestartState : IGameState
    {
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller)
        {
            PlayerController.Instance.HealthSystem.OnDead -= PlayerDeathResult;
        }

        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.StopState());
            GameTimer.Instance.SetTimer(controller.CurrentMap.mapGlobalTime);
            GameTimer.Instance.StopTimer();
            SpawnerStateController.Instance.ClearEnemy();
            SpawnerStateController.Instance.ClearItem();
            SpawnerStateController.Instance.SetupMapAndEnemy().Forget();
            controller.gameResult = EndResult.None;
            
            if (Time.timeScale <= 0f)
                controller.LerpTimeScaleAsync(1f, 1f).Forget();
            else
                Time.timeScale = 1f;
            
            CountdownStart().Forget();
            if (!PlayerController.Instance.gameObject.activeInHierarchy) PlayerController.Instance.gameObject.SetActive(true);
            PlayerController.Instance.ResetAllDependentBehavior();
            PlayerController.Instance.HealthSystem.OnDead += PlayerDeathResult;
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }

        private async UniTaskVoid CountdownStart()
        {
            await GameTimer.Instance.StartCountdownAsync(5f);
            GameStateController.Instance.SetState(new StartState());
        }

        private void PlayerDeathResult()
        {
            GameStateController.Instance.gameResult = EndResult.Failed;
            PlayerController.Instance.HealthSystem.OnDead -= PlayerDeathResult;
            GameStateController.Instance.SetState(new EndState());
        }
    }
}