using System;
using Characters.Controllers;
using Characters.SO.ComboStreakDataSO;
using Characters.SO.ComboStreakDataSO.StageDataSO;
using Manager;
using UnityEngine;

namespace Characters.ComboSystem
{
    /// <summary>
    /// - แยก KillCount vs Streak
    /// - ใช้ comboTimer ร่วมกัน
    /// - Stage เป็นแบบ tier จาก ComboStreakDataSo.stageTiers (minStreak -> stage)
    /// - แต่ละ tier "ทริกเกอร์ครั้งเดียว" ต่อรอบคอมโบ; ออกด้วยคำสั่งจากระบบ (ไม่มี duration ในสเตจ)
    /// - สเตจสุดท้ายคุม behavior (Freeze timer / Prevent streak decrease / Exit penalty / Auto-exit) ผ่าน Manager เท่านั้น
    /// </summary>
    public class ComboStreakSystem : MonoBehaviour, IFixedUpdateable
    {
        private ComboStreakDataSo data;
        public ComboStreakDataSo Data => data;

        private BaseController owner;

        // ===== Runtime State =====
        public int KillCount { get; private set; }
        public int CurrentStreak { get; private set; }
        public int RewardStreak => Mathf.Min(CurrentStreak, data.maxRewardStreak);

        /// <summary>ตัวคูณดรอป EXP (x0..xMax)</summary>
        public float CurrentBoostMultiplierX { get; private set; }

        public string CurrentGrade { get; private set; } = "D";

        // ตัวจับเวลาคอมโบ (ใช้ร่วมกันทั้ง KillCount และ Streak)
        private float comboTimer;

        // Stage runtime (ระบบแบบ tier)
        private BaseComboStageSo activeStage; // สเตจที่กำลังทำงาน (exclusive)
        private int activeTierIndex = -1; // index ของ tier ที่กำลัง Active (-1 = ไม่มี)
        private int highestTierReached = -1; // index สูงสุดของ tier ที่ "เคยเข้าแล้ว" ในรอบคอมโบนี้

        // Final-stage runtime controls (คุมโดย Manager เท่านั้น)
        private float finalStageTimer; // ใช้เฉพาะถ้ากำหนด auto-exit > 0

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

        /// <summary>(currentTime, maxTime)</summary>
        public event Action<float, float> OnTimerTick;

        /// <summary>x0..x (เช่น 0..5)</summary>
        public event Action<float> OnBoostChanged;

        public event Action<BaseComboStageSo> OnStageEnter;

        /// <summary>
        /// UI แถบขั้น: (currentStageMinOrCandidate, currentStreak, nextStageMinOrMinus1)
        /// </summary>
        public event Action<int, int, int> OnStageUpdate;

        public event Action<BaseComboStageSo> OnStageExit;

        public event Action OnBerserkEnter;
        public event Action OnBerserkExit;

        /// <summary>ช่องทางส่งสัญญาณให้ระบบอื่น (Stats/FX/Heal ฯลฯ)</summary>
        public event Action<string, float, float, float> OnStageEffect;

        private void OnEnable() => FixedUpdateManager.Instance.Register(this);
        private void OnDisable() => FixedUpdateManager.Instance.Unregister(this);

        public void AssignData(BaseController ownerCtrl, ComboStreakDataSo so)
        {
            owner = ownerCtrl;
            data = so;

            // OnStageEnter += s => Debug.Log($"[Combo] Enter Stage: {s.displayName}");
            // OnStageExit  += s => Debug.Log($"[Combo] Exit  Stage: {s.displayName}");
            // OnGradeChanged += s => Debug.Log(s);
            // OnKillCountChanged += i => Debug.Log($"Kill : {i}");
            // OnStageUpdate += (i, i1, arg3) => Debug.Log($"currentThreshould: {i}, currentStreak: {i1}, nexThreshold: {arg3}");
            // OnBerserkEnter += () => Debug.Log("BerserkEnter");
            // OnBerserkExit += () => Debug.Log("BerserkExit");

            ResetAll();
        }

