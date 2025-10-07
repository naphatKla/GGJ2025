using Unity.VisualScripting;
using UnityEngine;

namespace Characters.Controllers.EnemyStates
{
    public abstract class BaseEnemyState
    {
        public abstract void OnEnterState();
        public abstract void OnTickState(); // 0.2 tick time
        public abstract void OnExitState();
    }
}
