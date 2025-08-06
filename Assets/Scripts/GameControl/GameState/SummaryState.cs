using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class SummaryState : IGameState
    {
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            Time.timeScale = 0.001f;
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenPanel(UIPanelType.MapResult);
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}