using System;
using System.Threading;
using Characters.Controllers;
using Characters.MovementSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillParryRuntime : BaseSkillRuntime<SkillParryDataSo>, IAutoSkillTriggerSource
    {
        public event Action OnTriggerAutoSkill;
        private bool _isParryTrigger;
        private int _damageNegate;
        private Collider2D ownerCollider2D;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            ownerCollider2D = owner.HealthSystem.GetComponent<Collider2D>();

            owner.HealthSystem.OnHit -= TriggerParry;
            owner.HealthSystem.OnHit += TriggerParry;
        }

        private void OnDestroy()
        {
            if (!owner) return;
            owner.HealthSystem.OnHit -= TriggerParry;
        }

        private void OnParrySuccess()
        {
            OnTriggerAutoSkill?.Invoke();
            
            owner.TryPlayFeedback(skillData.ParrySuccessFeedback);
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            var targetsInRange =
                Physics2D.OverlapCircleAll(owner.transform.position, skillData.ExplosionRadius, damageLayer);

            StatusEffectManager.ApplyEffectTo(gameObject, skillData.SelfEffectsOnParrySuccess);
            owner.HealthSystem.Heal(skillData.HealOnSuccess);

            foreach (var target in targetsInRange)
            {
                StatusEffectManager.ApplyEffectTo(target.gameObject, skillData.ExplosionEffectsToTarget);
                Vector2 knockBackDirection = target.transform.position - owner.transform.position;
                Vector2 knockBackDestination = (Vector2)target.transform.position +
                                               (knockBackDirection.normalized * skillData.KnockBackDistance);

                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, skillData.KnockBackDuration);
                CombatManager.ApplyCalculatedDamageTo(target.gameObject, owner.gameObject, owner.gameObject,
                    target.ClosestPoint(owner.transform.position), skillData.ExplosionBaseDamage,
                    skillData.ExplosionDamageMultiplier, 0, 0, 0, 0);
            }

            if (owner is PlayerController playerController)
            {
                playerController.CombatRankSystem.OnParrySuccessCondition(false, _damageNegate);
            }
        }

        protected override void OnSkillStart()
        {
            _isParryTrigger = false;
            _damageNegate = 0;
            owner.MovementSystem.StopFromParry(skillData.StopWhileParry);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            if (!ownerCollider2D) return;

            await UniTask.Yield();

            if (Math.Abs(skillData.ParryColliderSizeMultiplier - 1) >= 0.01f)
            {
                if (ownerCollider2D is BoxCollider2D box)
                    box.size *= skillData.ParryColliderSizeMultiplier;
                else if (ownerCollider2D is CircleCollider2D circle)
                    circle.radius *= skillData.ParryColliderSizeMultiplier;
            }

            await UniTask.WaitUntil(() => _isParryTrigger, cancellationToken: cancelToken)
                .TimeoutWithoutException(TimeSpan.FromSeconds(skillData.ParryDuration));

            if (!_isParryTrigger) return;
            OnParrySuccess();
        }

        protected override void OnSkillExit()
        {
            _isParryTrigger = false;
            _damageNegate = 0;
            owner.MovementSystem.StopFromParry(false);

            if (Math.Abs(skillData.ParryColliderSizeMultiplier - 1) < 0.01f) return;
            if (!ownerCollider2D) return;
            if (ownerCollider2D is BoxCollider2D box)
                box.size /= skillData.ParryColliderSizeMultiplier;
            else if (ownerCollider2D is CircleCollider2D circle)
                circle.radius /= skillData.ParryColliderSizeMultiplier;
        }

        private void TriggerParry(int damageIncome)
        {
            if (!IsPerforming) return;
            _isParryTrigger = true;
            _damageNegate = damageIncome;
        }
    }
}