using System;
using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.InputSystems.Interface;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using GlobalSettings;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Abstract base class for all skill runtime behaviours.
    /// Provides a unified interface for executing skills during gameplay.
    /// </summary>
    public abstract class BaseSkillRuntime : MonoBehaviour
    {
        protected float cooldown;
        public float Cooldown => cooldown;
        public abstract bool IsPerforming { get; protected set; }
     

        /// <summary>
        /// Injects the skill data into this runtime behavior.
        /// Called during skill initialization.
        /// </summary>
        /// <param name="skillData">The data asset associated with this runtime skill.</param>
        public abstract void AssignSkillData(BaseSkillDataSo skillData, BaseController owner); 
        
        /// <summary>
        /// Executes the skill using the provided owner 
        /// Should be overridden in a generic subclass to implement full behavior.
        /// </summary>
        public abstract void PerformSkill();
        
        public virtual void SetCooldown(float value)
        {
            cooldown = Mathf.Max(0, value);
        }

        public virtual void UpdateCoolDown(float deltaTime)
        {
            if (Cooldown <= 0) return;
            SetCooldown(Cooldown - deltaTime);
        }

        public abstract void CancelSkill(int milliSecondDelay = 0);
    }

    /// <summary>
    /// Generic base class for runtime skill execution using async UniTask and cancellation support.
    /// Handles the complete lifecycle of a skill: initialization, execution, cancellation, and cleanup.
    /// Derived classes implement the specific logic via OnSkillStart, OnSkillUpdate, and OnSkillExit.
    /// </summary>
    /// <typeparam name="T">Skill data type associated with this runtime behavior.</typeparam>
    public abstract class BaseSkillRuntime<T> : BaseSkillRuntime where T : BaseSkillDataSo
    {
        #region Inspector & Variables

        /// <summary>
        /// The skill data containing configuration values for this runtime behavior.
        /// </summary>
        protected T skillData;

        /// <summary>
        /// The controller that owns and performs this skill (e.g., player or enemy).
        /// </summary>
        protected BaseController owner;

        /// <summary>
        /// The direction in which the skill should be executed (e.g., toward target).
        /// </summary>
        protected DirectionContainer aimDirection => owner.InputSystem.SightDirection;
        
        
        protected List<StatusEffectDataPayload> effectsApplyOnStart;

        /// <summary>
        /// 
        /// </summary>
        private CancellationTokenSource _cts;

        public override bool IsPerforming { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// Injects the skill data into this runtime behavior.
        /// Called during skill initialization.
        /// </summary>
        /// <param name="skillData">The data asset associated with this runtime skill.</param>
        public override void AssignSkillData(BaseSkillDataSo skillData, BaseController owner)
        {
            this.skillData = skillData as T;
            effectsApplyOnStart = new List<StatusEffectDataPayload>(skillData.StatusEffectOnSkillStart);
            SetCooldown(skillData.Cooldown);
            this.owner = owner;
        }

        /// <summary>
        /// Starts the skill execution process asynchronously.
        /// Binds the owner and direction, creates a new CancellationTokenSource,
        /// and executes the skill logic using a cancellable UniTask.
        /// If the skill has a configured lifetime, it schedules automatic cancellation after the specified duration.
        /// </summary>
        /// <param name="owner">The character executing the skill.</param>
        public override async void PerformSkill()
        {
            if (cooldown > 0) return;
            if (IsPerforming) return;
            
            SetCooldown(skillData.Cooldown);
            _cts = new CancellationTokenSource();
            
            HandleSkillStart();
            try
            {
                await OnSkillUpdate(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                //Debug.LogWarning($"{GetType()} was cancelled");
            }
            HandleSkillExit();
        }
        
        /// <summary>
        /// Cancels the currently running skill if active.
        /// Can be called manually or automatically after a delay (e.g., lifetime expiry).
        /// </summary>
        /// <param name="milliSecondDelay">Optional delay in milliseconds before canceling.</param>
        [Button]
        public override async void CancelSkill(int milliSecondDelay = 0)
        {
            await UniTask.Delay(milliSecondDelay);
            if (IsPerforming) return;
            _cts?.Cancel();
        }

        /// <summary>
        /// Handles pre-execution logic and sets the skill as currently performing.
        /// Triggers the OnSkillStart phase.
        /// </summary>
        protected virtual void HandleSkillStart()
        {
            IsPerforming = true;
            OnSkillStart();
            StatusEffectManager.ApplyEffectTo(owner.gameObject, effectsApplyOnStart);
        }

        /// <summary>
        /// Handles post-execution logic and resets the performing flag.
        /// Triggers the OnSkillExit phase.
        /// </summary>
        protected virtual void HandleSkillExit()
        {
            IsPerforming = false;
            OnSkillExit();
        }
        
        #endregion

        #region Abstract Methods

        /// <summary>
        /// Called immediately before the skill logic begins.
        /// Use this to play animations, disable input, etc.
        /// </summary>
        protected abstract void OnSkillStart();

        /// <summary>
        /// Coroutine containing the main execution logic for the skill (e.g., dashing, charging).
        /// Can yield over time or wait for conditions.
        /// </summary>
        /// <returns>Coroutine IEnumerator controlling skill duration or logic.</returns>
        protected abstract UniTask OnSkillUpdate(CancellationToken cancelToken);

        /// <summary>
        /// Called once the skill finishes execution.
        /// Use this for re-enabling control or stopping effects.
        /// </summary>
        protected abstract void OnSkillExit();

        #endregion
    }
}
