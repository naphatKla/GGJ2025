using System;
using Characters.MovementSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.Controllers
{
    public class TwinController : EnemyController
    {
        [BoxGroup("Body Part")] [SerializeField] private TransformMovementSystem redBody;
        [BoxGroup("Body Part")] [SerializeField] private TransformMovementSystem blueBody;

        private Vector2 _redBodyLocalPosOnStart;
        private Vector2 _blueBodyLocalPosOnStart;
        public TransformMovementSystem RedBody => redBody;
        public TransformMovementSystem BlueBody => blueBody;
        public Vector2 RedBodyLocalPosOnStart => _redBodyLocalPosOnStart;
        public Vector2 BlueBodyLocalPosOnStart => _blueBodyLocalPosOnStart;

        private void Start()
        {
            _redBodyLocalPosOnStart = redBody.transform.localPosition;
            _blueBodyLocalPosOnStart = blueBody.transform.localPosition;
        }
    }
}
