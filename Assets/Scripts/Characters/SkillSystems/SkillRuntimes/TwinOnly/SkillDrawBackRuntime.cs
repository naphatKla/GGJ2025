using System.Collections.Generic;
using System.Threading;
using Cameras;
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
            _twirlController.InputSystem.Enable = false;
            _twirlController.SkillSystem.SetCanUseSkills(false);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await _twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, 90), 0.5f,
                    RotateMode.FastBeyond360)
                .SetRelative();

            _twirlController.RedBody.DOLocalMoveX(20, 0.5f);
            _twirlController.BlueBody.DOLocalMoveX(-20, 0.5f);
            Cinemachine2DCameraController.Current.PushOrtho(22f, 3, this, 1f);

            float chaseDuration = 3f;
            float speed = 40f;
            float timeCount = 0f;

            while (timeCount <= chaseDuration)
            {
                Vector2 cur = _twirlController.transform.position;
                Vector2 target = PlayerController.Instance.transform.position;

                Vector2 next = Vector2.MoveTowards(cur, target, speed * Time.deltaTime);
                _twirlController.MovementSystem.TryMoveRawPosition(next);

                timeCount += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
            }

            _twirlController.RedBody.transform.DOLocalMove(_twirlController.RedBodyLocalPosOnStart, 0.5f);
            await _twirlController.BlueBody.transform.DOLocalMove(_twirlController.BlueBodyLocalPosOnStart, 0.125f);

            Cinemachine2DCameraController.Current.ShakeCamera(50f);

            
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectSelfOnSuccess);

            await UniTask.WaitForSeconds(3f, cancellationToken: cancelToken);
            await _twirlController.Body.transform
                .DORotate(new Vector3(0f, 0f, -90), 0.5f,
                    RotateMode.FastBeyond360)
                .SetRelative();
        }

        protected override void OnSkillExit()
        {
            _twirlController.InputSystem.Enable = true;
            _twirlController.SkillSystem.SetCanUseSkills(true);
            Cinemachine2DCameraController.Current.CancelByOwner(this);
        }
    }
}