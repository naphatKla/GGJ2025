using System;
using System.Collections.Generic;
using System.Threading;
using Cameras;
using Characters.CombatSystems;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Characters.SO.SkillDataSo.TwinOnly;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
            float chaseDuration = skillData.ChargeTime;
            float speed = 45f;
            float attackDuration = 0.07f;
            float attackCamShake = 50f;
            float hitPerSec = 1;
            float damageRadias = 8f;

            tws.Add(_twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, rotateAngle), rotateDuration,
                    RotateMode.FastBeyond360)
                .SetRelative());

            await UniTask.WaitForSeconds(delayChargeAfterRotate, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            tws.Add(_twirlController.RedBody.DOLocalMoveX(chargeDistance, moveToChargeDistanceDuration));
            tws.Add(_twirlController.BlueBody.DOLocalMoveX(-chargeDistance, moveToChargeDistanceDuration));

            await UniTask.WaitForSeconds(delayBeforeZoomOut, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            Cinemachine2DCameraController.Current.PushOrtho(orthoSize, zoomOutDuration, this, blendOverride);


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

            if (cancelToken.IsCancellationRequested) return;

            tws.Add(_twirlController.RedBody.transform.DOLocalMove(_twirlController.RedBodyLocalPosOnStart,
                attackDuration));
            tws.Add(_twirlController.BlueBody.transform.DOLocalMove(_twirlController.BlueBodyLocalPosOnStart,
                attackDuration));

            await UniTask.WaitForSeconds(attackDuration, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            Cinemachine2DCameraController.Current.ShakeCamera(attackCamShake);
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectSelfOnSuccess);

            owner.DamageOnTouch.EnableDamage(gameObject, this, hitPerSec, DamageOnTouch.OverlapShape.Circle,
                circle: damageRadias,
                baseSkillDamage: skillData.BaseDamagePerHit, damageMultiplier: skillData.DamageMultiplier);

            try
            {
                await UniTask.Yield(cancelToken);
            }
            catch (Exception e)
            {
            }
            finally
            {
                owner.DamageOnTouch.DisableDamage(this);
            }


            await UniTask.WaitForSeconds(stunTime, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            tws.Add(_twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, -rotateAngle), rotateDuration,
                    RotateMode.FastBeyond360)
                .SetRelative());

            await UniTask.WaitForSeconds(rotateDuration, cancellationToken: cancelToken);
        }

        protected override void OnSkillExit()
        {
            _twirlController.InputSystem.Enable = true;
            Cinemachine2DCameraController.Current.CancelByOwner(this);
            
            foreach (var tween in tws)
            {
                if (!tween.IsActive()) continue;
                tween.Kill(true);
            }
        }
    }
}