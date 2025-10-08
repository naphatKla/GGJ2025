using System.Threading;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Characters.SO.SkillDataSo.TwinOnly;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes.TwinOnly
{
    public class SkillTwirlRuntime : BaseSkillRuntime<SkillTwirlDataSo>
    {
        private TwinController _twirlController;

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
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            _twirlController.RedBody.transform.DOLocalMoveX(-8, 0.5f);
            await _twirlController.BlueBody.transform.DOLocalMoveX(8f, 0.5f);

            owner.MovementSystem.AddCurrentSpeedMultiplier(100f);
            await _twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, 360f * 10), skillData.TwirlDuration, RotateMode.FastBeyond360)
                .SetRelative()
                .SetEase(skillData.TwirlSpeedCurve);

            owner.MovementSystem.AddCurrentSpeedMultiplier(-100f);
            _twirlController.RedBody.transform.DOLocalMove(_twirlController.RedBodyLocalPosOnStart, 0.5f);
            await _twirlController.BlueBody.transform.DOLocalMove(_twirlController.BlueBodyLocalPosOnStart, 0.5f);
            owner.MovementSystem.AddCurrentSpeedMultiplier(100f);
        }

        protected override void OnSkillExit()
        {
            owner.SkillSystem.SetCanUseSkills(true);
        }
    }
}