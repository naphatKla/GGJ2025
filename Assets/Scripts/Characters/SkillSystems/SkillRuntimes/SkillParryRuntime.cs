using System;
using System.Threading;
using Characters.Controllers;
using Characters.HeathSystems;
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
        private Collider2D ownerCollider2D;

        private void OnDestroy()
        {
            if (!owner) return;
            owner.HealthSystem.OnHitAttempt -= OnHitAttempt;
        }

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            ownerCollider2D = owner.HealthSystem.GetComponent<Collider2D>();

            owner.HealthSystem.OnHitAttempt -= OnHitAttempt;
            owner.HealthSystem.OnHitAttempt += OnHitAttempt;
        }

        protected override void OnSkillStart()
        {
            _isParryTrigger = false;
            owner.MovementSystem.StopFromParry(skillData.StopWhileParry);

            // ปรับขนาด collider ถ้าต้องการ
            if (Math.Abs(skillData.ParryColliderSizeMultiplier - 1f) >= 0.01f && ownerCollider2D != null)
            {
                if (ownerCollider2D is BoxCollider2D box)
                    box.size *= skillData.ParryColliderSizeMultiplier;
                else if (ownerCollider2D is CircleCollider2D circle)
                    circle.radius *= skillData.ParryColliderSizeMultiplier;
            }

            // ===== Coyote Parry: โดนมาก่อน แล้วกดทันในช่วง buffer =====
            bool success = false;

            // 1) ยกเลิกดาเมจที่กำลังจะโดนจริงก่อน (ถ้ามี)
            success = owner.HealthSystem.ConsumePendingHit(info =>
            {
                _isParryTrigger = true;
            });

            // 2) ถ้าไม่มี pending (เช่น ตอนนั้น iframe อยู่) → ลองดูจาก HitAttempt แทน
            if (!success)
            {
                owner.HealthSystem.ConsumeHitAttempt(info =>
                {
                    _isParryTrigger = true;
                });
            }
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask
                .WaitUntil(() => _isParryTrigger, cancellationToken: cancelToken)
                .TimeoutWithoutException(TimeSpan.FromSeconds(skillData.ParryDuration));

            if (!_isParryTrigger) return;

            OnParrySuccess();
        }

        protected override void OnSkillExit()
        {
            _isParryTrigger = false;
            owner.MovementSystem.StopFromParry(false);

            // คืนขนาด collider กลับค่าเดิม
            if (Math.Abs(skillData.ParryColliderSizeMultiplier - 1f) < 0.01f) return;
            if (!ownerCollider2D) return;

            if (ownerCollider2D is BoxCollider2D box)
                box.size /= skillData.ParryColliderSizeMultiplier;
            else if (ownerCollider2D is CircleCollider2D circle)
                circle.radius /= skillData.ParryColliderSizeMultiplier;
        }

        private void OnParrySuccess()
        {
            OnTriggerAutoSkill?.Invoke();

            owner.TryPlayFeedback(skillData.ParrySuccessFeedback);

            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            var targetsInRange = Physics2D.OverlapCircleAll(
                owner.transform.position,
                skillData.ExplosionRadius,
                damageLayer
            );

            // เอฟเฟกต์ใส่ตัวเองตอน parry สำเร็จ
            StatusEffectManager.ApplyEffectTo(gameObject, skillData.SelfEffectsOnParrySuccess);
            owner.HealthSystem.Heal(skillData.HealOnSuccess);

            // ระเบิดศัตรูรอบ ๆ + knockback + ดาเมจ
            foreach (var target in targetsInRange)
            {
                StatusEffectManager.ApplyEffectTo(target.gameObject, skillData.ExplosionEffectsToTarget);

                Vector2 knockBackDirection = target.transform.position - owner.transform.position;
                Vector2 knockBackDestination =
                    (Vector2)target.transform.position +
                    knockBackDirection.normalized * skillData.KnockBackDistance;

                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, skillData.KnockBackDuration);

                CombatManager.ApplyCalculatedDamageTo(
                    target.gameObject,
                    owner.gameObject,
                    owner.gameObject,
                    target.ClosestPoint(owner.transform.position),
                    skillData.ExplosionBaseDamage,
                    skillData.ExplosionDamageMultiplier,
                    0, 0, 0, 0
                );
            }
        }

        private void OnHitAttempt(HealthSystem.HitInfo info)
        {
            if (!IsPerforming || _isParryTrigger) return;
            _isParryTrigger = true;
        }
    }
}