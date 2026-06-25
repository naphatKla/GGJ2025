using System;
using System.Collections.Generic;
using System.Threading;
using Characters.CharacterVisual;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillEnemyDashKnockbackRuntime : BaseSkillRuntime<SkillEnemyDashKnockbackDataSo>
    {
        private readonly HashSet<BaseController> _knockedTargets = new();
        private RotateSpriteByVelocity faceController;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            faceController = GetComponent<RotateSpriteByVelocity>();
        }

        protected override void OnSkillStart()
        {
            _knockedTargets.Clear();
            owner.MovementSystem.StopFromPiercerDash(true);

            owner.DamageOnTouch.OnHit -= OnDashHit;
            owner.DamageOnTouch.OnHit += OnDashHit;

            if (faceController)
                faceController.enabled = false;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            float timeCount = 0f;
            float rotateSpeedRad = 720f * Mathf.Deg2Rad;

            while (timeCount < skillData.DashChargeTime && !cancelToken.IsCancellationRequested)
            {
                timeCount += Time.deltaTime;

                Vector2 desired = GetDashDirection();
                if (desired.sqrMagnitude > 0.0001f && owner.Body)
                {
                    Vector3 upNext = Vector3.RotateTowards(
                        owner.Body.transform.up,
                        desired.normalized,
                        rotateSpeedRad * Time.deltaTime,
                        0f);

                    owner.Body.transform.up = upNext;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancelToken);
            }

            if (cancelToken.IsCancellationRequested) return;

            Vector2 dashDirection = owner.Body ? (Vector2)owner.Body.transform.up : GetDashDirection();
            if (dashDirection.sqrMagnitude <= 0.0001f) return;

            Vector2 dashPosition = (Vector2)transform.position + dashDirection * skillData.DashDistance;

            await UniTask.WaitForSeconds(skillData.DashPrepareDuration, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            owner.TryPlayFeedback(skillData.DashFeedback);
            owner.DamageOnTouch.EnableDamage(
                owner.gameObject,
                owner.CharacterData.CharacterId,
                this,
                1,
                skillData.DashBaseDamage,
                skillData.DamageMultiplier,
                canHitWithDamageOnTouch: true);

            var dashTask = owner.MovementSystem
                .TryMoveToPositionOverTime(
                    dashPosition,
                    skillData.DashDuration,
                    skillData.DashEaseCurve,
                    skillData.DashMoveCurve)?
                .WithCancellation(cancelToken);

            if (!dashTask.HasValue) return;

            var damageTask = UniTask.Delay(
                TimeSpan.FromSeconds(skillData.DamageEnableDuration),
                cancellationToken: cancelToken);

            await UniTask.WhenAll(dashTask.Value, damageTask);
        }

        protected override void OnSkillExit()
        {
            owner.DamageOnTouch.DisableDamage(this);
            owner.DamageOnTouch.OnHit -= OnDashHit;
            owner.MovementSystem.StopFromPiercerDash(false);
            _knockedTargets.Clear();

            if (faceController)
                faceController.enabled = true;
        }

        private void OnDashHit(GameObject target)
        {
            if (!CombatManager.TryGetCharacterFromCache(target, out var targetController)) return;
            if (targetController is not PlayerController) return;
            if (skillData.KnockBackOnlyOncePerSkill && !_knockedTargets.Add(targetController)) return;

            Vector2 knockBackDirection = targetController.transform.position - owner.transform.position;
            if (knockBackDirection.sqrMagnitude <= 0.0001f)
                knockBackDirection = GetDashDirection();

            if (knockBackDirection.sqrMagnitude <= 0.0001f)
                knockBackDirection = owner.transform.up;

            Vector2 knockBackDestination =
                (Vector2)targetController.transform.position +
                knockBackDirection.normalized * skillData.KnockBackDistance;

            targetController.MovementSystem.TryMoveToPositionOverTime(
                knockBackDestination,
                skillData.KnockBackDuration);

            if (skillData.EffectsToTarget != null)
                StatusEffectManager.ApplyEffectTo(targetController.gameObject, skillData.EffectsToTarget);
        }

        private Vector2 GetDashDirection()
        {
            Vector2 direction = aimDirection.direction;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;

            if (PlayerController.Instance)
            {
                direction = PlayerController.Instance.transform.position - owner.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                    return direction.normalized;
            }

            direction = owner.Body ? (Vector2)owner.Body.transform.up : (Vector2)owner.transform.up;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        }

        private void OnDisable()
        {
            if (owner && owner.DamageOnTouch)
                owner.DamageOnTouch.OnHit -= OnDashHit;

            if (owner)
                owner.MovementSystem.StopFromPiercerDash(false);

            if (faceController)
                faceController.enabled = true;

            _knockedTargets.Clear();
        }
    }
}
