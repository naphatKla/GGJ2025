using GameControl.Controller;
using GameControl.Interface;

namespace GameControl.SpawnerState
{
    public class PauseState : ISpawnerState
    {
        public void Enter(SpawnerStateController controller)
        {
            Timer.Instance.PauseTimer();
        }

        public void Update(SpawnerStateController controller) { }

        public void Exit(SpawnerStateController controller) { }
    }
}
