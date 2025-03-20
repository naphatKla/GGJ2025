using System;
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
            
            EnableMovementInputController(true);
            EnableSkillInputController(true);
        }

        private void OnDisable()
        {
            EnableMovementInputController(false);
            EnableSkillInputController(false);
        }
        #endregion

        public void EnableMovementInputController(bool isEnable)
        {
            if (isEnable) _inputSystem.OnMove += movementSystem.TryMove;
            else _inputSystem.OnMove -= movementSystem.TryMove;
        }

        public void EnableSkillInputController(bool isEnable)
        {
            if (isEnable)  _inputSystem.OnPrimarySkillPerform += skillSystem.PerformPrimarySkill;
            else _inputSystem.OnPrimarySkillPerform -= skillSystem.PerformPrimarySkill;
        }
    }
}
