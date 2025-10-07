using System;
using Characters.InputSystems.Interface;
using Characters.MovementSystems;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO;
using Manager;
using UnityEngine;

namespace Characters.InputSystems
{
    public class EnemyInputReader : MonoBehaviour, ICharacterInput
    {
        private EnemyDataSo _enemy;
        private BaseMovementSystem _move;

        private DirectionContainer _sight;
        DirectionContainer ICharacterInput.SightDirection { get => _sight; set => _sight = value; }

        public bool Enable { get; set; } = true;
        public Action<Vector2> OnMove { get; set; }
        public Action<SkillType> OnSkillPerform { get; set; }

        private float _stopSqr;
        private float _performSqr;

        private void OnEnable()
        {
            _move = GetComponent<BaseMovementSystem>();
            var ctrl = GetComponent<Characters.Controllers.EnemyController>();
            _enemy = ctrl && ctrl.CharacterData is EnemyDataSo e ? e : null;

            if (_enemy == null) { enabled = false; return; }

            _stopSqr = _enemy.StopDistance * _enemy.StopDistance;
            _performSqr = _enemy.PerformSkillDistance * _enemy.PerformSkillDistance;

            FixedUpdateManager.Instance.OnTick += HandleTick;   // subscribe tick 0.2s
        }

        private void OnDisable()
        {
            if (!FixedUpdateManager.IsAlive) return;
            FixedUpdateManager.Current.OnTick -= HandleTick;
        }

        private void HandleTick()
        {
            if (!Enable) return;
            var player = Characters.Controllers.PlayerController.Instance;
            if (!player) return;

            Vector2 pos = transform.position;
            Vector2 toP = (Vector2)player.transform.position - pos;
            float d2 = toP.sqrMagnitude;

            if (d2 > 1e-6f)
            {
                float inv = 1.0f / Mathf.Sqrt(d2);
                _sight.direction = toP * inv;
                _sight.length = 1.0f / inv;
            }
            else
            {
                _sight.direction = Vector2.zero;
                _sight.length = 0f;
            }

            bool stop = d2 < _stopSqr;
            _move?.StopFromInput(stop);
            OnMove?.Invoke(_sight.direction);

            if (d2 < _performSqr)
            {
                OnSkillPerform?.Invoke(SkillType.PrimarySkill);
                OnSkillPerform?.Invoke(SkillType.SecondarySkill);
            }
        }
    }
}
