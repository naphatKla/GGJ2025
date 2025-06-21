using GameControl;
using GameControl.Interface;
using UnityEngine;

namespace GameControl.GameState
{
    public class StartState : IGameState
    {
        public void Enter(GameStateController controller)
        {
            SpawnerStateController.Instance.SetState(new SpawnerState.SpawningState());
        }

        public void Update(GameStateController controller) { }

        public void Exit(GameStateController controller) { }
    }
}