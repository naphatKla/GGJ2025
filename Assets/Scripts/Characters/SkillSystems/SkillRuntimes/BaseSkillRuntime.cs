using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.InputSystems.Interface;
using Characters.StatusEffectSystems;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public abstract class BaseSkillRuntime : MonoBehaviour
    {
        protected float cooldown;
        public float Cooldown => cooldown;
        public bool IsCooldown => cooldown > 0;
        public abstract bool IsPerforming { get; protected set; }

        private Action cooldownReadyCallback;

        public abstract void AssignSkillData(BaseSkillDataSo skillData, BaseController owner);
        public abstract void PerformSkill();
        public abstract void CancelSkill(int milliSecondDelay = 0);

        public virtual void SetCooldown(float value)
        {
            float prev = cooldown;
            cooldown = Mathf.Max(0, value);
            if (prev > 0 && cooldown <= 0)
            {
                cooldownReadyCallback?.Invoke();
                cooldownReadyCallback = null;
            }
        }

        public virtual void UpdateCoolDown(float deltaTime)
        {
            if (cooldown <= 0) return;
            SetCooldown(cooldown - deltaTime);
        }

        public void RegisterCooldownReadyCallback(Action callback)
        {
            cooldownReadyCallback = callback;
        }

        public void ClearCooldownReadyCallback()
        {
            cooldownReadyCallback = null;
        }
    }

    public abstract class BaseSkillRuntime<T> : BaseSkillRuntime where T : BaseSkillDataSo
    {
        protected T skillData;
        protected BaseController owner;
        protected DirectionContainer aimDirection => owner.InputSystem.SightDirection;
        protected List<StatusEffectDataPayload> effectsApplyOnStart;

        private CancellationTokenSource _cts;
        public override bool IsPerforming { get; protected set; }

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            this.skillData = skillData as T;
            this.owner = owner;
            SetCooldown(skillData.Cooldown);
            effectsApplyOnStart = new List<StatusEffectDataPayload>(skillData.StatusEffectOnSkillStart);
        }

        public override async void PerformSkill()
        {
            if (IsCooldown || IsPerforming) return;

            SetCooldown(skillData.Cooldown);
            _cts = new CancellationTokenSource();

            HandleSkillStart();
            try
            {
                await OnSkillUpdate(_cts.Token);
            }
            catch (OperationCanceledException) { }

            HandleSkillExit();
        }

        [Button]
        public override async void CancelSkill(int milliSecondDelay = 0)
        {
            await UniTask.Delay(milliSecondDelay);
            if (IsPerforming) return;
            _cts?.Cancel();
        }

        protected virtual void HandleSkillStart()
        {
            IsPerforming = true;
            OnSkillStart();
            StatusEffectManager.ApplyEffectTo(owner.gameObject, effectsApplyOnStart);
        }

        protected virtual void HandleSkillExit()
        {
            IsPerforming = false;
            OnSkillExit();
        }

        protected abstract void OnSkillStart();
        protected abstract UniTask OnSkillUpdate(CancellationToken cancelToken);
        protected abstract void OnSkillExit();
    }
}
