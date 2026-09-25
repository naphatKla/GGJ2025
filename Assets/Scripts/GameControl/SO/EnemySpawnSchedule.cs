using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Challenge;
using Sirenix.OdinInspector;
using UI.Milestone;
using UnityEngine;

namespace GameControl.SO
{
    /// <summary>
    /// How a map decides which enemies to spawn and when. Only <see cref="Conditions"/> existed before - it is
    /// the default, so every map made before this was added keeps its exact behaviour.
    /// </summary>
    public enum EnemySpawnMode
    {
        [Tooltip("ระบบเดิม: สุ่มศัตรูตาม Chance / Enemy Point / Spawn Conditions")]
        Conditions = 0,

        [Tooltip("ไล่ลิสต์ศัตรูตามลำดับ ทีละ Step วินาที")]
        Sequential = 1,
    }

    /// <summary>Which enemies the old random spawner may still pick while a schedule is running (Mix With Conditions).</summary>
    public enum RandomSpawnerPool
    {
        [Tooltip("ตัวสุ่มเดิมหยิบได้ทุกตัวเหมือนปกติ")]
        AllMapEnemies = 0,

        [Tooltip("ตัวสุ่มเดิมหยิบเฉพาะศัตรูที่อยู่ในตาราง")]
        OnlyScheduledEnemies = 1,

        [Tooltip("ตัวสุ่มเดิมไม่หยิบศัตรูที่อยู่ในตาราง (ตารางคุมตัวพวกนั้นเอง)")]
        ExcludeScheduledEnemies = 2,
    }

    public enum SpawnIntervalMode
    {
        [Tooltip("Same timer the random spawner uses (Default Enemy Spawn Timer, shrinking with the map's growth).")]
        MapSpawnTimer = 0,

        [Tooltip("A fixed number of seconds.")]
        Custom = 1,

        [Tooltip("A new random value between Min and Max after every wave.")]
        RandomRange = 2,
    }

    public enum ScheduledSpawnPosition
    {
        [Tooltip("Same place the random spawner uses (outside the camera).")]
        Default = 0,

        [Tooltip("A ring around the player - Min/Max radius.")]
        AroundPlayer = 1,
    }

    /// <summary>
    /// Everything about spawning ONE enemy type from a schedule, except when it starts/stops - that part
    /// differs between a timed rule (<see cref="EnemySpawnRule"/>) and a sequence entry
    /// (<see cref="SequentialSpawnEntry"/>).
    /// </summary>
    [Serializable]
    public abstract class EnemySpawnRuleBase
    {
        private const string G = "$" + nameof(FoldoutTitle);

        #region Enemy

        [FoldoutGroup(G), PropertyOrder(-1)]
        [InfoBox("กฎนี้ยังไม่ได้เลือกศัตรู จะถูกข้ามตอนเล่น", InfoMessageType.Warning,
            "@this.enabled && this.HasNoEnemy")]
        [InfoBox("กฎนี้ปิดอยู่ (Enabled = ปิด) จะไม่ถูกใช้ทั้งตอนเล่นและใน Preview", InfoMessageType.None, "@!this.enabled")]
        [GUIColor("@this.enabled ? Color.green : Color.red")]
        [LabelText("Enabled")]
        [Tooltip("เปิด = ใช้กฎนี้\nปิด = เก็บกฎไว้แต่ไม่ใช้ (ไม่เกิดตอนเล่น และไม่แสดงใน Preview)")]
        public bool enabled = true;

        [FoldoutGroup(G), PropertyOrder(1)]
        // Unity's own popup, not Odin's ValueDropdown: Odin's popup window calls a Unity internal
        // (ContainerWindow.FitWindowRectToScreen) that this Unity version no longer has, so it throws on click.
        [CustomValueDrawer(nameof(DrawEnemyIdField))]
        [ValidateInput(nameof(ValidateEnemyId), "ศัตรูตัวนี้ไม่มีใน Enemy Options ของแมพ จะถูกข้ามตอนเล่น")]
        [LabelText("Enemy")]
        [Tooltip("ศัตรูที่จะให้เกิด เลือกจาก Enemy Setting > Enemy Options ของแมพ\n"
                 + "ค่าอื่นของศัตรู (pool, Per-Enemy Max, Spawn Effects, Enemy Data) ใช้จากตัวเลือกนั้นทั้งหมด\n"
                 + "ถ้าขึ้นว่า (not in map) แปลว่า id นี้ไม่มีในแมพ")]
        public string enemyId;

        [FoldoutGroup(G), PropertyOrder(2)]
        [LabelText("Note")]
        [Tooltip("โน้ตสำหรับ designer จะแสดงที่หัว foldout ไม่มีผลกับเกม\nเช่น \"บอสกลางเกม\" หรือ \"คลื่นเปิดฉาก\"")]
        public string note;

        #endregion

        #region Interval

        [FoldoutGroup(G), PropertyOrder(20)]
        [Title("Interval", "ระยะห่างระหว่างแต่ละคลื่น")]
        [EnumToggleButtons, HideLabel]
        [Tooltip("Map Spawn Timer = ใช้ตัวจับเวลาเดียวกับตัวสุ่มเดิมของแมพ (เร็วขึ้นเรื่อยๆ ตาม growth ของแมพ)\n"
                 + "Custom = ทุกๆ กี่วินาที ค่าคงที่\n"
                 + "Random Range = สุ่มระหว่าง Min–Max ใหม่ทุกคลื่น")]
        public SpawnIntervalMode intervalMode = SpawnIntervalMode.MapSpawnTimer;

