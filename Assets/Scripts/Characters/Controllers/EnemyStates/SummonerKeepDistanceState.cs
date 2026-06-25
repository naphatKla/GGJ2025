using Characters.InputSystems.Interface;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;
using UnityEngine;

namespace Characters.Controllers.EnemyStates
{
    public class SummonerKeepDistanceState : BaseEnemyState
    {
        private SummonerKeepDistanceStateDataSo _data;
        private float _fleeSqr;
        private float _preferredMinSqr;
        private float _preferredMaxSqr;
        private float _detectSqr;

        public override void AssignData(EnemyController ownerEnemy, BaseEnemyStateDataSo data)
        {
            base.AssignData(ownerEnemy, data);
            _data = data as SummonerKeepDistanceStateDataSo;
            _fleeSqr = _data.FleeDistance * _data.FleeDistance;
            _preferredMinSqr = _data.PreferredMinDistance * _data.PreferredMinDistance;
            _preferredMaxSqr = _data.PreferredMaxDistance * _data.PreferredMaxDistance;
            _detectSqr = _data.DetectDistance * _data.DetectDistance;
        }

        protected override void OnEnterState()
        {
        }

        protected override void OnUpdateState()
        {
            var player = PlayerController.Instance;
            if (!player) return;

            Vector2 selfPos = ownerEnemyController.transform.position;
            Vector2 toPlayer = (Vector2)player.transform.position - selfPos;
            float distanceSqr = toPlayer.sqrMagnitude;

            Vector2 toPlayerDirection = distanceSqr > 0.0001f ? toPlayer.normalized : Vector2.zero;
            enemyInputReader.SetSightDirection(new DirectionContainer
            {
                direction = toPlayerDirection,
                length = Mathf.Sqrt(distanceSqr)
            });

            Vector2 moveDirection = Vector2.zero;
            if (distanceSqr < _fleeSqr)
            {
                moveDirection = -toPlayerDirection;
            }
            else if (_data.RepositionWhenTooFar && distanceSqr > _preferredMaxSqr)
            {
                moveDirection = toPlayerDirection;
            }
            else if (_data.StrafeWhileInRange && distanceSqr >= _preferredMinSqr && distanceSqr <= _preferredMaxSqr)
            {
                moveDirection = Vector2.Perpendicular(toPlayerDirection) * _data.StrafeDirection;
            }

            ownerEnemyController.MovementSystem.StopFromInput(moveDirection == Vector2.zero);
            enemyInputReader.SetSightDirection(new DirectionContainer
            {
                direction = moveDirection == Vector2.zero ? toPlayerDirection : moveDirection.normalized,
                length = Mathf.Sqrt(distanceSqr)
            });
            enemyInputReader.MoveInput();

            if (distanceSqr <= _detectSqr)
                ownerEnemyController.SkillSystem.PerformSkill(SkillType.PrimarySkill);
        }

        protected override void OnExitState()
        {
            ownerEnemyController.MovementSystem.StopFromInput(false);
        }
    }
}
