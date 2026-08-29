using System;
using System.Collections.Generic;
using Characters.HeathSystems;
using Characters.SO.SkillDataSo;
using Characters.SkillSystems;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.Controllers
{
    /// <summary>
    /// Controller ของบอส "Bright2" (Design Ver 0.0.20) — "A Living Black Hole"
    /// <para/>
    /// ความสามารถหลักที่เพิ่มจาก EnemyController ปกติ:
    /// <list type="bullet">
    /// <item><b>Boss Phase system</b> — เปลี่ยน phase 1→2→3 ตาม %HP (threshold ตั้งค่าได้ใน Inspector)
    /// และ gate ว่า skill ไหนใช้ได้ในแต่ละ phase ผ่านการ re-assign skill เข้า SkillSystem</item>
    /// <item><b>Break Point integration</b> — auto-wire <see cref="BreakPointSystem"/> ที่ attach อยู่บน prefab</item>
    /// </list>
    /// <para/>
    /// Skill slot mapping เมื่อเข้าสู่ phase ใหม่ (เรียงตาม list):
    /// index 0 → Primary, index 1 → Secondary, index 2+ → Auto
    /// (Bright2: Devourer=Primary, The Immovable=Secondary, ที่เหลือเป็น Auto ตาม phase)
    /// </summary>
    public class Bright2Controller : EnemyController
    {
        #region Inspector & Variables

        /// <summary>Config ของแต่ละ phase — index 0 = Phase 1 (ไม่มี threshold เพราะเริ่มเกมมาอยู่แล้ว)</summary>
        [Serializable]
        public class PhaseConfig
        {
            [PropertyTooltip("เข้าสู่ phase นี้เมื่อ HP% ลดมาถึง <= ค่านี้")]
            [Unit(Units.Percent)]
            public float hpThresholdPercentage;

            [PropertyTooltip("Skill ที่ใช้ได้ใน phase นี้ — index 0 = Primary, 1 = Secondary, 2+ = Auto")]
            public List<BaseSkillDataSo> allowedSkills = new();
        }

        [Title("Boss Phase Configs")]
        [PropertyTooltip("Phase 1 อยู่ที่ index 0 (ไม่ใช้ threshold) — เรียง phase 2, 3 ตามลำดับ")]
        [SerializeField] private List<PhaseConfig> phaseConfigs = new()
        {
            new PhaseConfig { hpThresholdPercentage = 100 },
            new PhaseConfig { hpThresholdPercentage = 66 },
            new PhaseConfig { hpThresholdPercentage = 33 },
        };

        [Title("Break Point (Bright2 = 100 ตาม spec)")]
        [SerializeField] private float maxBreakPoint = 100f;

        [Title("Movement")]
        [PropertyTooltip("Bright2 is the heaviest mass in the game - nothing drags it around. "
                         + "Turn this off and a Black Hole map event pulls the boss toward itself far "
                         + "faster than Devourer pulls the Black Hole in, because map event pulls have "
                         + "no speed cap.")]
        [SerializeField] private bool immuneToExternalPull = true;

        /// <summary>Phase ปัจจุบัน (1-based). Phase ไม่มีทางย้อนกลับ แม้จะถูก heal</summary>
        [ShowInInspector, ReadOnly]
        public int CurrentPhase { get; private set; } = 1;

        /// <summary>(newPhase) — ไว้ให้ UI/VFX/BGM ฟังตอนบอสเปลี่ยน phase</summary>
        public event Action<int> OnPhaseChanged;

        private BreakPointSystem _breakPointSystem;

        #endregion

        #region Unity Methods

        protected override void OnEnable()
        {
            base.OnEnable();

            // จับตอนบอส "โดนดาเมจจริง" เพื่อเช็คว่าควรขยับ phase หรือยัง
            // (ใช้ OnTakeDamage เพราะ OnHealthChange ส่งแค่ delta ไม่รู้ % ปัจจุบันที่แท้จริง)
            if (HealthSystem != null)
                HealthSystem.OnTakeDamage += EvaluatePhase;

            ApplyPullImmunity();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (HealthSystem != null)
                HealthSystem.OnTakeDamage -= EvaluatePhase;
        }

        private void Start()
        {
            // Wire Break Point system (ถ้ามี attach อยู่บน prefab) — Bright2 ต้องมีเสมอ
            _breakPointSystem = GetComponent<BreakPointSystem>();
            if (_breakPointSystem != null)
                _breakPointSystem.AssignBreakPointData(this, maxBreakPoint);
            else
                Debug.LogWarning($"[Bright2Controller] No BreakPointSystem found on {name} — boss will have no Break Point!");
        }

        #endregion

        /// <summary>
        /// Bright2 is the heaviest mass in the game - external gravity effects don't move it. Re-applied on
        /// enable and on reset because the flag lives on the runtime movement system, not in the prefab.
        /// </summary>
        private void ApplyPullImmunity()
        {
            if (MovementSystem)
                MovementSystem.IgnoreExternalPull = immuneToExternalPull;
        }

        #region Phase System

        /// <summary>บังคับเปลี่ยน phase ด้วยมือ (debug/test ได้จาก Inspector)</summary>
        [Button]
        public void SetPhase(int newPhase)
        {
            newPhase = Mathf.Clamp(newPhase, 1, Mathf.Max(1, phaseConfigs.Count));
            if (newPhase == CurrentPhase && Application.isPlaying) return;

            CurrentPhase = newPhase;
            ApplySkillsForPhase(CurrentPhase);
            OnPhaseChanged?.Invoke(CurrentPhase);

            Debug.Log($"[Bright2] Enter Phase {CurrentPhase} ({name})");
        }

        /// <summary>เรียกทุกครั้งที่โดน actual damage — เลื่อน phase ขึ้นเมื่อ HP% ต่ำกว่า threshold</summary>
        private void EvaluatePhase()
        {
            if (HealthSystem == null || HealthSystem.IsDead) return;
            if (_breakPointSystem != null && _breakPointSystem.IsBreaking) return; // รอหลุด Breaking ก่อน

            float hpPercent = HealthSystem.HealthPercentage01 * 100f;

            // ไล่จาก phase สูงสุดลงมา เจอ threshold แรกที่ HP% ต่ำกว่า = phase เป้าหมาย
            int targetPhase = 1;
            for (int i = phaseConfigs.Count - 1; i >= 1; i--)
            {
                if (phaseConfigs[i] == null || phaseConfigs[i].allowedSkills == null) continue;
                if (hpPercent <= phaseConfigs[i].hpThresholdPercentage)
                {
                    targetPhase = i + 1;
                    break;
                }
            }

            // Phase ขึ้นเท่านั้น — กันกรณี heal แล้ว HP% กลับมาสูง
            if (targetPhase > CurrentPhase)
                SetPhase(targetPhase);
        }

        /// <summary>
        /// Re-assign skill ตาม phase:
        /// 1) Reset กลับไป default (จาก CharacterDataSo) — ซึ่งยัง cancel สกิลที่กำลัง perform ด้วย
        /// 2) ใส่ skill ของ phase นี้ทับ: index 0 = Primary, 1 = Secondary, 2+ = Auto
        /// (SetOrAddSkill จะ set cooldown เริ่มต้นเป็น 0 ให้ใช้ได้ทันที)
        /// </summary>
        private void ApplySkillsForPhase(int phase)
        {
            if (phaseConfigs == null || phaseConfigs.Count == 0) return;
            var config = phaseConfigs[Mathf.Clamp(phase - 1, 0, phaseConfigs.Count - 1)];
            if (config?.allowedSkills == null || config.allowedSkills.Count == 0)
            {
                // ยังไม่ได้ตั้งค่า skill ของ phase นี้ → ใช้ default จาก CharacterDataSo ต่อไป
                return;
            }

            SkillSystem.ResetSkillSystem();

            for (int i = 0; i < config.allowedSkills.Count; i++)
            {
                var skill = config.allowedSkills[i];
                if (!skill) continue;

                SkillType type = i switch
                {
                    0 => SkillType.PrimarySkill,
                    1 => SkillType.SecondarySkill,
                    _ => SkillType.AutoSkill,
                };

                SkillSystem.SetOrAddSkill(skill, type);
            }
        }

        #endregion

        #region Overrides

        public override void ResetAllDependentBehavior()
        {
            base.ResetAllDependentBehavior();

            // Respawn/reset → กลับ phase 1 และเคลียร์ Break Point
            ApplyPullImmunity();
            _breakPointSystem?.ResetBreakPointSystem();
            CurrentPhase = 1;
            ApplySkillsForPhase(1);
            OnPhaseChanged?.Invoke(1);
        }

        public override void CancelBehaviorOnDead()
        {
            base.CancelBehaviorOnDead();
            _breakPointSystem?.ResetBreakPointSystem();
        }

        #endregion
    }
}
