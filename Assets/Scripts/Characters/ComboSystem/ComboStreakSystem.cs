using System;
using Characters.Controllers;
using Characters.SO.ComboStreakDataSO;
using GameControl.Controller;
using Manager;
using UnityEngine;

namespace Characters.ComboSystem
{
    public enum FlowStageLevel
    {
        None = 0,
        I = 1,
        II = 2
    }

    public class ComboStreakSystem : MonoBehaviour, IFixedUpdateable
    {
        private ComboStreakDataSo _data;
        public ComboStreakDataSo Data => _data;

        private BaseController _owner;

        // ===== State =====
        public int CurrentStreak { get; private set; }
        public int RewardStreak => Mathf.Min(CurrentStreak, _data.maxRewardStreak);

        public float CurrentBoostMultiplierX { get; private set; }

        public FlowStageLevel CurrentStage { get; private set; } = FlowStageLevel.None;
        public bool IsStageIICooldown => _stageIICooldownTimer > 0f;

        private float _streakTimer;
        private float _stageIITimer;
        private float _stageIICooldownTimer;

        // ===== Events =====
        public event Action<int> OnStreakChanged;
        public event Action<float> OnStreakTimerTick;

        /// <summary>ส่งเป็นตัวคูณ x เช่น 0..5</summary>
        public event Action<float> OnBoostChanged;
        public event Action<FlowStageLevel> OnFlowStageEnter;
        public event Action<FlowStageLevel> OnFlowStageExit;
        

        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable() => FixedUpdateManager.Instance.Unregister(this);

        // ===== Public API =====
        public void AssignData(BaseController owner, ComboStreakDataSo data)
        {
            _owner = owner;
            _data = data;
        }

        public void ResetComboSystem()
        {
            ExitCurrentStageIfAny(force: true);
            SetStreak(0);
            _streakTimer = 0f;
            _stageIICooldownTimer = 0f;
            RecomputeBoost(alsoApplyPenalty: false, extraPenaltyX: 0f);
        }

        public void OnEnemyKilled()
        {
            SetStreak(CurrentStreak + 1);
            _streakTimer = _data.streakTimeoutSeconds;
            RecomputeBoost(alsoApplyPenalty: false, extraPenaltyX: 0f);
            TryEnterStages();
        }

        public void OnPlayerHit()
        {
            // ลดสตรีค (ถ้าไม่ได้ immunity จาก Stage II)
            if (CurrentStage != FlowStageLevel.II)
            {
                float reduce01 = _data.onHitStreakReducePercent / 100f; // เช่น 0.20
                int reduced = Mathf.FloorToInt(CurrentStreak * (1f - reduce01));
                SetStreak(reduced);
            }

            // ลด Boost จาก Max ตาม % ที่กำหนด
            float maxX = _data.maxBoostPercent / 100f; // เช่น 500% -> 5x
            float penaltyX = (_data.onHitBoostPenaltyPercentOfMax / 100f) * maxX;
            RecomputeBoost(alsoApplyPenalty: true, extraPenaltyX: penaltyX);
        }

        // ===== Loop =====
        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Streak Timer (ยกเว้นตอนอยู่ Stage II)
            if (CurrentStreak > 0 && CurrentStage != FlowStageLevel.II)
            {
                _streakTimer -= dt;
                OnStreakTimerTick?.Invoke(Mathf.Max(0f, _streakTimer));

                if (_streakTimer <= 0f)
                {
                    SetStreak(0);
                    RecomputeBoost(false, 0f);
                    ExitStageIIfBelowThreshold();
                }
            }

            // Stage II running
            if (CurrentStage == FlowStageLevel.II)
            {
                _stageIITimer -= dt;
                if (_stageIITimer <= 0f)
                    ExitStageII();
            }

            // Stage II cooldown
            if (_stageIICooldownTimer > 0f)
            {
                _stageIICooldownTimer -= dt;
                if (_stageIICooldownTimer < 0f) _stageIICooldownTimer = 0f;
            }
        }

        // ===== Internals =====
        private void SetStreak(int value)
        {
            int clamped = Mathf.Max(0, value);
            if (clamped == CurrentStreak) return;

            CurrentStreak = clamped;
            OnStreakChanged?.Invoke(CurrentStreak);
            Debug.Log("Set Streak!");
        }

        private void RecomputeBoost(bool alsoApplyPenalty, float extraPenaltyX)
        {
            float perStreakX = _data.boostPerStreakPercent / 100f; // 10% -> 0.1x/สตรีค
            float maxX = _data.maxBoostPercent / 100f; // 500% -> 5x

            float baseX = RewardStreak * perStreakX; // x ที่ได้จาก streak
            if (alsoApplyPenalty) baseX -= extraPenaltyX; // หัก penalty เป็นหน่วย x

            CurrentBoostMultiplierX = Mathf.Clamp(baseX, 0f, maxX);
            OnBoostChanged?.Invoke(CurrentBoostMultiplierX);

            if (_owner is PlayerController)
            {
                SpawnerStateController.Instance.EnemySpawnerController.ExpDropMultiplier = CurrentBoostMultiplierX;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void TryEnterStages()
        {
            if (CurrentStage == FlowStageLevel.None && CurrentStreak >= _data.stageIThreshold)
                EnterStageI();

            if (CurrentStage != FlowStageLevel.II
                && !IsStageIICooldown
                && CurrentStreak >= _data.stageIIThreshold)
                EnterStageII();
        }

        private void EnterStageI()
        {
            CurrentStage = FlowStageLevel.I;
            OnFlowStageEnter?.Invoke(FlowStageLevel.I);
        }

        private void ExitStageIIfBelowThreshold()
        {
            if (CurrentStage == FlowStageLevel.I && CurrentStreak < _data.stageIThreshold)
                ExitCurrentStageIfAny();
        }

        private void EnterStageII()
        {
            if (CurrentStage == FlowStageLevel.I)
                OnFlowStageExit?.Invoke(FlowStageLevel.I);

            CurrentStage = FlowStageLevel.II;
            _stageIITimer = _data.stageIIDuration;
            OnFlowStageEnter?.Invoke(FlowStageLevel.II);
        }

        private void ExitStageII()
        {
            SetStreak(Mathf.Max(0, CurrentStreak - _data.stageIIEndReduceStreak));
            OnFlowStageExit?.Invoke(FlowStageLevel.II);

            CurrentStage = FlowStageLevel.None;
            _stageIITimer = 0f;
            _stageIICooldownTimer = _data.stageIICooldownSeconds;

            if (CurrentStreak >= _data.stageIThreshold)
                EnterStageI();

            RecomputeBoost(false, 0f);
        }

        private void ExitCurrentStageIfAny(bool force = false)
        {
            if (CurrentStage == FlowStageLevel.None) return;

            var stage = CurrentStage;
            CurrentStage = FlowStageLevel.None;

            if (stage == FlowStageLevel.II)
            {
                _stageIITimer = 0f;
                if (!force)
                    _stageIICooldownTimer = _data.stageIICooldownSeconds;
            }

            OnFlowStageExit?.Invoke(stage);
        }
    }
}