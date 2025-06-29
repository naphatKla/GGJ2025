using GameControl.Controller;

namespace GameControl.Interface
{
    public interface IGameState
    {
        void OnEnable(GameStateController controller);
        void OnDisable(GameStateController controller);
        void Enter(GameStateController controller);
        void Update(GameStateController controller);
        void Exit(GameStateController controller);
    }   
}
