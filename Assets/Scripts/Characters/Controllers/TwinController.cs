using Characters.MovementSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.Controllers
{
    public class TwinController : EnemyController
    {
        [BoxGroup("Body Part")] [SerializeField] private TransformMovementSystem redBody;
        [BoxGroup("Body Part")] [SerializeField] private TransformMovementSystem blueBody;

        public TransformMovementSystem RedBody => redBody;
        public TransformMovementSystem BlueBody => blueBody;
    }
}
