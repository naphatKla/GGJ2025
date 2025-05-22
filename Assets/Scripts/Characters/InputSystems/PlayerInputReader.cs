using System;
using Characters.InputSystems.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.InputSystems
{
    /// <summary>
    /// Handles player input and translates it into movement and skill actions.
    /// Implements both `PlayerInputAction.IGameplayActions` and `ICharacterInput`
    /// to integrate with the input system and character movement.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour, PlayerInputAction.IGameplayActions, ICharacterInput
    {
        #region Inspectors & Variables

        /// <summary>
        /// Event triggered when the player moves.
        /// The Vector2 parameter represents the movement position.
        /// </summary>
        public Action<Vector2> OnMove { get; set; }

        /// <summary>
        /// Event triggered when the player performs the primary skill.
        /// The Vector2 parameter represents the target direction.
        /// </summary>
        public Action<Vector2> OnPrimarySkillPerform { get; set; }

        /// <summary>
        /// Event triggered when the player performs the secondary skill.
        /// The Vector2 parameter represents the target direction.
        /// </summary>
        public Action<Vector2> OnSecondarySkillPerform { get; set; }

        /// <summary>
        /// Instance of the player input system that handles all input actions.
        /// </summary>
        private PlayerInputAction _playerInputAction;

        /// <summary>
        /// Stores the current movement input direction.
        /// </summary>
        private Vector2 _mousePosition;

        /// <summary>
        /// Stores the aiming or sight direction based on player input.
        /// </summary>
        private Vector2 _sightDirection;

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
            Vector2 moveDirection = _mousePosition - (Vector2)transform.position;
            OnMove?.Invoke(moveDirection.normalized);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles movement input from the player. Converts screen position to world position.
        /// </summary>
        /// <param name="context">The input action context containing movement data.</param>
        public void OnMovement(InputAction.CallbackContext context)
        {
            _mousePosition = Camera.main.ScreenToWorldPoint(context.ReadValue<Vector2>());
        }

        /// <summary>
        /// Handles primary skill usage input. Determines the direction from the player to the target
        /// and triggers the primary skill event.
        /// </summary>
        /// <param name="context">The input action context containing skill activation data.</param>
        public void OnUsePrimarySkill(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            _sightDirection = (_mousePosition - (Vector2)transform.position).normalized;
            OnPrimarySkillPerform?.Invoke(_sightDirection);
        }

        /// <summary>
        /// Handles secondary skill usage input. Determines the direction from the player to the target
        /// and triggers the secondary skill event.
        /// </summary>
        /// <param name="context">The input action context containing skill activation data.</param>
        public void OnUseSecondarySkill(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            _sightDirection = (_mousePosition - (Vector2)transform.position).normalized;
            OnSecondarySkillPerform?.Invoke(_sightDirection);
        }

        #endregion
    }
}
