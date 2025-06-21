using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl.Interface
{
    public interface ISpawnerState
    {
        void Enter(SpawnerStateController controller);
        void Update(SpawnerStateController controller);
        void Exit(SpawnerStateController controller);
    }   
}