        public void ResetAll()
        {
            KillCount = 0;
            SetStreak(0);
            comboTimer = 0f;

            ForceExitStage(); // รวมเคลียร์ final timer
            highestTierReached = -1;
            activeTierIndex = -1;

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

            // +1 Streak
            SetStreak(CurrentStreak + 1);

            // รีเซ็ตเวลาคอมโบร่วม
            ResetComboTimerToMax();

            // อัปเดตตัวคูณดรอปตามสตรีค
            RecomputeBoost(false, 0f);

            // ประเมิน tier (เข้า/อัปเกรด) ตามลิสต์ใน Data
            EvaluateTierProgression();

            // แจ้ง UI
            PushStageUpdate();
        }

        public void OnPlayerHit(bool isTakeDamage)
        {
            if (!isTakeDamage) return;
            
            // สเตจสุดท้ายคุม "ห้ามลดสตรีค" โดย Manager
            if (PreventStreakDecrease)
            {
                ApplyBoostPenaltyFromHit();
                PushStageUpdate();
                return;
            }

            // ลดสตรีคตาม %
            float reduce01 = data.onHitStreakReducePercent / 100f;
            int reduced = Mathf.FloorToInt(CurrentStreak * (1f - reduce01));
            SetStreak(reduced);

            // ลดบูสต์ 10% ของ Max 
            ApplyBoostPenaltyFromHit();

            // ไม่ออกรุ่นสเตจอัตโนมัติจากการโดนตี (ออกด้วย Manager เท่านั้น)
            PushStageUpdate();
        }

        /// <summary>ให้ระบบอื่นสั่งลดสตรีคโดยตรง</summary>
        public void ReduceStreakBy(int amount)
        {
            SetStreak(Mathf.Max(0, CurrentStreak - Mathf.Max(0, amount)));
            PushStageUpdate();
        }

        public void ResetComboTimerToMax() => comboTimer = data.comboTimeoutSeconds;

        public void EmitStageEffect(string key, float v1 = 0, float v2 = 0, float v3 = 0)
            => OnStageEffect?.Invoke(key, v1, v2, v3);

        /// <summary>สั่งจบสเตจสุดท้าย (หรือสเตจใดๆ) ด้วย Manager: ใช้ตอนจบ Berserk</summary>
        public void EndFinalStage()
        {
            if (!IsInFinalStage) return;

            // ออกจากสเตจ
            ForceExitStage();

            // ลดสตรีคตามที่กำหนด
            if (data.finalStageExitReduceStreak > 0)
                ReduceStreakBy(data.finalStageExitReduceStreak);

            // รีเซ็ต progression เพื่อให้ผู้เล่น "วนเก็บใหม่"
            if (data.finalStageResetStageProgression)
            {
                highestTierReached = -1;
                activeTierIndex = -1;
            }

            PushStageUpdate();
        }

        // ===== Loop =====
        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // ----- Final stage auto-exit (คุมเวลาโดย Manager เท่านั้น) -----
            if (IsInFinalStage && data.finalStageAutoExitSeconds > 0f)
            {
                finalStageTimer -= dt;
                if (finalStageTimer <= 0f)
                {
                    EndFinalStage();
                }
            }

