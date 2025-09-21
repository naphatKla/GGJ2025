using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.Controller;
using GameControl.Interface;
using Manager.SoundManager;
using UI;

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
            SpawnerStateController.Instance.SetupMapAndEnemy(true).Forget();
            controller.gameResult = EndResult.None;
            
            if (!PlayerController.Instance.gameObject.activeInHierarchy) PlayerController.Instance.gameObject.SetActive(true);
            PlayerController.Instance.ResetAllDependentBehavior();
            PlayerController.Instance.HealthSystem.OnDeadAnimationFinish += PlayerDeathResult;
            
            CountdownStart().Forget();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }

        private void PlayerDeathResult()
        {
            GameStateController.Instance.gameResult = EndResult.Failed;
            PlayerController.Instance.HealthSystem.OnDead -= PlayerDeathResult;
            GameStateController.Instance.SetState(new EndState());
        }
        
        private async UniTaskVoid CountdownStart()
        {
            SoundManager.Instance.PlayUI(SoundName.UI.Gameplay_CountDown5Sec, timeScaleMode: SoundManager.TimeScaleMode.ScalePitch);
            await GameTimer.Instance.StartCountdownAsync(5f);
            GameStateController.Instance.SetState(new StartState());
        }
    }
}