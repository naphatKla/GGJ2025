using System;
using Characters.InputSystems.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.InputSystems
{
    public class PlayerInputReader : MonoBehaviour, PlayerInputAction.IGameplayActions, ICharacterInput
    {
        public Action<Vector2> OnMove { get; set; }
        public Action OnPrimarySkillPerform { get; set; }
        public Action OnSecondarySKillPerform { get; set; }
        
        private PlayerInputAction _playerInputAction;
        private Vector2 _movePosition;
        
        private void OnEnable()
        {
            if (_playerInputAction == null)
            {
                _playerInputAction = new PlayerInputAction();
                _playerInputAction.Gameplay.SetCallbacks(this);
            }
        
            _playerInputAction.Gameplay.Enable();
        }
    
        private void OnDisable()
        {
            _playerInputAction.Gameplay.Disable();
        }

        private void FixedUpdate()
        {
            OnMove?.Invoke(_movePosition);
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            _movePosition = Camera.main.ScreenToWorldPoint(context.ReadValue<Vector2>());
        }

        public void OnUsePrimarySkill(InputAction.CallbackContext context)
        {
            OnPrimarySkillPerform?.Invoke();
        }

        public void OnUseSecondarySkill(InputAction.CallbackContext context)
        {
            OnSecondarySKillPerform?.Invoke();
        }
    }
}
