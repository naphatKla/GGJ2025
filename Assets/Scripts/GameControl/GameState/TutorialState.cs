using Cysharp.Threading.Tasks;
using GameControl.Controller;
using GameControl.Interface;
using UI;

namespace GameControl.GameState
{
    public class TutorialState : IGameState
    {
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            UIManager.Instance.OpenTutorialPanel();
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }

}