using System;
using System.Linq;
using Characters.Controllers;
using Characters.SO.ComboStreakDataSO;
using Characters.SO.ComboStreakDataSO.StageDataSO;
using Manager;
using UnityEngine;

namespace Characters.ComboSystem
{
    public class ComboStreakSystem : MonoBehaviour, IFixedUpdateable
    {
        private ComboStreakDataSo data;
        public ComboStreakDataSo Data => data;

        private BaseController owner;

        private float _totalExpMultiplier;
        private int _totalExpMultiplierModifyTime;
        public string HighestRank => data.killGrades[_highestGradeIndex].label;
        public int HighestStreakCount { get; private set; }
        public float AverageExpMultiplier => _totalExpMultiplier / _totalExpMultiplierModifyTime;

        // ===== Runtime State =====
        public int KillCount { get; private set; }
        public int CurrentStreak { get; private set; }
        public int RewardStreak => Mathf.Min(CurrentStreak, data.maxRewardStreak);

        /// <summary>ตัวคูณดรอป EXP (x0..xMax)</summary>
        public float CurrentBoostMultiplierX { get; private set; }

        public string CurrentGrade { get; private set; } = null;
        private int _highestGradeIndex;

        // Shared combo timer
        private float comboTimer;

        // Stage runtime
        private BaseComboStageSo activeStage; // stage ปัจจุบัน (exclusive)
        private int activeTierIndex = -1; // index ของ tier ปัจจุบัน (-1 = unstage)
        private int highestTierReached = -1; // เก็บไว้เผื่อใช้ภายหลัง (ไม่ผูก logic แล้ว)

        // Final-stage runtime controls
        private float finalStageTimer;
        public int FlowStageI = 0;
        public int FlowStageII = 0;

        private bool IsInFinalStage =>
            data.stageTiers != null &&
            data.stageTiers.Count > 0 &&
            activeTierIndex == data.stageTiers.Count - 1;

        private bool FreezeComboTimer =>
            IsInFinalStage && data.finalStageFreezeComboTime;

        private bool PreventStreakDecrease =>
            IsInFinalStage && data.finalStagePreventStreakDecrease;

        // ===== Events =====
        public event Action<int> OnKillComboChanged;
        public event Action<string> OnGradeChanged;
        public event Action<int> OnStreakChanged;
        public event Action<float, float> OnTimerTick;
        public event Action<float> OnBoostChanged;
        public event Action<BaseComboStageSo> OnStageEnter;
        public event Action<int, int, int> OnStageUpdate; // (currentStageMin, currentStreak, nextStageMin)
        public event Action<BaseComboStageSo> OnStageExit;
        public event Action OnBerserkEnter;
        public event Action OnBerserkExit;
        public event Action<string, float, float, float> OnStageEffect;

        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable() => FixedUpdateManager.Current?.Unregister(this);

        public void AssignData(BaseController ownerCtrl, ComboStreakDataSo so)
        {
            owner = ownerCtrl;
            data = so;
            ResetAll();
        }

        public void ResetAll()
        {
            KillCount = 0;
            CurrentStreak = 0;
            comboTimer = 0f;

            ForceExitStage(); // เคลียร์ stage + final timer
            highestTierReached = -1;
            activeTierIndex = -1;
            
            // แจ้ง unstage เพื่อ sync UI
            OnStageEnter?.Invoke(null);

            RecomputeBoost(false, 0f);
            EvaluateGrade();
            PushStageUpdate();
        }

        // ===== Public API =====

        public void OnEnemyKilled()
        {
            // KillCount ไม่มีเพดาน
            KillCount++;
            OnKillComboChanged?.Invoke(KillCount);
            EvaluateGrade();

            // +1 Streak (จะคำนวณและสลับ stage ข้างใน)
            SetStreak(CurrentStreak + 1);

            // รีเซ็ตเวลาคอมโบร่วม
            ResetComboTimerToMax();

            // อัปเดตตัวคูณดรอปตามสตรีค
            RecomputeBoost(false, 0f);

            // แจ้ง UI ช่วง
            PushStageUpdate();
        }

        public void OnPlayerHit(bool isTakeDamage)
        {
            if (!isTakeDamage) return;

            // final stage: กันสตรีคลด
            if (PreventStreakDecrease)
            {
                ApplyBoostPenaltyFromHit();
                PushStageUpdate();
                return;
            }

            // ลดสตรีคตาม %
            float reduce01 = data.onHitStreakReducePercent / 100f;
            int reduced = Mathf.FloorToInt(CurrentStreak * (1f - reduce01));

            // SetStreak จะจัดการสลับ stage ให้อัตโนมัติ
            SetStreak(reduced);

            // ลดบูสต์ตามสัดส่วนของ Max
            ApplyBoostPenaltyFromHit();

            PushStageUpdate();
        }

