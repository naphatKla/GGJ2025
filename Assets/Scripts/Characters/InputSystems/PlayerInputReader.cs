using System;
using Characters.InputSystems.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.InputSystems
{
    public class PlayerInputReader : MonoBehaviour, PlayerInputAction.IGameplayActions, ICharacterInput
    {
        #region Inspectors & Variables
        public Action<Vector2> OnMove { get; set; }
        public Action<Vector2> OnPrimarySkillPerform { get; set; }
        public Action<Vector2> OnSecondarySKillPerform { get; set; }

        private PlayerInputAction _playerInputAction;
        private Vector2 _movePosition;
        #endregion

        #region Unity Methods
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
        #endregion

        #region Methods
        public void OnMovement(InputAction.CallbackContext context)
        {
            _movePosition = Camera.main.ScreenToWorldPoint(context.ReadValue<Vector2>());
        }

        public void OnUsePrimarySkill(InputAction.CallbackContext context)
        {
            OnPrimarySkillPerform?.Invoke(_movePosition);
        }

        public void OnUseSecondarySkill(InputAction.CallbackContext context)
        {
            OnSecondarySKillPerform?.Invoke(_movePosition);
        }
        #endregion
    }
}
