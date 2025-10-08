using Characters.MovementSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.Controllers
{
    public class TwinController : EnemyController
    {
        [BoxGroup("Body Part")] [SerializeField] private Transform redBody;
        [BoxGroup("Body Part")] [SerializeField] private Transform blueBody;

        private Vector2 _redBodyLocalPosOnStart;
        private Vector2 _blueBodyLocalPosOnStart;
        public Transform RedBody => redBody;
        public Transform BlueBody => blueBody;
        public Vector2 RedBodyLocalPosOnStart => _redBodyLocalPosOnStart;
        public Vector2 BlueBodyLocalPosOnStart => _blueBodyLocalPosOnStart;

        private void Start()
        {
            _redBodyLocalPosOnStart = redBody.transform.localPosition;
            _blueBodyLocalPosOnStart = blueBody.transform.localPosition;
        }
    }
}
