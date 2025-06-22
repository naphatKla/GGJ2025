using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl.Interface
{
    public interface IGameState
    {
        void Enter(GameStateController controller);
        void Update(GameStateController controller);
        void Exit(GameStateController controller);
    }   
}