        [FoldoutGroup(G), PropertyOrder(21)]
        [ShowIf(nameof(intervalMode), SpawnIntervalMode.Custom)]
        [Unit(Units.Second), MinValue(0.05f)]
        [LabelText("Every")]
        [Tooltip("เกิด 1 คลื่นทุกกี่วินาที เช่น 20 = ทุก 20 วินาที")]
        public float customInterval = 10f;

        [FoldoutGroup(G), PropertyOrder(22)]
        [ShowIf(nameof(intervalMode), SpawnIntervalMode.RandomRange)]
        [InfoBox("Min มากกว่า Max ระบบจะสลับให้เองตอนเล่น", InfoMessageType.Warning, "@this.intervalMin > this.intervalMax")]
        [Unit(Units.Second), MinValue(0.05f)]
        [LabelText("Min")]
        [Tooltip("ระยะห่างสั้นที่สุดระหว่างคลื่น (วินาที)")]
        public float intervalMin = 5f;

        [FoldoutGroup(G), PropertyOrder(23)]
        [ShowIf(nameof(intervalMode), SpawnIntervalMode.RandomRange)]
        [Unit(Units.Second), MinValue(0.05f)]
        [LabelText("Max")]
        [Tooltip("ระยะห่างยาวที่สุดระหว่างคลื่น (วินาที)")]
        public float intervalMax = 15f;

        [FoldoutGroup(G), PropertyOrder(24)]
        [LabelText("First Wave At Start")]
        [Tooltip("เปิด = คลื่นแรกออกทันทีที่กฎนี้เริ่ม\nปิด = รอให้ครบหนึ่ง interval ก่อนแล้วค่อยออกคลื่นแรก")]
        public bool spawnOnStart = true;

        #endregion

        #region Amount

        [FoldoutGroup(G), PropertyOrder(30)]
        [Title("Amount", "จำนวนศัตรูต่อคลื่น และจำกัดทั้งเกม")]
        [InfoBox("Per Wave (Min) มากกว่า (Max) ระบบจะใช้ค่า Min", InfoMessageType.Warning, "@this.amountMin > this.amountMax")]
        [MinValue(1)]
        [LabelText("Per Wave (Min)")]
        [Tooltip("จำนวนศัตรูต่อคลื่น (ค่าต่ำสุด)\nแต่ละคลื่นสุ่มจำนวนระหว่าง Min–Max ถ้าอยากได้จำนวนคงที่ให้ใส่เท่ากัน")]
        public int amountMin = 1;

        [FoldoutGroup(G), PropertyOrder(31)]
        [MinValue(1)]
        [LabelText("Per Wave (Max)")]
        [Tooltip("จำนวนศัตรูต่อคลื่น (ค่าสูงสุด)\nแต่ละคลื่นสุ่มจำนวนระหว่าง Min–Max")]
        public int amountMax = 1;

