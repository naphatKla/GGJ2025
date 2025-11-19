using System;
using System.Collections.Generic;
using System.Linq;
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
    
    public abstract class BaseSkillRuntime : MonoBehaviour
    {
        protected float cooldown;
        protected float currentCooldown;
        protected float globalCooldownCounter;
        protected int maxStack;
        protected int currentStack;
        public float Cooldown => cooldown;
        public float CurrentCooldown => currentCooldown;
        public int MaxStack => maxStack;
        public int CurrentStack => currentStack;
        public bool IsCooldown => currentCooldown > 0;
        public abstract bool IsPerforming { get; protected set; }
        private Action cooldownReadyCallback;
        public  Action SkillPerformCallback;

        public abstract void AssignSkillData(BaseSkillDataSo skillData, BaseController owner);
        public abstract void PerformSkill();
        public abstract void CancelSkill(int milliSecondDelay = 0);

        public virtual void SetCurrentCooldown(float value)
        {
            float prev = currentCooldown;
            currentCooldown = Mathf.Max(0, value);
            if (prev > 0 && currentCooldown <= 0)
            {
                currentStack = maxStack;
                cooldownReadyCallback?.Invoke();
                cooldownReadyCallback = null;
            }
        }

        public virtual void UpdateCoolDown(float deltaTime)
        {
            if (globalCooldownCounter > 0)
                globalCooldownCounter -= deltaTime;
            
            if (currentCooldown > 0)
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

        protected CancellationTokenSource cts = new CancellationTokenSource();
        public override bool IsPerforming { get; protected set; }

        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            this.skillData = skillData as T;
            this.owner = owner;
            cooldown = skillData.Cooldown;
            maxStack = skillData.MaxStack;
            currentStack = maxStack;
            SetCurrentCooldown(skillData.Cooldown);
            effectsApplyOnStart = new List<StatusEffectDataPayload>(skillData.StatusEffectOnSkillStart);
        }

        public override async void PerformSkill()
        {
            if (IsCooldown || IsPerforming) return;
            if (globalCooldownCounter > 0) return;

            globalCooldownCounter = skillData.GlobalCooldown;
            currentStack = Mathf.Clamp(currentStack - 1, 0, maxStack);
            
            if (currentStack <= 0)
                SetCurrentCooldown(skillData.Cooldown);

            HandleSkillStart();
            try
            {
                await OnSkillUpdate(cts.Token);
            }
            catch (OperationCanceledException) { }

            HandleSkillExit();
        }

        [Button]
        public override async void CancelSkill(int milliSecondDelay = 0)
        {
            await UniTask.Delay(milliSecondDelay);
            if (!IsPerforming) return;
            cts?.Cancel();
            cts = new CancellationTokenSource();
        }

        protected virtual void HandleSkillStart()
        {
            IsPerforming = true;
            SkillPerformCallback?.Invoke();
            owner.TryPlayFeedback(skillData.StartFeedback);
            OnSkillStart();
            StatusEffectManager.ApplyEffectTo(owner.gameObject, effectsApplyOnStart);
        }

        protected virtual void HandleSkillExit()
        {
            IsPerforming = false;
            owner.TryPlayFeedback(skillData.ExitFeedback);
            OnSkillExit();
            
            if (!skillData.ClearBuffOnSkillExit) return;
            if (!owner) return;
            foreach (var statusEffectName in skillData.StatusEffectOnSkillStart.Select(effect => effect.EffectData.EffectName))
                StatusEffectManager.RemoveEffectAt(owner.gameObject, statusEffectName);
        }

        protected abstract void OnSkillStart();
        protected abstract UniTask OnSkillUpdate(CancellationToken cancelToken);
        protected abstract void OnSkillExit();

        private void OnDestroy()
        {
            CancelSkill();
        }
    }
}
