using Characters.Controllers;
using GameControl.Controller;
using GameControl.Interface;
using MoreMountains.Feedbacks;

namespace GameControl.GameState
{
    public class SummaryState : IGameState
    {
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenResultMenu();
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0, -1, true, 6.2f, true);
            PlayerController.Instance.GetSummaryStatsOnStateEnd();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}