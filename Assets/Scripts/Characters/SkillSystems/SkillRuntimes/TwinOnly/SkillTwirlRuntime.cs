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
    public class SkillTwirlRuntime : BaseSkillRuntime<SkillTwirlDataSo>
    {
        private TwinController _twirlController;
        private List<Tween> tws = new List<Tween>();

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

        protected override void OnSkillStart()
        {
            owner.SkillSystem.SetCanUseSkills(false);
            owner.MovementSystem.AddCurrentSpeedMultiplier(-100f);
            owner.MovementSystem.AddCurrentSpeedMultiplier(skillData.SpeedUpMultiplier);
            tws.Clear();
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            tws.Add(_twirlController.RedBody.transform.DOLocalMoveX(-skillData.ExpandRadius,
                skillData.StartExpandDuration));
            tws.Add(_twirlController.BlueBody.transform.DOLocalMoveX(skillData.ExpandRadius,
                skillData.StartExpandDuration));

            await UniTask.WaitForSeconds(skillData.StartExpandDuration, cancellationToken: cancelToken);
            if (cancelToken.IsCancellationRequested) return;

            owner.MovementSystem.AddCurrentSpeedMultiplier(100f);

            tws.Add(_twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, 360f * skillData.SpinRound), skillData.TwirlDuration,
                    RotateMode.FastBeyond360)
                .SetRelative()
                .SetEase(skillData.TwirlSpeedCurve));

            await UniTask.WaitForSeconds(skillData.DelayEnableDamageAfterStartSpin, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested)
            {
                owner.MovementSystem.AddCurrentSpeedMultiplier(-100f);
                return;
            }

            // damage on here
            owner.DamageOnTouch.EnableDamage(gameObject, this, 3f, DamageOnTouch.OverlapShape.Circle,
                circle: skillData.DamageRadius, baseSkillDamage: skillData.BaseDamagePerHit,
                damageMultiplier: skillData.DamageMultiplier);

            await UniTask.WaitForSeconds(skillData.DamageDuration, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested)
            {
                owner.MovementSystem.AddCurrentSpeedMultiplier(-100f);
                return;
            }

            // damage off here
            owner.DamageOnTouch.DisableDamage(this);

            float twirlDurationLeft =
                skillData.TwirlDuration - (skillData.DelayEnableDamageAfterStartSpin + skillData.DamageDuration);
            await UniTask.WaitForSeconds(twirlDurationLeft, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested)
            {
                owner.MovementSystem.AddCurrentSpeedMultiplier(-100f);
                return;
            }

            owner.MovementSystem.AddCurrentSpeedMultiplier(-100f);
            tws.Add(_twirlController.RedBody.transform.DOLocalMove(_twirlController.RedBodyLocalPosOnStart,
                skillData.MergeBackDuration));
            tws.Add(_twirlController.BlueBody.transform.DOLocalMove(_twirlController.BlueBodyLocalPosOnStart,
                skillData.MergeBackDuration));

            await UniTask.WaitForSeconds(skillData.MergeBackDuration, cancellationToken: cancelToken);
            
            if (cancelToken.IsCancellationRequested) return;
            
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.IronBody);
            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectSelfOnSuccess);
            Cinemachine2DCameraController.Current?.ShakeCamera(20);
        }

        protected override void OnSkillExit()
        {
            owner.MovementSystem.AddCurrentSpeedMultiplier(100f);
            owner.MovementSystem.AddCurrentSpeedMultiplier(-skillData.SpeedUpMultiplier);

            foreach (var tween in tws)
            {
                if (!tween.IsActive()) continue;
                tween.Kill(true);
            }
        }
    }
}