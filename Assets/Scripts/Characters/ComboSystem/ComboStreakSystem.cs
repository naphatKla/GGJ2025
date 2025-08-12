using System;
using Characters.SO.ComboStreakDataSO;
using Manager;
using UnityEngine;

namespace Characters.ComboSystem
{
    public enum FlowStageLevel { None = 0, I = 1, II = 2 }

    public class ComboStreakSystem : MonoBehaviour, IFixedUpdateable
    {
        [SerializeField] private ComboStreakDataSo data;
        public ComboStreakDataSo Data => data;

        // ===== State =====
        public int CurrentStreak { get; private set; }
        public int RewardStreak => Mathf.Min(CurrentStreak, data.maxRewardStreak);
        
        public float CurrentBoostMultiplierX { get; private set; }

        public FlowStageLevel CurrentStage { get; private set; } = FlowStageLevel.None;
        public bool IsStageIICooldown => _stageIICooldownTimer > 0f;

        private float _streakTimer;
        private float _stageIITimer;
        private float _stageIICooldownTimer;
        private int _stageIIHealCounter;

        // ===== Events =====
        public event Action<int> OnStreakChanged;
        public event Action<float> OnStreakTimerTick;
        
        /// <summary>ส่งเป็นตัวคูณ x เช่น 0..5</summary>
        public event Action<float> OnBoostChanged;

        public event Action<FlowStageLevel> OnFlowStageEnter;
        public event Action<FlowStageLevel> OnFlowStageExit;

        // Bridge ไป Stats/Move/Dash/Heal/UI
        public event Action<int> OnStageI_DamageFlatGranted;
        /// <summary>(damageAddPct01, speedAddPct01, dashDeltaSec)</summary>
        public event Action<float, float, float> OnStageII_ModifiersGranted;
        public event Action OnStageII_HealTrigger;

        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable() => FixedUpdateManager.Instance.Unregister(this);

        // ===== Public API =====
        public void EndRunReset()
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
            _streakTimer = data.streakTimeoutSeconds;

            if (CurrentStage == FlowStageLevel.II)
            {
                _stageIIHealCounter++;
                if (_stageIIHealCounter >= data.stageIIHealEveryNStreaks)
                {
                    _stageIIHealCounter = 0;
                    OnStageII_HealTrigger?.Invoke();
                }
            }

            RecomputeBoost(alsoApplyPenalty: false, extraPenaltyX: 0f);
            TryEnterStages();
        }

        public void OnPlayerHit()
        {
            // ลดสตรีค (ถ้าไม่ได้ immunity จาก Stage II)
            if (CurrentStage != FlowStageLevel.II)
            {
                float reduce01 = data.onHitStreakReducePercent / 100f; // เช่น 0.20
                int reduced = Mathf.FloorToInt(CurrentStreak * (1f - reduce01));
                SetStreak(reduced);
            }

            // ลด Boost จาก Max ตาม % ที่กำหนด
            float maxX = data.maxBoostPercent / 100f; // เช่น 500% -> 5x
            float penaltyX = (data.onHitBoostPenaltyPercentOfMax / 100f) * maxX;
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
            float perStreakX = data.boostPerStreakPercent / 100f;  // 10% -> 0.1x/สตรีค
            float maxX = data.maxBoostPercent / 100f;               // 500% -> 5x

            float baseX = RewardStreak * perStreakX;                // x ที่ได้จาก streak
            if (alsoApplyPenalty) baseX -= extraPenaltyX;           // หัก penalty เป็นหน่วย x

            CurrentBoostMultiplierX = Mathf.Clamp(baseX, 0f, maxX);
            OnBoostChanged?.Invoke(CurrentBoostMultiplierX);
        }

        private void TryEnterStages()
        {
            if (CurrentStage == FlowStageLevel.None && CurrentStreak >= data.stageIThreshold)
                EnterStageI();

            if (CurrentStage != FlowStageLevel.II
                && !IsStageIICooldown
                && CurrentStreak >= data.stageIIThreshold)
                EnterStageII();
        }

        private void EnterStageI()
        {
            CurrentStage = FlowStageLevel.I;
            OnFlowStageEnter?.Invoke(FlowStageLevel.I);
            OnStageI_DamageFlatGranted?.Invoke(data.stageIDamageFlat);
        }

        private void ExitStageIIfBelowThreshold()
        {
            if (CurrentStage == FlowStageLevel.I && CurrentStreak < data.stageIThreshold)
                ExitCurrentStageIfAny();
        }

        private void EnterStageII()
        {
            if (CurrentStage == FlowStageLevel.I)
                OnFlowStageExit?.Invoke(FlowStageLevel.I);

            CurrentStage = FlowStageLevel.II;
            _stageIITimer = data.stageIIDuration;
            _stageIIHealCounter = 0;
            OnFlowStageEnter?.Invoke(FlowStageLevel.II);

            // ส่งค่าเป็น 0..1 (เช่น +50% = 0.5)
            OnStageII_ModifiersGranted?.Invoke(
                data.stageIIDamagePercent / 100f,
                data.stageIISpeedPercent / 100f,
                data.stageIIDashDurationDeltaSeconds
            );
        }

        private void ExitStageII()
        {
            SetStreak(Mathf.Max(0, CurrentStreak - data.stageIIEndReduceStreak));
            OnFlowStageExit?.Invoke(FlowStageLevel.II);

            CurrentStage = FlowStageLevel.None;
            _stageIITimer = 0f;
            _stageIICooldownTimer = data.stageIICooldownSeconds;

            if (CurrentStreak >= data.stageIThreshold)
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
                    _stageIICooldownTimer = data.stageIICooldownSeconds;
            }

            OnFlowStageExit?.Invoke(stage);
        }
    }
}