using System.Threading;
using Characters.InputSystems.Interface;
using Characters.SkillSystems;
using Characters.SO.CharacterDataSO.EnemyStateDataSO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.Controllers.EnemyStates
{
    public class ChasingPlayerState : BaseEnemyState
    {
        private ChasingPlayerStateDataSo _data;
        private float _stopSqr;
        private float _performSqr;

        public override void AssignData(EnemyController ownerEnemy, BaseEnemyStateDataSo data)
        {
            base.AssignData(ownerEnemy, data);
            _data = data as ChasingPlayerStateDataSo;
            _stopSqr = _data.StopDistance * _data.StopDistance;
            _performSqr = _data.PerformSkillDistance * _data.PerformSkillDistance;
        }

        protected override void OnEnterState()
        {
           
        }

        protected override async UniTask OnUpdateState(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var player = PlayerController.Instance;
                if (!player)
                {
                    await UniTask.WaitForSeconds(0.2f, cancellationToken: cancellationToken);
                    continue;
                }

                Vector2 pos = ownerEnemyController.transform.position;
                Vector2 toP = (Vector2)player.transform.position - pos;
                float d2 = toP.sqrMagnitude;
                DirectionContainer sight = new DirectionContainer();

                if (d2 > 1e-6f)
                {
                    float inv = 1.0f / Mathf.Sqrt(d2);
                    sight.direction = toP * inv;
                    sight.length = 1.0f / inv;
                }
                else
                {
                    sight.direction = Vector2.zero;
                    sight.length = 0f;
                }

                enemyInputReader.SetSightDirection(sight);

                bool stop = d2 < _stopSqr;
                ownerEnemyController.MovementSystem.StopFromInput(stop);
                enemyInputReader.MoveInput();

                if (d2 < _performSqr)
                {
                    enemyInputReader.PerformSkill(SkillType.PrimarySkill);
                    enemyInputReader.PerformSkill(SkillType.SecondarySkill);
                }

                await UniTask.WaitForSeconds(0.2f, cancellationToken: cancellationToken);
            }
        }

        protected override void OnExitState()
        {
        }
    }
}