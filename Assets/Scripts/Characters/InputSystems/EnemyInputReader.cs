using System;
using System.Collections.Generic;
using Characters.Controllers;
using Characters.Controllers.EnemyStates;
using Characters.InputSystems.Interface;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;
using Manager;
using UnityEngine;

namespace Characters.InputSystems
{
    public class EnemyInputReader : MonoBehaviour, ICharacterInput
    {
        private EnemyController _ownerEnemy;
        private EnemyDataSo _enemyData;
        private DirectionContainer _sight;
        private BaseEnemyStateDataSo _defaultStateData;
        private BaseEnemyStateDataSo _currentStateData;
        private Queue<EnemyStateDataPayload> _stateQueue = new();
        private BaseEnemyState _currentState;

        DirectionContainer ICharacterInput.SightDirection
        {
            get => _sight;
            set => _sight = value;
        }
        
        public bool Enable { get; set; } = true;
        public Action<Vector2> OnMove { get; set; }
        public Action<SkillType> OnSkillPerform { get; set; }

        private void OnEnable()
        {
            FixedUpdateManager.Instance.OnTick += OnTick;
        }

        private void OnDisable()
        {
            if (!FixedUpdateManager.Current) return;
            FixedUpdateManager.Current.OnTick -= OnTick;
        }

        public virtual void AssignData(EnemyController owner)
        {
            _ownerEnemy = owner;

            if (owner.CharacterData is not EnemyDataSo enemyDataSo)
            {
                Debug.LogWarning("Enemy Data Was Wrong Type!");
                return;
            }

            _enemyData = enemyDataSo;
            _defaultStateData = _enemyData.DefaultState;

            ResetInputSystem();
        }
        
        private void OnTick()
        {
            if (!Enable) return;
            _currentState?.HandleOnUpdate();
        }
        
        public void UpdateStateOnHealthChanged(float changedValue)
        {
            if (!Enable) return;
            if (_stateQueue.Count <= 0) return;
            if (_ownerEnemy.HealthSystem.HealthPercentage01 * 100 > _stateQueue.Peek().HpPercentageToEnter) return;
            ChangeState(_stateQueue.Dequeue().StateData);
        }

        protected virtual void ChangeState(BaseEnemyStateDataSo stateData)
        {
            if (!Enable) return;
            if (_currentStateData && _currentStateData == stateData) return;
            
            var type = stateData.SkillRuntime;
            var newState = (BaseEnemyState)Activator.CreateInstance(type);

            if (newState == null)
            {
                Debug.LogWarning("State instance was null");
                return;
            }

            _currentState?.HandleOnExit();
            newState.AssignData(_ownerEnemy, stateData);
            newState.HandleOnStart();
            _currentState = newState;
            _currentStateData = stateData;
        }
        
        public void SetSightDirection(DirectionContainer directionContainer)
        {
            _sight = directionContainer;
        }

        public void MoveInput()
        {
            OnMove?.Invoke(_sight.direction);
        }

        public void PerformSkill(SkillType type)
        {
            OnSkillPerform?.Invoke(SkillType.PrimarySkill);
            OnSkillPerform?.Invoke(SkillType.SecondarySkill);
        }
        
        public void ResetInputSystem()
        {
            _currentState?.HandleOnExit();
            _stateQueue = new Queue<EnemyStateDataPayload>(_enemyData.StateList);
            ChangeState(_defaultStateData);
        }
    }
}