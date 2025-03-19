using Characters.InputSystems.Interface;
using Characters.MovementSystem;
using UnityEngine;

namespace Characters.Controllers
{
    public abstract class CharacterController : MonoBehaviour
    {
        [SerializeField] private BaseMovementSystem movementSystem;
        private ICharacterInput _inputSystem;

        private void OnEnable()
        {
            if (_inputSystem == null && !TryGetComponent(out _inputSystem))
                Debug.Log("Error Input Reader is required, Please Add Input Reader Component");
            
            _inputSystem.OnMove += movementSystem.TryMove;
        }

        private void OnDisable()
        {
            _inputSystem.OnMove -= movementSystem.TryMove;
        }
    }
}