        [FoldoutGroup(G), PropertyOrder(32)]
        [ShowIf("@this.amountMax > 1")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Delay Inside Wave")]
        [Tooltip("เว้นระยะระหว่างศัตรูแต่ละตัวในคลื่นเดียวกัน\n0 = ออกมาพร้อมกันทั้งคลื่น\n0.3 = ทยอยออกทีละตัวห่างกัน 0.3 วินาที")]
        public float burstDelay;

        [FoldoutGroup(G), PropertyOrder(33)]
        [InfoBox("Max Total = 1: ศัตรูตัวนี้จะเกิดแค่ครั้งเดียวต่อรอบเกม (เหมาะกับบอส)", InfoMessageType.Info, "@this.maxTotal == 1")]
        [MinValue(0)]
        [LabelText("Max Total")]
        [Tooltip("กฎนี้เกิดศัตรูรวมได้สูงสุดกี่ตัวต่อรอบเกม\n0 = ไม่จำกัด\n1 = เกิดครั้งเดียว (เช่น บอส)\n"
                 + "ครบแล้วกฎนี้หยุดเอง (ใน Sequential ที่เปิด Loop จะนับใหม่ทุกรอบ)")]
        public int maxTotal;

        [FoldoutGroup(G), PropertyOrder(34)]
        [Range(0f, 100f)]
        [LabelText("Wave Chance %")]
        [Tooltip("โอกาสที่แต่ละคลื่นจะเกิดจริง (%)\n100 = เกิดทุกคลื่น\n50 = ครึ่งหนึ่ง\nคลื่นที่ไม่เกิดจะรอ interval ถัดไปตามปกติ")]
        public float chance = 100f;

        #endregion

        #region Rules

        [FoldoutGroup(G), PropertyOrder(40)]
        [Title("Conditions & Position", "เงื่อนไขเพิ่มเติมและตำแหน่งเกิด")]
        [LabelText("Respect Per-Enemy Max")]
        [Tooltip("เปิด = เคารพ Per-Enemy Max ของศัตรูตัวนั้น (จำนวนสูงสุดที่มีชีวิตพร้อมกัน) ถ้าเต็มจะข้ามไป\n"
                 + "ปิด = เกิดได้แม้จะเกิน\nศัตรูจากตารางนับรวมใน Per-Enemy Max เสมอ ตัวสุ่มเดิมจึงเห็นจำนวนที่ถูกต้อง")]
        public bool respectPerEnemyMax = true;

        [FoldoutGroup(G), PropertyOrder(41)]
        [LabelText("Check Spawn Conditions")]
        [Tooltip("เปิด = ต้องผ่าน Spawn Conditions เดิมของศัตรูตัวนั้นด้วย (เช่น Time Window)\n"
                 + "ปิด = ตารางตัดสินเองทั้งหมด ไม่สน Spawn Conditions")]
        public bool checkSpawnConditions;

        [FoldoutGroup(G), PropertyOrder(42)]
        [LabelText("Play Spawn Effects")]
        [Tooltip("เปิด = เล่น Spawn Effects ของศัตรูตัวนั้น (popup, หน่วงเวลา ฯลฯ) เหมือนตอนตัวสุ่มเดิมเกิด\nปิด = เกิดทันทีเงียบๆ")]
        public bool playSpawnEffects = true;

        [FoldoutGroup(G), PropertyOrder(43)]
        [EnumToggleButtons]
        [LabelText("Position")]
        [Tooltip("Default = ตำแหน่งเดียวกับตัวสุ่มเดิม (นอกกล้อง)\nAround Player = วงแหวนรอบตัว player ตาม Radius Min–Max")]
        public ScheduledSpawnPosition spawnPosition = ScheduledSpawnPosition.Default;

        [FoldoutGroup(G), PropertyOrder(44)]
        [ShowIf(nameof(spawnPosition), ScheduledSpawnPosition.AroundPlayer)]
        [InfoBox("Radius Min มากกว่า Max ระบบจะสลับให้เองตอนเล่น", InfoMessageType.Warning, "@this.radiusMin > this.radiusMax")]
        [MinValue(0f)]
        [LabelText("Radius Min")]
        [Tooltip("ระยะใกล้สุดจาก player (หน่วย Unity)\nกล้องเห็นประมาณ 22 หน่วยจากตัว player ถ้าอยากให้เกิดนอกจอใช้ค่ามากกว่านั้น")]
        public float radiusMin = 20f;

        [FoldoutGroup(G), PropertyOrder(45)]
        [ShowIf(nameof(spawnPosition), ScheduledSpawnPosition.AroundPlayer)]
        [MinValue(0f)]
        [LabelText("Radius Max")]
        [Tooltip("ระยะไกลสุดจาก player (หน่วย Unity)")]
        public float radiusMax = 25f;

        #endregion
        #region Helpers

        public bool IsUsable => enabled && !string.IsNullOrWhiteSpace(enemyId);

        private bool HasNoEnemy => string.IsNullOrWhiteSpace(enemyId);

        public int RollAmount() => UnityEngine.Random.Range(Mathf.Max(1, amountMin), Mathf.Max(1, amountMin, amountMax) + 1);

        /// <param name="mapSpawnTimer">Current random-spawner timer (it shrinks as the map grows).</param>
        public float RollInterval(float mapSpawnTimer)
        {
            switch (intervalMode)
            {
                case SpawnIntervalMode.Custom:
                    return Mathf.Max(0.05f, customInterval);
                case SpawnIntervalMode.RandomRange:
                    float min = Mathf.Max(0.05f, Mathf.Min(intervalMin, intervalMax));
                    float max = Mathf.Max(min, Mathf.Max(intervalMin, intervalMax));
                    return UnityEngine.Random.Range(min, max);
                default:
                    return Mathf.Max(0.05f, mapSpawnTimer);
            }
        }

        public string IntervalSummary => intervalMode switch
        {
            SpawnIntervalMode.Custom => $"every {customInterval:0.##}s",
            SpawnIntervalMode.RandomRange => $"every {intervalMin:0.##}-{intervalMax:0.##}s",
            _ => "map timer"
        };

        public string AmountSummary
        {
            get
            {
                string amount = amountMax > amountMin ? $"x{amountMin}-{amountMax}" : $"x{Mathf.Max(1, amountMin)}";
                if (maxTotal > 0) amount += $" (max {maxTotal})";
                if (chance < 100f) amount += $" {chance:0.#}%";
                return amount;
            }
        }

        protected abstract string TimingSummary { get; }

        public string FoldoutTitle
        {
            get
            {
                string id = string.IsNullOrWhiteSpace(enemyId) ? "<no enemy>" : enemyId;
                string title = $"{(enabled ? "" : "[OFF] ")}{id}   |   {TimingSummary}   |   {IntervalSummary} {AmountSummary}";
                return string.IsNullOrWhiteSpace(note) ? title : $"{title}   // {note}";
            }
        }

        /// <summary>
        /// Popup of the map's enemy ids. Falls back to a text field when the map can't be worked out
        /// (e.g. a ChallengeDataSO that no MilestoneContainer lists). An id that is not on the map is kept and
        /// shown as "(not in map)" instead of being silently wiped.
        /// </summary>
        private string DrawEnemyIdField(string value, GUIContent label)
        {
#if UNITY_EDITOR
            string text = label != null ? label.text : "";
            var ids = EnemySpawnEditorLookup.EnemyIdsForSelection();
            if (ids.Count == 0) return UnityEditor.EditorGUILayout.TextField(text, value);

            bool missing = !string.IsNullOrEmpty(value) && !ids.Contains(value);
            var display = new List<string> { "<None>" };
            display.AddRange(ids);
            if (missing) display.Add(value + "  (not in map)");

            int current = string.IsNullOrEmpty(value) ? 0 : missing ? display.Count - 1 : ids.IndexOf(value) + 1;
            int picked = UnityEditor.EditorGUILayout.Popup(text, current, display.ToArray());

            if (picked == current) return value;
            if (picked == 0) return "";
            return picked - 1 < ids.Count ? ids[picked - 1] : value;
#else
            return value;
#endif
        }

        private bool ValidateEnemyId(string id)
        {
            if (!enabled || string.IsNullOrWhiteSpace(id)) return true; // empty is allowed, just skipped
            var ids = EnemySpawnEditorLookup.EnemyIdsForSelection();
            return ids.Count == 0 || ids.Contains(id); // can't tell which map - don't nag
        }

        #endregion
    }

    /// <summary>A rule with its own start / expire time (used by a milestone's Override Map Spawn rules).</summary>
    [Serializable]
    public class EnemySpawnRule : EnemySpawnRuleBase
    {
        private const string G = "$" + nameof(FoldoutTitle);