            // ----- Shared Combo Timer -----
            if (!FreezeComboTimer) // แช่เวลาเฉพาะตอนอยู่ Final และเปิด flag
            {
                if (CurrentStreak > 0 || KillCount > 0)
                {
                    comboTimer -= dt;
                    if (comboTimer <= 0f)
                    {
                        // คอมโบหมดเวลา → รีเซ็ตทุกอย่าง
                        KillCount = 0;
                        OnKillComboChanged?.Invoke(KillCount);
                        SetStreak(0);

                        ForceExitStage();
                        highestTierReached = -1;
                        activeTierIndex = -1;

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
            OnStreakChanged?.Invoke(CurrentStreak);

            // อัปเดตบูสต์ทันทีเมื่อสตรีคเปลี่ยน
            RecomputeBoost(false, 0f);
        }

        private void RecomputeBoost(bool alsoApplyPenalty, float extraPenaltyX)
        {
            float perStreakX = data.boostPerStreakPercent / 100f; // 10% -> 0.1x ต่อสตรีค
            float maxX = data.maxBoostPercent / 100f; // 500% -> x5

            float baseX = RewardStreak * perStreakX;
            if (alsoApplyPenalty) baseX -= extraPenaltyX;

            CurrentBoostMultiplierX = Mathf.Clamp(baseX, 0f, maxX);
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
            if (data.killGrades == null || data.killGrades.Count == 0)
            {
                if (CurrentGrade != "D")
                {
                    CurrentGrade = "D";
                    OnGradeChanged?.Invoke(CurrentGrade);
                }

                return;
            }

            string best = "D";
            int bestMin = int.MinValue;

            foreach (var g in data.killGrades)
            {
                if (KillCount >= g.minKillCount && g.minKillCount >= bestMin)
                {
                    best = g.label;
                    bestMin = g.minKillCount;
                }
            }

            if (best != CurrentGrade)
            {
                CurrentGrade = best;
                OnGradeChanged?.Invoke(CurrentGrade);
            }
        }

        /// <summary>
        /// Tier progression:
        /// - Trigger ทีละขั้นเมื่อ CurrentStreak >= tier.minStreak และ tier นั้นยังไม่เคยเข้าในคอมโบนี้
        /// - ถ้าแตะหลายขั้นด้วยการขึ้นทีละสตรีค ระบบจะอัปเกรดตามลำดับ (Exit เดิม -> Enter ใหม่)
        /// - ไม่ออกเพราะสตรีคลดลง (ออกเฉพาะ Reset หรือคำสั่งจาก Manager: EndFinalStage)
        /// - เมื่อเข้าขั้นสุดท้าย: รีเซ็ตคอมโบไทเมอร์เป็น max และเริ่มจับเวลาสำหรับ auto-exit (ถ้ากำหนด)
        /// </summary>
        private void EvaluateTierProgression()
        {
            if (data.stageTiers == null || data.stageTiers.Count == 0) return;

            // next tier คือ tier ถัดจากที่เคยเข้าไปสูงสุดแล้ว
            int nextTierIdx = highestTierReached + 1;
            if (nextTierIdx >= data.stageTiers.Count) return; // ถึงสุดแล้ว

            var nextTier = data.stageTiers[nextTierIdx];
            if (nextTier.stage == null) return;

            if (CurrentStreak >= nextTier.minStreak)
            {
                // อัปเกรด: ออกจากสเตจเดิมก่อน (ถ้ามี)
                if (activeStage != null)
                    ForceExitStage();

                EnterStageTier(nextTierIdx);

                // ถ้าคือขั้นสุดท้าย → จัดการ behavior จาก Manager
                if (!IsInFinalStage) return;
                if (data.finalStageResetComboTimerOnEnter)
                    ResetComboTimerToMax();

                finalStageTimer = data.finalStageAutoExitSeconds > 0f
                    ? data.finalStageAutoExitSeconds
                    : 0f;
                    
                OnBerserkEnter?.Invoke();
            }
        }

        private void EnterStageTier(int tierIndex)
        {
            highestTierReached = tierIndex;
            activeTierIndex = tierIndex;

            var tier = data.stageTiers[tierIndex];
            var s = tier.stage;

            activeStage = s;
            OnStageEnter?.Invoke(s);
            s.OnEnter(new StageContext(this, CurrentStreak));
        }

        private void ForceExitStage()
        {
            if (activeStage == null)
            {
                finalStageTimer = 0f;
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

        /// <summary>
        /// ส่งค่าอัปเดต UI แบบ “ช่วง” ซ้อนทับกัน:
        /// [0 -> T0], [T0 -> T1], ..., [T(n-2) -> T(n-1)]
        /// หากตอนนี้อยู่สเตจสุดท้ายแล้ว จะไม่ส่งอัปเดต
        /// </summary>
        private void PushStageUpdate()
        {
            var tiers = data.stageTiers;
            if (tiers == null || tiers.Count == 0) return;

            var currentMin = activeTierIndex == -1? 0 : tiers[activeTierIndex].minStreak;
            int nextIndex = activeTierIndex + 1;
            var nextMin = nextIndex >= tiers.Count ? currentMin : tiers[nextIndex].minStreak;

            OnStageUpdate?.Invoke(currentMin, CurrentStreak, nextMin);
        }
    }
}