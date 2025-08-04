using System;
using System.Threading;
using Characters.FeedbackSystems;
using Characters.MovementSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillParryRuntime : BaseSkillRuntime<SkillParryDataSo>
    {
        private bool _isParryTrigger;
        
        protected override void OnSkillStart()
        {
            _isParryTrigger = false;
            owner.HealthSystem.OnTakeDamage += TriggerParry;
            owner.MovementSystem.StopFromParry(skillData.StopWhileParry);
            owner.TryPlayFeedback(FeedbackName.ParryUSe);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask.WaitUntil(() => _isParryTrigger, cancellationToken: cancelToken)
                .TimeoutWithoutException(TimeSpan.FromSeconds(skillData.ParryDuration));
            
            if (!_isParryTrigger) return;
            owner.TryPlayFeedback(FeedbackName.ParrySuccess);
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            var targetsInRange = Physics2D.OverlapCircleAll(owner.transform.position, skillData.ExplosionRadius, damageLayer);
            
            foreach (var target in targetsInRange)
            {
                StatusEffectManager.ApplyEffectTo(target.gameObject, skillData.ExplosionEffects);
                Vector2 knockBackDirection = target.transform.position - owner.transform.position;
                Vector2 knockBackDestination = (Vector2)target.transform.position + (knockBackDirection.normalized * skillData.KnockBackDistance);

                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, skillData.KnockBackDuration);
                CombatManager.ApplyDamageTo(target.gameObject, owner.gameObject, skillData.ExplosionDamageMultiplier);
            }
        }

        protected override void OnSkillExit()
        {
            _isParryTrigger = false;
            owner.MovementSystem.StopFromParry(false);
            owner.HealthSystem.OnTakeDamage -= TriggerParry;
        }

        private void TriggerParry()
        {
            _isParryTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!owner) return;
            Gizmos.DrawSphere(owner.gameObject.transform.position, skillData.ExplosionRadius);
        }
    }
}