        [FoldoutGroup(G), PropertyOrder(10)]
        [Title("Timing", "นับเป็นวินาทีตั้งแต่เริ่มรอบเกม")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Start At")]
        [Tooltip("กฎนี้เริ่มเกิดได้ตั้งแต่วินาทีที่เท่าไหร่\n0 = ตั้งแต่เกมเริ่ม\n300 = หลังผ่านไป 5 นาที")]
        public float startAt;

        [FoldoutGroup(G), PropertyOrder(11)]
        [GUIColor("@this.useExpire ? Color.green : Color.red")]
        [LabelText("Use Expire")]
        [Tooltip("เปิด = หยุดเกิดหลังเวลาที่กำหนด (Expire At)\nปิด = เกิดไปเรื่อยๆ จนจบเกม หรือจนระบบ Rush เข้ามาคุม")]
        public bool useExpire;

        [FoldoutGroup(G), PropertyOrder(12)]
        [ShowIf(nameof(useExpire))]
        [InfoBox("Expire At ต้องมากกว่า Start At ไม่งั้นกฎนี้จะไม่เกิดเลย", InfoMessageType.Error,
            "@this.useExpire && this.expireAt <= this.startAt")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Expire At")]
        [Tooltip("หลังวินาทีนี้จะไม่เกิดใหม่อีก ศัตรูที่เกิดไปแล้วยังอยู่ตามปกติ\nเช่น Start At 60 + Expire At 180 = เกิดเฉพาะนาทีที่ 1–3")]
        public float expireAt = 120f;

        protected override string TimingSummary =>
            $"{startAt:0.##}s -> {(useExpire ? $"{expireAt:0.##}s" : "end")}";
    }

    /// <summary>An entry of a Sequential list - its start/stop time comes from its position in the list.</summary>
    [Serializable]
    public class SequentialSpawnEntry : EnemySpawnRuleBase
    {
        private const string G = "$" + nameof(FoldoutTitle);

        [FoldoutGroup(G), PropertyOrder(10)]
        [Title("Timing", "เวลาเริ่มมาจากลำดับในลิสต์ (ดู Preview)")]
        [GUIColor("@this.useCustomDuration ? Color.green : Color.red")]
        [LabelText("Custom Duration")]
        [Tooltip("ปิด = entry นี้ยาวเท่า Step ของลิสต์\nเปิด = ตั้งความยาวของ entry นี้เอง entry ถัดไปจะเลื่อนตามไปด้วย")]
        public bool useCustomDuration;

        [FoldoutGroup(G), PropertyOrder(11)]
        [ShowIf(nameof(useCustomDuration))]
        [Unit(Units.Second), MinValue(1f)]
        [LabelText("Duration")]
        [Tooltip("entry นี้ยาวกี่วินาทีก่อนถึง entry ถัดไป")]
        public float customDuration = 30f;

        protected override string TimingSummary => useCustomDuration ? $"{customDuration:0.##}s slot" : "step slot";
    }

    [Serializable]
    public class SequentialSpawnSettings
    {
        [InfoBox("ไล่ศัตรูตามลำดับในลิสต์: entry แรกเริ่มที่ Start Offset แล้วเปลี่ยนเป็นตัวถัดไปทุก Step วินาที\n"
                 + "ตัวอย่าง Step 30: entry 0 = 0–30 วิ, entry 1 = 30–60 วิ, entry 2 = 60–90 วิ ...\n"
                 + "ดูช่วงเวลาจริงได้ที่ Preview ด้านล่าง")]
        [Unit(Units.Second), MinValue(1f)]
        [LabelText("Step")]
        [Tooltip("แต่ละ entry อยู่นานกี่วินาทีก่อนถึงตัวถัดไป\nentry ที่เปิด Custom Duration ใช้ค่าของตัวเองแทน")]
        public float step = 30f;

        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Start Offset")]
        [Tooltip("entry แรกเริ่มที่วินาทีเท่าไหร่\n0 = ตั้งแต่เกมเริ่ม")]
        public float startOffset;

        [LabelText("Stop Previous")]
        [Tooltip("เปิด = พอถึงตัวถัดไป ตัวก่อนหน้าหยุดเกิด (ส่งไม้ต่อกัน)\n"
                 + "ปิด = สะสม: ตัวที่เริ่มไปแล้วเกิดต่อไปเรื่อยๆ ศัตรูจะเพิ่มชนิดขึ้นตามเวลา")]
        public bool stopPrevious = true;

        [ShowIf("@this.stopPrevious && !this.loop")]
        [LabelText("Last Entry Runs Forever")]
        [Tooltip("เปิด = entry สุดท้ายเกิดต่อไปจนจบเกม\nปิด = entry สุดท้ายหยุดเมื่อหมดเวลา แล้วแมพจะเงียบ (ไม่มีศัตรูจากตาราง)")]
        public bool lastEntryRunsForever = true;

        [LabelText("Loop")]
        [Tooltip("เปิด = เล่นจบลิสต์แล้ววนกลับไป entry แรก (เหมาะกับแมพ Endless)\nMax Total ของแต่ละ entry นับใหม่ทุกรอบ")]
        public bool loop;

        [InfoBox("ยังไม่มี entry แมพนี้จะกลับไปใช้ Conditions (ตัวสุ่มเดิม)", InfoMessageType.Warning, "@this.entries == null || this.entries.Count == 0")]
        [ListDrawerSettings(ShowIndexLabels = true, ShowPaging = false)]
        [LabelText("Entries (in order)")]
        [Tooltip("ลิสต์ศัตรูเรียงตามลำดับเวลา กด + เพื่อเพิ่ม ลากเพื่อเปลี่ยนลำดับ")]
        public List<SequentialSpawnEntry> entries = new();
    }


    /// <summary>A rule with its start/expire already worked out for the current mode.</summary>
    public class ResolvedSpawnRule
    {
        public EnemySpawnRuleBase Rule;
        public float Start;
        /// <summary>&lt; 0 = never expires.</summary>
        public float Expire = -1f;
    }

    /// <summary>
    /// Finds the spawn rules of a map's milestone. Milestones live in <see cref="MilestoneDataContainer"/>
    /// (mapID -> one <see cref="ChallengeDataSO"/> per milestone) - the same lookup the milestone menu uses.
    /// </summary>
    public static class MilestoneSpawnLookup
    {
        public static ChallengeDataSO FindMilestone(MilestoneDataContainer container, string mapId, int milestoneIndex)
        {
            if (container == null || container.milestoneList == null || string.IsNullOrEmpty(mapId)) return null;
            var category = container.milestoneList.Find(c => c != null && c.mapID == mapId);
            if (category?.milestoneEntries == null) return null;
            if (milestoneIndex < 0 || milestoneIndex >= category.milestoneEntries.Count) return null;
            return category.milestoneEntries[milestoneIndex];
        }
    }

    /// <summary>What actually runs this run: the rules plus the random-spawner settings that go with them.</summary>
    public class SpawnScheduleSource
    {
        /// <summary>Where it came from, for logs / debug ("map Sequential", "milestone 2 override (Milestone_lv2)").</summary>
        public string Label;
        public readonly List<ResolvedSpawnRule> Rules = new();
        /// <summary>Sequential + Loop only: length of one pass; 0 otherwise.</summary>
        public float CycleLength;
        /// <summary>Sequential + Loop only: when the first pass starts.</summary>
        public float CycleOffset;
        public bool MixWithConditions;
        public RandomSpawnerPool RandomPool;

        public bool IsEmpty => Rules.Count == 0;

        /// <summary>Random spawner runs while this phase is active: nothing scheduled (= Conditions) or Mix is on.</summary>
        public bool RandomSpawnerOn => IsEmpty || MixWithConditions;
    }

    /// <summary>What a milestone with Override Map Spawn does with the map's own mode.</summary>
    public enum MilestoneSpawnFlow
    {
        [Tooltip("ใช้กฎของ milestone อย่างเดียวทั้งเกม (ไม่สนโหมดของแมพ)")]
        Replace = 0,

        [Tooltip("ใช้กฎของ milestone ก่อน แล้วส่งต่อให้โหมดของแมพ")]
        ThenMapMode = 1,

        [Tooltip("ใช้กฎของ milestone และโหมดของแมพพร้อมกัน")]
        Together = 2,
    }

    public enum MilestoneHandOver
    {
        [Tooltip("ส่งต่อที่วินาทีที่กำหนด (Hand Over At)")]
        AtTime = 0,

        [Tooltip("ส่งต่อเมื่อกฎของ milestone จบครบทุกข้อ (ถึง Expire At หรือครบ Max Total)\nกฎที่ไม่มีทั้งสองอย่างจะไม่มีวันจบ")]
        WhenRulesFinish = 1,
    }

    /// <summary>
    /// The full plan for one run: <see cref="First"/> always runs from the start; <see cref="Second"/> (the map's
    /// mode) either takes over at the hand-over (Then Map Mode) or runs alongside (Together).
    /// </summary>
    public class SpawnSchedulePlan
    {
        public string Label;
        public SpawnScheduleSource First;
        /// <summary>Null = no second phase (map mode alone, or a milestone that Replaces it).</summary>
        public SpawnScheduleSource Second;
        public MilestoneSpawnFlow Flow;
        public MilestoneHandOver HandOver;
        public float HandOverAt;
        public bool SecondStartsAtHandOver = true;

        public bool HasAnyRules => (First != null && !First.IsEmpty) || (Second != null && !Second.IsEmpty);
    }

    /// <summary>Turns spawn settings into a flat list of timed rules. Shared by runtime and previews.</summary>
    public static class EnemySpawnScheduleResolver
    {
        public const string FallbackText = "FALLS BACK TO CONDITIONS (random spawner as before).";

        /// <summary>
        /// Picks what runs this run. The selected milestone takes part only when it has Override Map Spawn ON and
        /// at least one usable rule - then its After Milestone flow decides how the map's own mode joins in.
        /// Otherwise it is the map's own mode alone. A plan with no rules anywhere = behave as Conditions.
        /// </summary>
        public static SpawnSchedulePlan ResolveForRun(MapDataSO map, ChallengeDataSO milestone, int milestoneIndex)
        {
            var mapSource = ResolveMap(map);

            if (milestone != null && milestone.overrideMapSpawn)
            {
                var fromMilestone = ResolveMilestone(milestone);
                fromMilestone.Label = $"milestone {milestoneIndex} ({milestone.name})";

                if (!fromMilestone.IsEmpty)
                {
                    var plan = new SpawnSchedulePlan
                    {
                        First = fromMilestone,
                        Flow = milestone.spawnFlow,
                        HandOver = milestone.spawnHandOver,
                        HandOverAt = Mathf.Max(0f, milestone.spawnHandOverAt),
                        SecondStartsAtHandOver = milestone.spawnMapTimelineStartsAtHandOver
                    };
                    if (plan.Flow != MilestoneSpawnFlow.Replace) plan.Second = mapSource;

                    plan.Label = plan.Flow switch
                    {
                        MilestoneSpawnFlow.ThenMapMode => $"{fromMilestone.Label} then {mapSource.Label}",
                        MilestoneSpawnFlow.Together => $"{fromMilestone.Label} + {mapSource.Label}",
                        _ => $"{fromMilestone.Label} replaces {mapSource.Label}"
                    };
                    return plan;
                }
            }

            return new SpawnSchedulePlan { First = mapSource, Label = mapSource.Label };
        }

        /// <summary>
        /// Latest Expire among the rules when every rule can finish; -1 when some rule never finishes
        /// (no Expire and no Max Total); float.NaN when the finish depends on Max Total (unknown time).
        /// </summary>
        public static float EstimateRulesFinish(SpawnScheduleSource source)
        {
            if (source == null || source.IsEmpty) return 0f;
            float latest = 0f;
            bool unknown = false;
            foreach (var r in source.Rules)
            {
                bool hasExpire = r.Expire >= 0f;
                bool hasMax = r.Rule.maxTotal > 0;
                if (!hasExpire && !hasMax) return -1f;
                if (hasExpire) latest = Mathf.Max(latest, r.Expire);
                else unknown = true;
            }
            return unknown ? float.NaN : latest;
        }

        public static SpawnScheduleSource ResolveMap(MapDataSO map)
        {
            var source = new SpawnScheduleSource { Label = map != null ? $"map {map.enemySpawnMode}" : "no map" };
            if (map == null) return source;

            source.MixWithConditions = map.mixWithConditions;
            source.RandomPool = map.randomSpawnerPool;

            if (map.enemySpawnMode == EnemySpawnMode.Sequential && map.sequentialSpawn != null)
            {
                ResolveSequential(map.sequentialSpawn, source.Rules, out source.CycleLength);
                source.CycleOffset = Mathf.Max(0f, map.sequentialSpawn.startOffset);
            }

            return source;
        }

        public static SpawnScheduleSource ResolveMilestone(ChallengeDataSO milestone)
        {
            var source = new SpawnScheduleSource { Label = milestone != null ? milestone.name : "no milestone" };
            if (milestone == null) return source;

            source.MixWithConditions = milestone.spawnMixWithConditions;
            source.RandomPool = milestone.spawnRandomSpawnerPool;
            ResolveTimed(milestone.spawnRules, source.Rules);
            return source;
        }

        public static void ResolveTimed(IList<EnemySpawnRule> rules, List<ResolvedSpawnRule> result)
        {
            if (rules == null) return;
            foreach (var rule in rules)
            {
                if (rule == null || !rule.IsUsable) continue;
                result.Add(new ResolvedSpawnRule
                {
                    Rule = rule,
                    Start = Mathf.Max(0f, rule.startAt),
                    Expire = rule.useExpire ? Mathf.Max(0f, rule.expireAt) : -1f
                });
            }
        }

        private static void ResolveSequential(SequentialSpawnSettings settings, List<ResolvedSpawnRule> result,
            out float cycleLength)
        {
            cycleLength = 0f;
            if (settings?.entries == null) return;

            var usable = settings.entries.Where(e => e != null && e.IsUsable).ToList();
            float t = Mathf.Max(0f, settings.startOffset);

            for (int i = 0; i < usable.Count; i++)
            {
                var entry = usable[i];
                float duration = Mathf.Max(1f, entry.useCustomDuration ? entry.customDuration : settings.step);
                bool isLast = i == usable.Count - 1;

                float expire = settings.stopPrevious ? t + duration : -1f;
                if (isLast && settings.stopPrevious && settings.lastEntryRunsForever && !settings.loop)
                    expire = -1f;

                result.Add(new ResolvedSpawnRule { Rule = entry, Start = t, Expire = expire });
                t += duration;
            }

            if (settings.loop && usable.Count > 0)
                cycleLength = t - Mathf.Max(0f, settings.startOffset);
        }

        /// <summary>Readable timeline of the map's own mode, for the MapDataSO inspector.</summary>
        public static string BuildMapPreview(MapDataSO map)
        {
            if (map == null) return "";
            if (map.enemySpawnMode == EnemySpawnMode.Conditions)
                return "Conditions mode - the random spawner works exactly as before. Nothing is scheduled.";

            var source = ResolveMap(map);
            if (source.IsEmpty) return "No usable entries -> " + FallbackText;
            return BuildSourcePreview(source, KnownIds(map));
        }

        /// <summary>Readable timeline of a milestone ChallengeDataSO's Spawn Rules and how the map's mode joins in.</summary>
        /// <param name="knownIds">Enemy ids that exist on the milestone's map(s); null/empty = don't warn.</param>
        /// <param name="map">The milestone's map, to preview the map-mode part (null = unknown).</param>
        public static string BuildMilestonePreview(ChallengeDataSO milestone, ICollection<string> knownIds, MapDataSO map)
        {
            if (milestone == null) return "";
            if (!milestone.overrideMapSpawn)
                return "Override Map Spawn is OFF - this milestone uses the map's own Enemy Spawn Mode.";

            var source = ResolveMilestone(milestone);
            if (source.IsEmpty)
                return "Override is ON but there are no usable rules -> this milestone uses the map's own Enemy Spawn Mode.";

            var sb = new StringBuilder();
            switch (milestone.spawnFlow)
            {
                case MilestoneSpawnFlow.Replace:
                    sb.AppendLine("Flow: Replace - only these rules for the whole run.");
                    sb.AppendLine();
                    sb.Append(BuildSourcePreview(source, knownIds));
                    break;

                case MilestoneSpawnFlow.ThenMapMode:
                    string handOver;
                    if (milestone.spawnHandOver == MilestoneHandOver.AtTime)
                        handOver = $"at {Mathf.Max(0f, milestone.spawnHandOverAt):0.#}s";
                    else
                    {
                        float finish = EstimateRulesFinish(source);
                        handOver = finish < 0f ? "NEVER - WARNING: a rule has no Expire At and no Max Total"
                            : float.IsNaN(finish) ? "when every rule is done (depends on Max Total)"
                            : $"when every rule is done (about {finish:0.#}s)";
                    }

                    sb.AppendLine($"Flow: Then Map Mode - hand over {handOver}.");
                    sb.AppendLine();
                    sb.AppendLine("== 1) Milestone rules (until hand-over) ==");
                    sb.AppendLine(BuildSourcePreview(source, knownIds));
                    sb.AppendLine();
                    sb.AppendLine(milestone.spawnMapTimelineStartsAtHandOver
                        ? "== 2) Map mode (its timeline starts at 0 at the hand-over) =="
                        : "== 2) Map mode (real game time - earlier parts are skipped) ==");
                    sb.Append(map != null ? BuildMapPreview(map) : "Map unknown (not in MilestoneContainer).");
                    break;

                case MilestoneSpawnFlow.Together:
                    sb.AppendLine("Flow: Together - these rules AND the map's mode at the same time.");
                    sb.AppendLine();
                    sb.AppendLine("== Milestone rules ==");
                    sb.AppendLine(BuildSourcePreview(source, knownIds));
                    sb.AppendLine();
                    sb.AppendLine("== Map mode (alongside) ==");
                    sb.Append(map != null ? BuildMapPreview(map) : "Map unknown (not in MilestoneContainer).");
                    break;
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildSourcePreview(SpawnScheduleSource source, ICollection<string> knownIds)
        {
            var sb = new StringBuilder();
            sb.AppendLine(source.MixWithConditions
                ? $"Random spawner: ON too ({source.RandomPool})"
                : "Random spawner: OFF (schedule only)");
            sb.AppendLine("Rush: schedule stops, Rush takes over.");
            if (source.CycleLength > 0f) sb.AppendLine($"Loops every {source.CycleLength:0.##}s");
            sb.AppendLine();
            AppendTimeline(sb, source.Rules, source.CycleLength, knownIds);
            return sb.ToString().TrimEnd();
        }

        private static void AppendTimeline(StringBuilder sb, List<ResolvedSpawnRule> rules, float cycle,
            ICollection<string> knownIds)
        {
            var cuts = new SortedSet<float>();
            foreach (var r in rules)
            {
                cuts.Add(r.Start);
                if (r.Expire >= 0f) cuts.Add(r.Expire);
            }

            var points = cuts.ToList();
            for (int i = 0; i < points.Count; i++)
            {
                float from = points[i];
                bool open = i == points.Count - 1;
                if (open && cycle > 0f)
                {
                    sb.AppendLine($"{from,6:0.#}s -> loops back to the first entry");
                    break;
                }
                float to = open ? float.PositiveInfinity : points[i + 1];
                float probe = open ? from + 0.001f : (from + to) * 0.5f;

                var active = rules.Where(r => probe >= r.Start && (r.Expire < 0f || probe < r.Expire))
                    .Select(r => $"{r.Rule.enemyId} {r.Rule.AmountSummary} ({r.Rule.IntervalSummary})")
                    .ToList();

                string range = open ? $"{from,6:0.#}s ->   end" : $"{from,6:0.#}s -> {to,5:0.#}s";
                sb.AppendLine($"{range} | {(active.Count > 0 ? string.Join(", ", active) : "-")}");
            }

            if (knownIds == null || knownIds.Count == 0) return;
            var unknown = rules.Select(r => r.Rule.enemyId).Where(id => !knownIds.Contains(id)).Distinct().ToList();
            if (unknown.Count == 0) return;
            sb.AppendLine();
            sb.AppendLine($"WARNING: not in the map's Enemy Options (will be skipped): {string.Join(", ", unknown)}");
        }

        private static HashSet<string> KnownIds(MapDataSO map) =>
            map?.EnemyOptions?.Where(o => o != null && !string.IsNullOrWhiteSpace(o.id)).Select(o => o.id).ToHashSet()
            ?? new HashSet<string>();
    }

    /// <summary>
    /// Editor-only helpers for the Inspector: which enemy ids a rule may use (from the selected map, or from the
    /// map(s) a selected milestone ChallengeDataSO belongs to) and which milestone asset a map previews.
    /// Everything returns empty in a build.
    /// <para/>
    /// Odin redraws the Inspector many times a second, so nothing here may scan the AssetDatabase per repaint:
    /// the asset lists and every derived answer are cached, refreshed every <see cref="CacheSeconds"/> or right
    /// away when project files change. Previews go through <see cref="Throttled"/>.
    /// </summary>
    public static class EnemySpawnEditorLookup
    {
#if UNITY_EDITOR
        private const double CacheSeconds = 3.0;
        private const double PreviewSeconds = 0.5;

        private static double _cacheTime = double.NegativeInfinity;
        private static List<MilestoneDataContainer> _containers = new();
        private static List<MapDataSO> _maps = new();
        private static readonly Dictionary<string, object> _results = new();
        private static readonly Dictionary<string, (double time, string value)> _throttled = new();

        [UnityEditor.InitializeOnLoadMethod]
        private static void HookInvalidation()
        {
            UnityEditor.EditorApplication.projectChanged += Invalidate;
            UnityEditor.Undo.undoRedoPerformed += Invalidate;
        }

        private static void Invalidate() => _cacheTime = double.NegativeInfinity;

        /// <summary>Reloads the asset lists when stale and drops every derived answer with them.</summary>
        private static void EnsureCache()
        {
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            if (now - _cacheTime < CacheSeconds) return;

            _containers = LoadAll<MilestoneDataContainer>().ToList();
            _maps = LoadAll<MapDataSO>().ToList();
            _results.Clear();
            _throttled.Clear();
            _cacheTime = now;
        }

        private static T Cached<T>(string key, Func<T> build)
        {
            EnsureCache();
            if (_results.TryGetValue(key, out var value) && value is T typed) return typed;
            var result = build();
            _results[key] = result;
            return result;
        }
#endif

        /// <summary>
        /// Rebuilds a read-only Inspector text (a preview) at most twice a second instead of on every repaint.
        /// Outside the editor it simply builds it.
        /// </summary>
        public static string Throttled(UnityEngine.Object owner, string key, Func<string> build)
        {
#if UNITY_EDITOR
            if (owner == null) return build();
            EnsureCache();
            string id = owner.GetInstanceID() + ":" + key;
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            if (_throttled.TryGetValue(id, out var entry) && now - entry.time < PreviewSeconds) return entry.value;
            string value = build();
            _throttled[id] = (now, value);
            return value;
#else
            return build();
#endif
        }

        /// <summary>Enemy ids for whatever is open in the Inspector (MapDataSO or milestone ChallengeDataSO).</summary>
        public static List<string> EnemyIdsForSelection()
        {
#if UNITY_EDITOR
            var sel = UnityEditor.Selection.activeObject;
            if (sel == null) return new List<string>();

            return Cached("sel:" + sel.GetInstanceID(), () =>
            {
                if (sel is MapDataSO map) return IdsOf(map).ToList();
                if (sel is ChallengeDataSO challenge) return EnemyIdsForMapIds(MapIdsForMilestone(challenge)).ToList();
                return new List<string>();
            });
#else
            return new List<string>();
#endif
        }

        /// <summary>Map ids this milestone belongs to: MilestoneContainer first, then its Auto Selected / Map Filter lists.</summary>
        public static HashSet<string> MapIdsForMilestone(ChallengeDataSO challenge)
        {
#if UNITY_EDITOR
            if (challenge == null) return new HashSet<string>();
            return Cached("mapIds:" + challenge.GetInstanceID(), () =>
            {
                var result = new HashSet<string>();
                foreach (var container in _containers)
                {
                    if (container == null || container.milestoneList == null) continue;
                    foreach (var cat in container.milestoneList)
                        if (cat?.milestoneEntries != null && cat.milestoneEntries.Contains(challenge) && !string.IsNullOrEmpty(cat.mapID))
                            result.Add(cat.mapID);
                }
                if (result.Count > 0) return result;

                if (challenge.confirmSelectedMapIds != null)
                    foreach (var a in challenge.confirmSelectedMapIds)
                        if (!string.IsNullOrEmpty(a.autoSelectedMapID)) result.Add(a.autoSelectedMapID);
                if (challenge.allowedMapIds != null)
                    foreach (var id in challenge.allowedMapIds)
                        if (!string.IsNullOrEmpty(id)) result.Add(id);
                return result;
            });
#else
            return new HashSet<string>();
#endif
        }

        /// <summary>Union of Enemy Option ids of every MapDataSO whose mapId is in the set.</summary>
        public static HashSet<string> EnemyIdsForMapIds(ICollection<string> mapIds)
        {
#if UNITY_EDITOR
            if (mapIds == null || mapIds.Count == 0) return new HashSet<string>();
            return Cached("ids:" + string.Join("|", mapIds.OrderBy(x => x)), () =>
            {
                var result = new HashSet<string>();
                foreach (var map in _maps)
                    if (map != null && mapIds.Contains(map.mapId))
                        result.UnionWith(IdsOf(map));
                return result;
            });
#else
            return new HashSet<string>();
#endif
        }

        /// <summary>First MapDataSO whose mapId this milestone belongs to (for previews), or null.</summary>
        public static MapDataSO FirstMapForMilestone(ChallengeDataSO challenge)
        {
#if UNITY_EDITOR
            if (challenge == null) return null;
            return Cached("firstMap:" + challenge.GetInstanceID(), () =>
            {
                var mapIds = MapIdsForMilestone(challenge);
                return mapIds.Count == 0 ? null : _maps.FirstOrDefault(m => m != null && mapIds.Contains(m.mapId));
            });
#else
            return null;
#endif
        }

        /// <summary>The milestone ChallengeDataSO a map would use at this index (first MilestoneDataContainer that lists the map).</summary>
        public static ChallengeDataSO FindMilestoneForMap(MapDataSO map, int milestoneIndex)
        {
#if UNITY_EDITOR
            if (map == null) return null;
            var milestones = MilestonesForMap(map);
            return milestoneIndex >= 0 && milestoneIndex < milestones.Count ? milestones[milestoneIndex] : null;
#else
            return null;
#endif
        }

        /// <summary>All milestone ChallengeDataSOs listed for this map, in order.</summary>
        public static List<ChallengeDataSO> MilestonesForMap(MapDataSO map)
        {
#if UNITY_EDITOR
            if (map == null) return new List<ChallengeDataSO>();
            return Cached("milestones:" + map.GetInstanceID() + ":" + map.mapId, () =>
            {
                foreach (var container in _containers)
                {
                    var cat = container != null ? container.milestoneList?.Find(c => c != null && c.mapID == map.mapId) : null;
                    if (cat?.milestoneEntries != null) return new List<ChallengeDataSO>(cat.milestoneEntries);
                }
                return new List<ChallengeDataSO>();
            });
#else
            return new List<ChallengeDataSO>();
#endif
        }

        private static IEnumerable<string> IdsOf(MapDataSO map) =>
            map?.EnemyOptions?.Where(o => o != null && !string.IsNullOrWhiteSpace(o.id)).Select(o => o.id).Distinct()
            ?? Enumerable.Empty<string>();

#if UNITY_EDITOR
        private static IEnumerable<T> LoadAll<T>() where T : UnityEngine.Object
        {
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) yield return asset;
            }
        }
#endif
    }
}
