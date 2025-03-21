using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
using UnityEngine;

namespace Characters.Controllers
{
    public abstract class BaseController : MonoBehaviour
    {
        #region Inspectors & Fields

        public BaseMovementSystem movementSystem;
        [SerializeField] private SkillSystem skillSystem;
        private ICharacterInput _inputSystem;

        #endregion

        #region Unity Methods

        private void Start()
        {
            skillSystem.Initialize(this);
        }

        private void OnEnable()
        {
            if (_inputSystem == null && !TryGetComponent(out _inputSystem))
                Debug.Log("Error Input Reader is required, Please Add Input Reader Component");

            ToggleMovementInputController(true);
            ToggleSkillInputController(true);
        }

        private void OnDisable()
        {
            ToggleMovementInputController(false);
            ToggleSkillInputController(false);
        }

        #endregion

        #region Methods
        
        public void ToggleMovementInputController(bool isToggle)
        {
            if (isToggle) _inputSystem.OnMove += movementSystem.TryMove;
            else _inputSystem.OnMove -= movementSystem.TryMove;
        }

        public void ToggleSkillInputController(bool isToggle)
        {
            if (isToggle)
            {
                _inputSystem.OnPrimarySkillPerform += skillSystem.PerformPrimarySkill;
                _inputSystem.OnSecondarySkillPerform += skillSystem.PerformSecondarySkill;
            }
            else 
            {
                _inputSystem.OnPrimarySkillPerform -= skillSystem.PerformPrimarySkill;
                _inputSystem.OnSecondarySkillPerform -= skillSystem.PerformSecondarySkill;
            }
        }
        
        #endregion
    }
}