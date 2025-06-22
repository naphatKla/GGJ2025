using GameControl.Interface;

namespace GameControl.SpawnerState
{
    public class SpawningState : ISpawnerState
    {
        public void Enter(SpawnerStateController controller)
        {
            Timer.Instance.ResumeTimer();
        }

        public void Update(SpawnerStateController controller) { }

        public void Exit(SpawnerStateController controller) { }
    }
}
