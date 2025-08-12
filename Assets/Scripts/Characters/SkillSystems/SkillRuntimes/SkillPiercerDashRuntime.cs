using System;
using System.Threading;
using Characters.CharacterVisual;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillPiercerDashRuntime : BaseSkillRuntime<SkillPiercerDashDataSo>
    {
        private RotateSpriteByVelocity faceController;
        private bool _isPlayer;

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            base.AssignSkillData(skillData, owner);
            faceController = GetComponent<RotateSpriteByVelocity>();
            _isPlayer = base.owner is PlayerController;
        }

        protected override void OnSkillStart()
        {
            owner.TryPlayFeedback(FeedbackName.PiercerDash);
            owner.MovementSystem.StopFromPiercerDash(true);

            if (faceController)
                faceController.enabled = false;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            float timeCount = 0;
            float rotateSpeedDeg = 720;
            float rotateSpeedRad = rotateSpeedDeg * Mathf.Deg2Rad;

            while (timeCount < skillData.DashChargeTime && !cancelToken.IsCancellationRequested)
            {
                timeCount += Time.deltaTime;

                if (_isPlayer)
                    owner.transform.up = aimDirection.direction;
                else
                {
                    var desired = (Vector3)aimDirection.direction; // 2D ใช้ up เป็นแกนหัน
                    if (desired.sqrMagnitude > 0.0001f)
                    {
                        var upNow = owner.transform.up;
                        var upNext = Vector3.RotateTowards(upNow, desired.normalized, rotateSpeedRad * Time.deltaTime, 0f);
                        owner.transform.up = upNext;
                    }
                }
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancelToken);
            }

            if (cancelToken.IsCancellationRequested) return;
            Vector2 destination = owner.transform.position + (owner.transform.up * skillData.DashDistance);

            await UniTask.WaitForSeconds(skillData.DashPrepareDuration, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 1, skillData.DashBaseDamage,
                skillData.DamageMultiplier);

            var dashTask = owner.MovementSystem.TryMoveToPositionOverTime(destination, skillData.DashDuration,
                skillData.DashEaseCurve, skillData.DashMoveCurve).WithCancellation(cancelToken);

            var damageTask = UniTask.Delay(TimeSpan.FromSeconds(skillData.DamageEnableDuration),
                cancellationToken: cancelToken);
            await UniTask.WhenAll(dashTask, damageTask);
        }

        protected override void OnSkillExit()
        {
            owner.DamageOnTouch.DisableDamage(this);
            owner.MovementSystem.StopFromPiercerDash(false);

            if (faceController)
                faceController.enabled = true;
        }
    }
}