        public void ReduceStreakBy(int amount)
        {
            SetStreak(Mathf.Max(0, CurrentStreak - Mathf.Max(0, amount)));
            PushStageUpdate();
        }

        public void ResetComboTimerToMax() => comboTimer = data.comboTimeoutSeconds;

        public void EmitStageEffect(string key, float v1 = 0, float v2 = 0, float v3 = 0)
            => OnStageEffect?.Invoke(key, v1, v2, v3);

        /// <summary>บังคับจบสเตจสุดท้าย (หรือสเตจใดๆ) โดย Manager เช่นจบบ้าเลือด</summary>
        public void EndFinalStage()
        {
            // ออกจากสเตจ (จะยิง OnBerserkExit ถ้าอยู่ final)
            ForceExitStage();

            // ลดสตรีคตามกำหนด
            if (data.finalStageExitReduceStreak > 0)
                SetStreak(Mathf.Max(0, CurrentStreak - data.finalStageExitReduceStreak));

            // reset progression ถ้าต้องการ (ยังเก็บตัวแปรไว้แม้ logic หลักไม่ใช้แล้ว)
            if (data.finalStageResetStageProgression)
            {
                highestTierReached = -1;
                // หมายเหตุ: เราไม่ force activeTierIndex = -1 ที่นี่ เพราะ SetStreak ด้านบนจะประเมินให้
            }

            // PushStageUpdate เรียกใน SetStreak แล้ว
        }

        // ===== Loop =====
        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Final stage auto-exit
            if (IsInFinalStage && data.finalStageAutoExitSeconds > 0f)
            {
                finalStageTimer -= dt;
                if (finalStageTimer <= 0f)
                {
                    EndFinalStage();
                }
            }

            // Shared Combo Timer
            if (!FreezeComboTimer)
            {
                if (CurrentStreak > 0 || KillCount > 0)
                {
                    comboTimer -= dt;
                    if (comboTimer <= 0f)
                    {
                        // คอมโบหมดเวลา → รีเซ็ตทุกอย่าง
                        KillCount = 0;
                        OnKillComboChanged?.Invoke(KillCount);

                        SetStreak(0); // จะจัดการ unstage ให้อัตโนมัติ
                        ForceExitStage(); // เผื่อกรณีมี stage อยู่ (กันซ้ำซ้อน)

                        highestTierReached = -1;

                        // แจ้ง unstage เพื่อให้ UI sync กรณี activeTierIndex เป็น -1 อยู่แล้ว
                        OnStageEnter?.Invoke(null);

                        RecomputeBoost(false, 0f);
                        EvaluateGrade();
                        PushStageUpdate();
                    }
                }
            }

            OnTimerTick?.Invoke(Mathf.Max(0f, comboTimer), data.comboTimeoutSeconds);
        }

        // ===== Internals =====

        private void SetStreak(int value)
        {
            int clamped = Mathf.Clamp(value, 0, data.maxRewardStreak);
            if (clamped == CurrentStreak) return;

            CurrentStreak = clamped;
            
            if (HighestStreakCount < CurrentStreak)
                HighestStreakCount = CurrentStreak;
            
            OnStreakChanged?.Invoke(CurrentStreak);

            // อัปเดตบูสต์ทันทีเมื่อสตรีคเปลี่ยน
            RecomputeBoost(false, 0f);

            // คำนวณช่วงสเตจจากสตรีคปัจจุบัน แล้วสลับถ้าต่าง
            EvaluateAndApplyStageForCurrentStreak();
        }

