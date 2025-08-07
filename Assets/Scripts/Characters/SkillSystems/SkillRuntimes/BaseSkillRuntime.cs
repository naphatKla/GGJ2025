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
    // interface for skill which can trigger the another auto skill in skill system
    public interface IAutoSkillTriggerSource
    {
        event Action OnTriggerAutoSkill;
    }

    // for skill which have special condition, etc light step, เป็นสกิลที่ใช้แล้ว จะรอ condition พิเศษ
    public interface ISpecialConditionSkill
    {
        public bool IsWaitForCondition { get;}
    }
    
    public abstract class BaseSkillRuntime : MonoBehaviour
    {
        protected float cooldown;
        protected float currentCooldown;
        public float Cooldown => cooldown;
        public float CurrentCooldown => currentCooldown;
        public bool IsCooldown => currentCooldown > 0;
        public abstract bool IsPerforming { get; protected set; }
        private Action cooldownReadyCallback;

        public abstract void AssignSkillData(BaseSkillDataSo skillData, BaseController owner);
        public abstract void PerformSkill();
        public abstract void CancelSkill(int milliSecondDelay = 0);

        public virtual void SetCurrentCooldown(float value)
        {
            float prev = currentCooldown;
            currentCooldown = Mathf.Max(0, value);
            if (prev > 0 && currentCooldown <= 0)
            {
                cooldownReadyCallback?.Invoke();
                cooldownReadyCallback = null;
            }
        }

        public virtual void UpdateCoolDown(float deltaTime)
        {
            if (currentCooldown <= 0) return;
            SetCurrentCooldown(currentCooldown - deltaTime);
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
            cooldown = skillData.Cooldown;
            SetCurrentCooldown(skillData.Cooldown);
            effectsApplyOnStart = new List<StatusEffectDataPayload>(skillData.StatusEffectOnSkillStart);
        }

        public override async void PerformSkill()
        {
            if (IsCooldown || IsPerforming) return;

            SetCurrentCooldown(skillData.Cooldown);
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
