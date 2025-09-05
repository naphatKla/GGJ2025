using Characters.Controllers;
using GameControl.Controller;
using GameControl.Interface;
using MoreMountains.Feedbacks;
using UI;

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
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}