        private void EvaluateAndApplyStageForCurrentStreak()
        {
            var tiers = data.stageTiers;
            int targetTierIdx = -1;

            if (tiers != null && tiers.Count > 0)
            {
                // หา tier สูงสุดที่ minStreak <= CurrentStreak
                for (int i = tiers.Count - 1; i >= 0; --i)
                {
                    if (CurrentStreak >= tiers[i].minStreak)
                    {
                        targetTierIdx = i;
                        break;
                    }
                }
            }

            // ถ้า target ไม่ต่างจากปัจจุบัน → แค่ push UI ก็พอ
            if (targetTierIdx == activeTierIndex)
            {
                PushStageUpdate();
                return;
            }

            // ออกจาก stage เดิมถ้ามี
            if (activeStage != null)
                ForceExitStage();
            else
                finalStageTimer = 0f; // เผื่อ safety

            // ---- เข้าสเตจใหม่ ----
            activeTierIndex = targetTierIdx;

            // unstage (ไม่มี tier หรืออยู่ต่ำกว่า tier แรก)
            if (targetTierIdx < 0)
            {
                activeStage = null;
                OnStageEnter?.Invoke(null); // แจ้ง UI ว่า unstage
                PushStageUpdate();
                return;
            }

            // ดึง stage (ยอมให้เป็น null ได้ → ถือเป็น unstage official)
            var s = tiers[targetTierIdx].stage;
            activeStage = s;

            OnStageEnter?.Invoke(s);
            if (s != null)
                s.OnEnter(new StageContext(this, CurrentStreak));

            // ถ้าเป็นการ "เข้า" final stage → ตั้งพฤติกรรมพิเศษ
            if (IsInFinalStage)
            {
                if (data.finalStageResetComboTimerOnEnter)
                    ResetComboTimerToMax();

                finalStageTimer = data.finalStageAutoExitSeconds > 0f
                    ? data.finalStageAutoExitSeconds
                    : 0f;

                OnBerserkEnter?.Invoke();
            }

            // บันทึก highest ไว้เผื่อใช้ฟีเจอร์อื่นต่อไป (ไม่บังคับ)
            if (activeTierIndex > highestTierReached)
                highestTierReached = activeTierIndex;

            PushStageUpdate();
        }

        private void RecomputeBoost(bool alsoApplyPenalty, float extraPenaltyX)
        {
            float perStreakX = data.boostPerStreakPercent / 100f; // 10% -> 0.1x ต่อสตรีค
            float maxX = data.maxBoostPercent / 100f; // 500% -> x5

            float baseX = RewardStreak * perStreakX;
            if (alsoApplyPenalty) baseX -= extraPenaltyX;

            CurrentBoostMultiplierX = Mathf.Clamp(baseX, 0f, maxX);

            _totalExpMultiplier += CurrentBoostMultiplierX;
            _totalExpMultiplierModifyTime++;
            OnBoostChanged?.Invoke(CurrentBoostMultiplierX);
        }

        private void ApplyBoostPenaltyFromHit()
        {
            float maxX = data.maxBoostPercent / 100f;
            float penaltyX = (data.onHitBoostPenaltyPercentOfMax / 100f) * maxX;
            RecomputeBoost(true, penaltyX);
        }

        private void EvaluateGrade()
        {
            string best = null;
            int bestMin = int.MinValue;
            int bestIndex = -1;

            foreach (var g in data.killGrades)
            {
                if (KillCount >= g.minKillCount && g.minKillCount >= bestMin)
                {
                    best = g.label;
                    bestMin = g.minKillCount;
                    bestIndex = data.killGrades.IndexOf(g);
                }
            }

            if (best != CurrentGrade)
            {
                if (bestIndex != -1 && bestIndex > _highestGradeIndex)
                    _highestGradeIndex = bestIndex;
                
                CurrentGrade = best;
                OnGradeChanged?.Invoke(CurrentGrade);
            }
        }

        private void EnterStageTier_legacy(int tierIndex)
        {
            // (เก็บไว้เผื่ออ้างอิง แต่ไม่ใช้แล้ว)
            highestTierReached = Mathf.Max(highestTierReached, tierIndex);
            activeTierIndex = tierIndex;

            var tier = data.stageTiers[tierIndex];
            var s = tier.stage;

            activeStage = s;
            OnStageEnter?.Invoke(s);
            s?.OnEnter(new StageContext(this, CurrentStreak));
        }

        private void ForceExitStage()
        {
            if (activeStage == null)
            {
                finalStageTimer = 0f;
                activeTierIndex = -1;
                return;
            }

            if (IsInFinalStage)
                OnBerserkExit?.Invoke();

            var exiting = activeStage;

            activeStage = null;
            activeTierIndex = -1;
            finalStageTimer = 0f;

            exiting.OnExit(new StageContext(this, CurrentStreak));
            OnStageExit?.Invoke(exiting);
        }

        private void PushStageUpdate()
        {
            var tiers = data.stageTiers;
            if (tiers == null || tiers.Count == 0)
            {
                // ไม่มี tier เลย: ส่งช่วง [0, Current, 0] ก็ได้ แต่ส่วนใหญ่ UI จะซ่อนไปแล้ว
                OnStageUpdate?.Invoke(0, CurrentStreak, 0);
                return;
            }

            var currentMin = activeTierIndex == -1 ? 0 : tiers[activeTierIndex].minStreak;
            int nextIndex = activeTierIndex + 1;
            var nextMin = nextIndex >= tiers.Count ? currentMin : tiers[nextIndex].minStreak;

            OnStageUpdate?.Invoke(currentMin, CurrentStreak, nextMin);
        }
    }
}