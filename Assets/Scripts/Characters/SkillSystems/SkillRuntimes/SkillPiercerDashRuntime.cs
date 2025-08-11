using System;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillPiercerDashRuntime : BaseSkillRuntime<SkillPiercerDashDataSo>
    {
        protected override void OnSkillStart()
        {
            owner.TryPlayFeedback(FeedbackName.PiercerDash);
            owner.MovementSystem.StopFromPiercerDash(true);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            float timeCount = 0;
            
            while (timeCount < skillData.DashChargeTime && !cancelToken.IsCancellationRequested)
            {
                timeCount += Time.fixedDeltaTime;
                owner.transform.up = aimDirection.direction;
                await UniTask.Yield(PlayerLoopTiming.Update, cancelToken);
            }

            if (cancelToken.IsCancellationRequested) return;
            Vector2 finalDirection = aimDirection.direction;

            await UniTask.WaitForSeconds(skillData.DashChargeTime, cancellationToken: cancelToken);
            
            if (cancelToken.IsCancellationRequested) return;

            Vector2 destination = finalDirection * skillData.DashDistance;
            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 1, skillData.DashBaseDamage,
                skillData.DamageMultiplier);

            var dashTask = owner.MovementSystem.TryMoveToPositionOverTime(destination, skillData.DashDuration,
                skillData.DashEaseCurve, skillData.DashMoveCurve).WithCancellation(cancelToken);
            
            var damageTask = UniTask.Delay(TimeSpan.FromSeconds(skillData.DamageEnableDuration), cancellationToken: cancelToken);
            await UniTask.WhenAll(dashTask, damageTask);
        }

        protected override void OnSkillExit()
        {
            owner.DamageOnTouch.DisableDamage(this);
            owner.MovementSystem.StopFromPiercerDash(false);
        }
    }
}