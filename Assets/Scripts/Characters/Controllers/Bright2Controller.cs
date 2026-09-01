using System;
using System.Collections.Generic;
using Characters.HeathSystems;
using Characters.SO.CharacterDataSO;
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
        [InfoBox("THIS is Bright2's real skill loadout - the Primary/Secondary on Bright2Data are "
                 + "only a starting point and get overwritten as soon as character data is assigned. "
                 + "Order in each list is the slot: [0] Primary, [1] Secondary, [2+] Auto (no limit).")]
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

        [Title("Debug")]
        [PropertyTooltip("Log every skill this boss performs, plus phase and Break Point events.")]
        [SerializeField] private bool debugLog = true;

        [PropertyTooltip("Phase the boss starts the game in. 0 = normal (starts at phase 1). The Phase "
                         + "buttons below only work during play - CurrentPhase is runtime state and resets "
                         + "when you enter play mode, so set this instead to start a run in a later phase.")]
        [MinValue(0)]
        [SerializeField] private int debugStartPhase;

        [PropertyTooltip("One row per skill this boss owns across all phases. Untick to stop that skill "
                         + "from being cast - checked at the moment of casting, so it takes effect on the "
                         + "very next attempt, live in play mode, with no re-assign or restart.")]
        [LabelText("Skill Switches")]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false,
            ShowPaging = false, ShowItemCount = false, Expanded = true)]
        [SerializeField] private List<SkillSwitch> skillSwitches = new();

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

            if (SkillSystem != null)
            {
                SkillSystem.ExternalPerformFilter = IsSkillAllowed;
                SkillSystem.OnSkillPerform += LogSkillPerform;
            }

            ApplyPullImmunity();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (HealthSystem != null)
                HealthSystem.OnTakeDamage -= EvaluatePhase;

            if (SkillSystem != null)
            {
                SkillSystem.ExternalPerformFilter = null;
                SkillSystem.OnSkillPerform -= LogSkillPerform;
            }
        }

        private void Start()
        {
            // Wire Break Point system (ถ้ามี attach อยู่บน prefab) — Bright2 ต้องมีเสมอ
            _breakPointSystem = GetComponent<BreakPointSystem>();
            if (_breakPointSystem != null)
            {
                _breakPointSystem.AssignBreakPointData(this, maxBreakPoint);
                _breakPointSystem.OnBreakPointChanged += LogBreakPointChanged;
                _breakPointSystem.OnBreakingStart += LogBreakingStart;
                _breakPointSystem.OnBreakingEnd += LogBreakingEnd;
            }
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

        #region Debug

        /// <summary>One row of the skill switch list. Plain serialized data, so the Inspector can edit it.</summary>
        [Serializable]
        public class SkillSwitch
        {
            /// <summary>Re-bound by RefreshSkillSwitches - never serialized, so it survives no domain reload.</summary>
            [NonSerialized, HideInInspector]
            public Bright2Controller owner;

            [HorizontalGroup("Row"), HideLabel, ReadOnly]
            public BaseSkillDataSo skill;

            [HorizontalGroup("Row", Width = 60), LabelText("On"), LabelWidth(22)]
            public bool enabled = true;

            /// <summary>Fires this skill right now, ignoring cooldown and range. Play mode only.</summary>
            [Button("Cast"), HorizontalGroup("Row", Width = 55)]
            private void Cast() => owner?.DebugCastSkill(skill);
        }

        /// <summary>
        /// Keeps one switch per skill used by any phase, preserving whatever is already toggled. Runs in
        /// the editor on validate and again whenever the phase loadout is applied, so the list always
        /// matches what the boss can actually cast.
        /// </summary>
        private void RefreshSkillSwitches()
        {
            skillSwitches ??= new List<SkillSwitch>();
            if (phaseConfigs == null) return;

            skillSwitches.RemoveAll(entry => entry == null || !entry.skill);

            // owner is not serialized, so it must be handed back to every row after any reload.
            foreach (var entry in skillSwitches)
                entry.owner = this;

            foreach (var config in phaseConfigs)
            {
                if (config?.allowedSkills == null) continue;

                foreach (var skill in config.allowedSkills)
                {
                    if (!skill) continue;
                    if (skillSwitches.Exists(entry => entry.skill == skill)) continue;

                    skillSwitches.Add(new SkillSwitch { owner = this, skill = skill, enabled = true });
                }
            }
        }

        private void OnValidate() => RefreshSkillSwitches();

        public bool IsSkillDisabled(BaseSkillDataSo skillData)
        {
            if (!skillData || skillSwitches == null) return false;

            var entry = skillSwitches.Find(e => e != null && e.skill == skillData);
            return entry != null && !entry.enabled;
        }

        public void SetSkillDisabled(BaseSkillDataSo skillData, bool disabled)
        {
            if (!skillData) return;
            RefreshSkillSwitches();

            var entry = skillSwitches.Find(e => e != null && e.skill == skillData);
            if (entry != null) entry.enabled = !disabled;
        }

        /// <summary>Skills already reported as switched off, so the per-frame check logs once, not forever.</summary>
        private readonly HashSet<BaseSkillDataSo> _blockedReported = new();

        private bool IsSkillAllowed(BaseSkillDataSo skillData)
        {
            if (!IsSkillDisabled(skillData))
            {
                _blockedReported.Remove(skillData);
                return true;
            }

            // The enemy state asks every frame, so this would flood the console - report each skill once.
            if (debugLog && _blockedReported.Add(skillData))
                Debug.Log($"[Bright2] '{SkillLabel(skillData)}' is switched OFF - not casting it", this);

            return false;
        }

        private void LogSkillPerform(BaseSkillDataSo skillData, int slotIndex)
        {
            if (!debugLog) return;
            Debug.Log($"[Bright2] Phase {CurrentPhase} | PERFORM '{SkillLabel(skillData)}' (slot {slotIndex})", this);
        }

        private void LogBreakPointChanged(float current, float max)
        {
            if (!debugLog) return;
            Debug.Log($"[Bright2] Break Point {current:0}/{max:0}", this);
        }

        private void LogBreakingStart()
        {
            if (!debugLog) return;
            Debug.Log("[Bright2] BREAKING - skills cancelled, stun + amplified damage incoming", this);
        }

        private void LogBreakingEnd()
        {
            if (!debugLog) return;
            Debug.Log("[Bright2] Breaking ended - Break Point restored", this);
        }

        private static string SkillLabel(BaseSkillDataSo skillData)
        {
            if (!skillData) return "<null>";
            return string.IsNullOrEmpty(skillData.SkillName) ? skillData.name : skillData.SkillName;
        }

        /// <summary>Blocks every skill of the current phase except the one passed in - the fast way to isolate one.</summary>
        /// <summary>
        /// Forces a skill to perform immediately: clears its cooldown, then calls the runtime directly so
        /// the Activate Radius and the enemy state's own distance gate are both bypassed. Only the skills
        /// of the CURRENT phase have a runtime - jump phases first to reach the others.
        /// </summary>
        public void DebugCastSkill(BaseSkillDataSo skillData)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Bright2] Cast only works in play mode - skill runtimes exist at runtime only.", this);
                return;
            }

            if (!skillData || SkillSystem == null) return;

            var runtime = SkillSystem.GetSkillRuntimeOrDefault(skillData);
            if (!runtime)
            {
                Debug.LogWarning($"[Bright2] '{SkillLabel(skillData)}' is not assigned in Phase {CurrentPhase} - "
                                 + "jump to the phase that owns it first.", this);
                return;
            }

            if (runtime.IsPerforming)
            {
                Debug.Log($"[Bright2] '{SkillLabel(skillData)}' is already performing", this);
                return;
            }

            runtime.SetCurrentCooldown(0);
            runtime.ClearGlobalCooldown();
            runtime.PerformSkill();

            // PerformSkill refuses silently on several conditions, so verify instead of claiming success.
            // Cooldown was just zeroed: if it is counting again the skill definitely ran - that also covers
            // instant skills (the self-buffs) which are no longer "performing" by the time we look.
            bool started = runtime.IsPerforming || runtime.IsCooldown;

            if (started)
                Debug.Log($"[Bright2] Forced cast '{SkillLabel(skillData)}'", this);
            else
                Debug.LogWarning($"[Bright2] '{SkillLabel(skillData)}' refused to start - check that its "
                                 + "switch is On.", this);
        }

        [ButtonGroup("PhaseJump")]
        [Button("Phase 1")]
        private void DebugPhase1() => DebugForcePhase(1);

        [ButtonGroup("PhaseJump")]
        [Button("Phase 2")]
        private void DebugPhase2() => DebugForcePhase(2);

        [ButtonGroup("PhaseJump")]
        [Button("Phase 3")]
        private void DebugPhase3() => DebugForcePhase(3);

        /// <summary>
        /// Jumps straight to a phase and re-applies its loadout, ignoring the "same phase does nothing"
        /// guard in <see cref="SetPhase"/> so a phase can also be re-applied or stepped back down while
        /// testing. Note that <see cref="EvaluatePhase"/> still runs on the next hit, so stepping DOWN
        /// only sticks while the boss's HP is above that phase's threshold.
        /// </summary>
        private void DebugForcePhase(int phase)
        {
            if (phaseConfigs == null || phaseConfigs.Count == 0) return;

            CurrentPhase = Mathf.Clamp(phase, 1, phaseConfigs.Count);
            ApplySkillsForPhase(CurrentPhase);
            OnPhaseChanged?.Invoke(CurrentPhase);

            Debug.Log($"[Bright2] Forced Phase {CurrentPhase} - skills re-applied", this);
        }

        [Button, PropertyTooltip("Switches every other skill off, leaving only this one castable.")]
        public void DebugIsolateSkill(BaseSkillDataSo skillToKeep)
        {
            RefreshSkillSwitches();

            int blocked = 0;
            foreach (var entry in skillSwitches)
            {
                entry.enabled = entry.skill == skillToKeep;
                if (!entry.enabled) blocked++;
            }

            Debug.Log($"[Bright2] Isolated '{SkillLabel(skillToKeep)}' - switched off {blocked} other skill(s)", this);
        }

        [Button]
        private void DebugEnableAllSkills()
        {
            RefreshSkillSwitches();
            foreach (var entry in skillSwitches)
                entry.enabled = true;

            Debug.Log("[Bright2] All skills enabled", this);
        }

        #endregion

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

            RefreshSkillSwitches();
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

        /// <summary>
        /// CharacterData only carries the phase-1 Primary and Secondary. The rest of a phase's loadout
        /// lives in <c>phaseConfigs</c>, and nothing applies it on its own - <c>ApplySkillsForPhase</c>
        /// otherwise runs only when the phase changes, which leaves phase 1 permanently short of its
        /// auto skills. Hooking it here covers every way this boss gets its data: placed in the scene,
        /// pulled from the pool, or assigned by the spawner.
        /// <para/>
        /// Overridden on Bright2 alone - no other enemy's assignment path is touched.
        /// </summary>
        public override void AssignCharacterData(BaseCharacterDataSo data)
        {
            base.AssignCharacterData(data);

            if (debugStartPhase > 0 && phaseConfigs != null && phaseConfigs.Count > 0)
            {
                CurrentPhase = Mathf.Clamp(debugStartPhase, 1, phaseConfigs.Count);
                if (debugLog)
                    Debug.Log($"[Bright2] Debug Start Phase - beginning at phase {CurrentPhase}", this);
            }

            ApplySkillsForPhase(CurrentPhase);
        }

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
