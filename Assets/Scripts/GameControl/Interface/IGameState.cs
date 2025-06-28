using GameControl.Controller;

namespace GameControl.Interface
{
    public interface IGameState
    {
        void Enter(GameStateController controller);
        void Update(GameStateController controller);
        void Exit(GameStateController controller);
    }   
}
