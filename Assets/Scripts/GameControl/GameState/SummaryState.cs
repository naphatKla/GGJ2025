using GameControl;
using GameControl.Controller;
using GameControl.Interface;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace GameControl.GameState
{
    public class SummaryState : IGameState
    {
        public void OnEnable(GameStateController controller) { }

        public void OnDisable(GameStateController controller) { }

        public void Enter(GameStateController controller)
        {
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenPanel(UIPanelType.MapResult);
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0, -1, true, 6.2f, true);
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}