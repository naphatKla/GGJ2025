using System;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
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

        // เก็บ hit ที่ทำให้ parry ติด (ไม่ว่าจะมาจาก coyote หรือ guard)
        private HealthSystem.HitInfo? _parryHitInfo;

        private void OnDestroy()
        {
            if (!owner) return;
            owner.HealthSystem.OnHitAttempt -= OnHitAttempt;
        }

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);

            ownerCollider2D = owner.HealthSystem.GetComponent<Collider2D>();

            // กัน duplicate subscribe
            owner.HealthSystem.OnHitAttempt -= OnHitAttempt;
            owner.HealthSystem.OnHitAttempt += OnHitAttempt;
        }

        protected override void OnSkillStart()
        {
            _isParryTrigger = false;
            _parryHitInfo   = null;

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
                _parryHitInfo   = info;
                _isParryTrigger = true;
            });

            // 2) ถ้าไม่มี pending (เช่น ตอนนั้น iframe อยู่) → ลองดูจาก HitAttempt แทน
            if (!success)
            {
                owner.HealthSystem.ConsumeHitAttempt(info =>
                {
                    _parryHitInfo   = info;
                    _isParryTrigger = true;
                });
            }
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            // เวลาเริ่มช่วง parry window
            float startTime = Time.time;

            await UniTask
                .WaitUntil(() => _isParryTrigger, cancellationToken: cancelToken)
                .TimeoutWithoutException(TimeSpan.FromSeconds(skillData.ParryDuration));

            if (!_isParryTrigger) return;

            float elapsed = Time.time - startTime;
            float perfectWindow = skillData.ParryDuration * (skillData.PerfectParryDurationPercentage / 100f);
            bool isPerfect = elapsed <= perfectWindow;

            var hitInfo = _parryHitInfo ?? default;
            OnParrySuccess(hitInfo, isPerfect);
        }

        protected override void OnSkillExit()
        {
            _isParryTrigger = false;
            _parryHitInfo   = null;

            owner.MovementSystem.StopFromParry(false);

            // คืนขนาด collider กลับค่าเดิม
            if (Math.Abs(skillData.ParryColliderSizeMultiplier - 1f) < 0.01f) return;
            if (!ownerCollider2D) return;

            if (ownerCollider2D is BoxCollider2D box)
                box.size /= skillData.ParryColliderSizeMultiplier;
            else if (ownerCollider2D is CircleCollider2D circle)
                circle.radius /= skillData.ParryColliderSizeMultiplier;
        }

        private void OnParrySuccess(HealthSystem.HitInfo hitInfo, bool isPerfect)
        {
            float healAmount = isPerfect
                ? skillData.PerfectParrySuccess.healOnSuccess
                : skillData.NormalParrySuccess.healOnSuccess;

            float knockBackDistance = isPerfect
                ? skillData.PerfectParrySuccess.knockBackDistance
                : skillData.NormalParrySuccess.knockBackDistance;

            float knockBackDuration = isPerfect
                ? skillData.PerfectParrySuccess.knockBackDuration
                : skillData.NormalParrySuccess.knockBackDuration;

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
            owner.HealthSystem.Heal(healAmount);

            // ระเบิดศัตรูรอบ ๆ + knockback + ดาเมจ
            foreach (var target in targetsInRange)
            {
                StatusEffectManager.ApplyEffectTo(target.gameObject, skillData.ExplosionEffectsToTarget);

                Vector2 knockBackDirection = target.transform.position - owner.transform.position;
                Vector2 knockBackDestination =
                    (Vector2)target.transform.position +
                    knockBackDirection.normalized * knockBackDistance;

                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, knockBackDuration);

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

            if (owner is PlayerController player)
            {
                player.PlayerDisplay.UpdateParrySuccessFeedbackText(isPerfect ? "PERFECT PARRY!" : "PARRY!");
                player.CombatRankSystem.OnParrySuccessCondition(isPerfect, Mathf.CeilToInt(hitInfo.damage));
            }

            if (isPerfect)
            {
                owner.TryPlayFeedback(FeedbackName.Skill.PerfectParry);
                currentCooldown -= cooldown * (skillData.CooldownReduceOnPerfectParry / 100f);
            }
        }

        private void OnHitAttempt(HealthSystem.HitInfo info)
        {
            // กรณีกด parry ก่อน แล้วโดนตีในช่วง parry window
            if (!IsPerforming || _isParryTrigger) return;

            _parryHitInfo   = info;
            _isParryTrigger = true;
        }
    }
}
