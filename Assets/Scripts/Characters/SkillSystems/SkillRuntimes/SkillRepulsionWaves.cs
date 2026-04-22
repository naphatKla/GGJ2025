using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillRepulsionWaveRuntime : BaseSkillRuntime<SkillRepulsionWaveDataSo>
    {
        protected override void OnSkillStart()
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            var targetsInRange = Physics2D.OverlapCircleAll(
                owner.transform.position,
                skillData.ExplosionRadius,
                damageLayer
            );
            
            // ระเบิดศัตรูรอบ ๆ + knockback + ดาเมจ
            foreach (var target in targetsInRange)
            {
                Vector2 knockBackDirection = target.transform.position - owner.transform.position;
                Vector2 knockBackDestination =
                    (Vector2)target.transform.position +
                    knockBackDirection.normalized * skillData.KnockBackDistance;

                CombatManager.TryGetCharacterFromCache(target.gameObject, out var targetController);
                targetController.MovementSystem.TryMoveToPositionOverTime(knockBackDestination, skillData.KnockBackDuration);

                CombatManager.ApplyCalculatedDamageTo(
                    target.gameObject,
                    owner.gameObject,
                    owner.CharacterData.CharacterId,
                    owner.gameObject,
                    target.ClosestPoint(owner.transform.position),
                    skillData.ExplosionBaseDamage,
                    skillData.ExplosionDamageMultiplier,
                    0, 0, 0, 0
                );
            }
        }

        protected override UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnSkillExit()
        {
            
        }
    }
}
