using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cameras;
using Characters.CombatSystems;
using Characters.Controllers;
using Characters.MovementSystems;
using Characters.SO.SkillDataSo;
using Characters.SO.SkillDataSo.TwinOnly;
using Characters.StatusEffectSystems;
using Characters.StatusEffectSystems.StatusEffects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes.TwinOnly
{
    public class SkillDrawBackRuntime : BaseSkillRuntime<SkillDrawBackDataSo>
    {
        private TwinController _twirlController;
        private List<Tween> tws = new List<Tween>();
        private float stunTime = 0f;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            if (owner is not TwinController)
            {
                Debug.LogError("Wrong Type of owner. Skill Twirl is made for The Twin Only!");
                return;
            }

            _twirlController = (TwinController)owner;
            base.AssignSkillData(skillData, owner);
        }

        public override void PerformSkill()
        {
            if (IsCooldown || IsPerforming) return;
            if (owner.HealthSystem.HealthPercentage01 * 100 > skillData.AvailableOnHpLessOrEqualThan) return;
            base.PerformSkill();
        }

        protected override void OnSkillStart()
        {
            _twirlController.InputSystem.Enable = false;
            _twirlController.SkillSystem.SetCanUseSkills(false);
            _twirlController.HealthSystem.CanAim = false;   
            tws.Clear();

            stunTime = skillData.EffectSelfOnSuccess.Count > 0
                ? skillData.EffectSelfOnSuccess[0].OverrideDuration + 0.15f
                : 0;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            float rotateAngle = 270;
            float rotateDuration = 0.5f;
            float delayChargeAfterRotate = 0.5f;
            float chargeDistance = 20;
            float moveToChargeDistanceDuration = 0.5f;
            float delayBeforeZoomOut = 0.15f;
            float orthoSize = 24f;
            float zoomOutDuration = skillData.ChargeTime - 0.5f;
            float blendOverride = 0.25f;
            float chaseDuration = skillData.ChargeTime - skillData.FleeDuration;
            float followSpeed = skillData.FollowSpeed;
            float fleeDuration = skillData.FleeDuration;
            float attackMergeBackDuration = skillData.AttackMergeBackDuration;
            float attackCamShake = 50f;
            float hitPerSec = 1;
            float normalDamageRadius = 2f;

            if (await SplitOut(cancelToken, rotateAngle, rotateDuration, delayChargeAfterRotate, chargeDistance,
                    moveToChargeDistanceDuration, delayBeforeZoomOut, orthoSize, zoomOutDuration,
                    blendOverride)) return;
            
            if (await Follow(cancelToken, chaseDuration, followSpeed, fleeDuration)) return;
            if (await Attack(cancelToken, attackMergeBackDuration, attackCamShake, hitPerSec, normalDamageRadius)) return;

            if (cancelToken.IsCancellationRequested) return;

            tws.Add(_twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, -rotateAngle), rotateDuration,
                    RotateMode.FastBeyond360)
                .SetRelative());

            await UniTask.WaitForSeconds(rotateDuration, cancellationToken: cancelToken);
        }
        
        private async Task<bool> SplitOut(CancellationToken cancelToken, float rotateAngle, float rotateDuration,
            float delayChargeAfterRotate, float chargeDistance, float moveToChargeDistanceDuration,
            float delayBeforeZoomOut,
            float orthoSize, float zoomOutDuration, float blendOverride)
        {
            tws.Add(_twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, rotateAngle), rotateDuration,
                    RotateMode.FastBeyond360)
                .SetRelative());

            await UniTask.WaitForSeconds(delayChargeAfterRotate, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return true;

            tws.Add(_twirlController.RedBody.DOLocalMoveX(chargeDistance, moveToChargeDistanceDuration));
            tws.Add(_twirlController.BlueBody.DOLocalMoveX(-chargeDistance, moveToChargeDistanceDuration));

            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectSelfSplitOut);
            await UniTask.WaitForSeconds(delayBeforeZoomOut, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return true;

            Cinemachine2DCameraController.Current.PushOrtho(orthoSize, zoomOutDuration, this, blendOverride);
            return false;
        }
        
        private async Task<bool> Follow(CancellationToken cancelToken, float chaseDuration, float speed, float fleeDuration)
        {
            float timeCount = 0f;

            while (timeCount <= chaseDuration && !cancelToken.IsCancellationRequested)
            {
                Vector2 cur = _twirlController.transform.position;
                Vector2 target = PlayerController.Instance.transform.position;

                Vector2 next = Vector2.MoveTowards(cur, target, speed * Time.deltaTime);
                _twirlController.MovementSystem.TryMoveRawPosition(next);

                timeCount += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: cancelToken);
            }

            await UniTask.WaitForSeconds(fleeDuration, cancellationToken: cancelToken);

            return cancelToken.IsCancellationRequested;
        }
        
        private async Task<bool> Attack(CancellationToken cancelToken, float attackDuration, float attackCamShake,
            float hitPerSec,
            float damageRadius)
        {
            tws.Add(_twirlController.RedBody.transform.DOLocalMove(_twirlController.RedBodyLocalPosOnStart,
                attackDuration));
            tws.Add(_twirlController.BlueBody.transform.DOLocalMove(_twirlController.BlueBodyLocalPosOnStart,
                attackDuration));

            await UniTask.WaitForSeconds(attackDuration, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return true;

            Cinemachine2DCameraController.Current.ShakeCamera(attackCamShake);
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
            
            owner.DamageOnTouch.EnableDamage(gameObject, this, hitPerSec, DamageOnTouch.OverlapShape.Circle,
                circle: damageRadius,
                baseSkillDamage: skillData.BaseDamagePerHit, damageMultiplier: skillData.DamageMultiplier);

            try
            {
                await UniTask.WaitForSeconds(0.015f, cancellationToken: cancelToken);
            }
            catch (Exception e)
            {
                // ignored
            }
            finally
            {
                owner.DamageOnTouch.DisableDamage(this);
                await UniTask.WaitForSeconds(0.01f, cancellationToken: cancelToken);
            }

            // if not success and was stun 
            if (StatusEffectManager.TryGetEffect(owner.gameObject, StatusEffectName.Stun, out BaseStatusEffect _))
            {
                return false;
            }

            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectSelfOnSuccess);
            
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            var targetsInRange =
                Physics2D.OverlapCircleAll(owner.transform.position, skillData.ExplosionRadius, damageLayer);
            
            foreach (var target in targetsInRange)
            {
                Vector2 knockBackDirection = target.transform.position - owner.transform.position;
                Vector2 knockBackDestination = (Vector2)target.transform.position +
                                               (knockBackDirection.normalized * skillData.KnockBackDistance);

                target.GetComponent<BaseMovementSystem>()
                    .TryMoveToPositionOverTime(knockBackDestination, skillData.KnockBackDuration);
                
                CombatManager.ApplyCalculatedDamageTo(target.gameObject, owner.gameObject, owner.gameObject,
                    target.ClosestPoint(owner.transform.position), skillData.BaseExplosionDamagePerHit,
                    skillData.ExplosionDamageMultiplier, 0, 0, 0, 0);
            }
            
            owner.TryPlayFeedback(skillData.AttackSuccessFeedback);
            
            await UniTask.WaitForSeconds(stunTime, cancellationToken: cancelToken);
            return false;
        }

        protected override void OnSkillExit()
        {
            _twirlController.InputSystem.Enable = true;
            _twirlController.HealthSystem.CanAim = true;
            _twirlController.SkillSystem.SetCanUseSkills(true);
            Cinemachine2DCameraController.Current.CancelByOwner(this);

            foreach (var tween in tws)
            {
                if (!tween.IsActive()) continue;
                tween.Kill(true);
            }
        }
    }
}