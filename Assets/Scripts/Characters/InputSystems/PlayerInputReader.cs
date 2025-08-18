using System;
using Characters.InputSystems.Interface;
using Characters.SkillSystems;
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

        DirectionContainer ICharacterInput.SightDirection
        {
            get => _sightDirection;
            set => _sightDirection = value;
        }

        public bool Enable { get; set; } = true;

        /// <summary>
        /// Event triggered when the player moves.
        /// The Vector2 parameter represents the movement position.
        /// </summary>
        public Action<Vector2> OnMove { get; set; }

        public Action<SkillType> OnSkillPerform { get; set; }

        /// <summary>
        /// Instance of the player input system that handles all input actions.
        /// </summary>
        private PlayerInputAction _playerInputAction;

        /// <summary>
        /// Stores the current movement input direction.
        /// </summary>
        private Vector2 _mousePosition;

        private DirectionContainer _sightDirection;

        private bool _primaryToggle;
        private bool _secondaryToggle;
        private float _toggleTickTime = 0.3f;
        private float _lastTimeToggle;

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
            _primaryToggle = false;
            _secondaryToggle = false;
        }
        
        private void Update()
        {
            if (!Enable)
            {
                _primaryToggle = false;
                _secondaryToggle = false;
                return;
            }
            _mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mouseDirection = _mousePosition - (Vector2)transform.position;
            _sightDirection.direction = mouseDirection.normalized;
            _sightDirection.length = mouseDirection.magnitude;
            OnMove?.Invoke(_sightDirection.direction);
            
            if (Time.time - _lastTimeToggle < _toggleTickTime) return;
            _lastTimeToggle = Time.time;
            
            if (_primaryToggle)
                OnSkillPerform?.Invoke(SkillType.PrimarySkill);
            
            if (_secondaryToggle)
                OnSkillPerform?.Invoke(SkillType.SecondarySkill);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles movement input from the player. Converts screen position to world position.
        /// </summary>
        /// <param name="context">The input action context containing movement data.</param>
        public void OnMovement(InputAction.CallbackContext context)
        {
            //_mousePosition = Camera.main.ScreenToWorldPoint(context.ReadValue<Vector2>());
        }

        /// <summary>
        /// Handles primary skill usage input. Determines the direction from the player to the target
        /// and triggers the primary skill event.
        /// </summary>
        /// <param name="context">The input action context containing skill activation data.</param>
        public void OnUsePrimarySkill(InputAction.CallbackContext context)
        {
            if (!Enable) return;
            if (!context.performed)
            {
                _primaryToggle = false;
                return;
            }

            _primaryToggle = true;
            OnSkillPerform?.Invoke(SkillType.PrimarySkill);
        }

        /// <summary>
        /// Handles secondary skill usage input. Determines the direction from the player to the target
        /// and triggers the secondary skill event.
        /// </summary>
        /// <param name="context">The input action context containing skill activation data.</param>
        public void OnUseSecondarySkill(InputAction.CallbackContext context)
        {
            if (!Enable) return;
            if (!context.performed)
            {
                _secondaryToggle = false;
                return;
            }

            _secondaryToggle = true;
            OnSkillPerform?.Invoke(SkillType.SecondarySkill);
        }

        #endregion
    }
}
