using System.Threading;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillPressureRuntime : BaseSkillRuntime<SkillPressureDataSo>
    {
        protected override void OnSkillStart()
        {
            owner.MovementSystem.StopAllMovementAndTween();
            owner.TryPlayFeedback(FeedbackName.Pressure);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask.WaitForSeconds(skillData.ChargeDuration, cancellationToken: cancelToken);

            var layerMask = CharacterGlobalSettings.Instance.EnemyLayerDictionary[transform.tag];
            var targets = Physics2D.OverlapCircleAll(transform.position, skillData.ExplosionRadius, layerMask);

            foreach (var target in targets)
            {
                CombatManager.ApplyCalculatedDamageTo(target.gameObject, owner.gameObject,
                    target.ClosestPoint(owner.transform.position), skillData.BaseDamage, skillData.DamageMultiplier, 0,
                    0, 0, 0);
            }
        }

        protected override void OnSkillExit()
        {
            owner.MovementSystem.ResetMovementSystem();
            CombatManager.ApplyRawDamageTo(owner.gameObject, owner.HealthSystem.MaxHealth);
        }
    }
}