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
        [Tooltip("Old behaviour: weighted random from the Enemy Options, gated by each enemy's Spawn Conditions.")]
        Conditions = 0,

        [Tooltip("An ordered list: each entry takes over after the previous one's step.")]
        Sequential = 1,
    }

    /// <summary>Which enemies the old random spawner may still pick while a schedule is running (Mix With Conditions).</summary>
    public enum RandomSpawnerPool
    {
        [Tooltip("Random spawner behaves exactly as in Conditions mode.")]
        AllMapEnemies = 0,

        [Tooltip("Random spawner only picks enemies that appear in the active schedule.")]
        OnlyScheduledEnemies = 1,

        [Tooltip("Random spawner never picks enemies the schedule handles - the schedule fully owns those.")]
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

        [FoldoutGroup(G), PropertyOrder(0)]
        [GUIColor("@this.enabled ? Color.green : Color.red")]
        [LabelText("Enabled")]
        [Tooltip("Untick to keep the entry but skip it at runtime (and in the preview).")]
        public bool enabled = true;

        [FoldoutGroup(G), PropertyOrder(1)]
        // Unity's own popup, not Odin's ValueDropdown: Odin's popup window calls a Unity internal
        // (ContainerWindow.FitWindowRectToScreen) that this Unity version no longer has, so it throws on click.
        [CustomValueDrawer(nameof(DrawEnemyIdField))]
        [ValidateInput(nameof(ValidateEnemyId), "Pick an enemy id that exists in this map's Enemy Setting > Enemy Options.")]
        [LabelText("Enemy")]
        [Tooltip("Id from this map's Enemy Options. Pools, per-enemy max, spawn effects and enemy data all come "
                 + "from that option.")]
        public string enemyId;

        [FoldoutGroup(G), PropertyOrder(2)]
        [LabelText("Note")]
        [Tooltip("Free text for designers - shows in the foldout title. Not used by the game.")]
        public string note;

        #endregion

        #region Interval

        [FoldoutGroup(G), PropertyOrder(20)]
        [Title("Interval", "Time between waves")]
        [EnumToggleButtons, HideLabel]
        public SpawnIntervalMode intervalMode = SpawnIntervalMode.MapSpawnTimer;

        [FoldoutGroup(G), PropertyOrder(21)]
        [ShowIf(nameof(intervalMode), SpawnIntervalMode.Custom)]
        [Unit(Units.Second), MinValue(0.05f)]
        [LabelText("Every")]
        [Tooltip("Seconds between waves.")]
        public float customInterval = 10f;

        [FoldoutGroup(G), PropertyOrder(22)]
        [ShowIf(nameof(intervalMode), SpawnIntervalMode.RandomRange)]
        [Unit(Units.Second), MinValue(0.05f)]
        [LabelText("Min")]
        [Tooltip("Shortest possible gap between waves.")]
        public float intervalMin = 5f;

        [FoldoutGroup(G), PropertyOrder(23)]
        [ShowIf(nameof(intervalMode), SpawnIntervalMode.RandomRange)]
        [Unit(Units.Second), MinValue(0.05f)]
        [LabelText("Max")]
        [Tooltip("Longest possible gap between waves.")]
        public float intervalMax = 15f;

        [FoldoutGroup(G), PropertyOrder(24)]
        [LabelText("First Wave At Start")]
        [Tooltip("On: the first wave comes the moment this entry starts. Off: it waits one interval first.")]
        public bool spawnOnStart = true;

        #endregion

        #region Amount

        [FoldoutGroup(G), PropertyOrder(30)]
        [Title("Amount")]
        [MinValue(1)]
        [LabelText("Per Wave (Min)")]
        [Tooltip("Enemies per wave - a random count between Min and Max.")]
        public int amountMin = 1;

        [FoldoutGroup(G), PropertyOrder(31)]
        [MinValue(1)]
        [LabelText("Per Wave (Max)")]
        [Tooltip("Enemies per wave - a random count between Min and Max.")]
        public int amountMax = 1;

        [FoldoutGroup(G), PropertyOrder(32)]
        [ShowIf("@this.amountMax > 1")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Delay Inside Wave")]
        [Tooltip("Gap between enemies of the same wave. 0 = all at once.")]
        public float burstDelay;

        [FoldoutGroup(G), PropertyOrder(33)]
        [MinValue(0)]
        [LabelText("Max Total")]
        [Tooltip("Stop after this many enemies from this entry in one run. 0 = unlimited, 1 = a one-off (e.g. a "
                 + "boss that must appear exactly once).")]
        public int maxTotal;

        [FoldoutGroup(G), PropertyOrder(34)]
        [Range(0f, 100f)]
        [LabelText("Wave Chance %")]
        [Tooltip("Chance each wave actually happens. A skipped wave still waits a full interval.")]
        public float chance = 100f;

        #endregion

        #region Rules

        [FoldoutGroup(G), PropertyOrder(40)]
        [Title("Rules")]
        [LabelText("Respect Per-Enemy Max")]
        [Tooltip("Obey the enemy option's Per-Enemy Max (max alive at once). Scheduled enemies always count "
                 + "toward it, so the random spawner still sees them.")]
        public bool respectPerEnemyMax = true;

        [FoldoutGroup(G), PropertyOrder(41)]
        [LabelText("Check Spawn Conditions")]
        [Tooltip("Also require the enemy option's own Spawn Conditions to pass. Off = the schedule decides alone.")]
        public bool checkSpawnConditions;

        [FoldoutGroup(G), PropertyOrder(42)]
        [LabelText("Play Spawn Effects")]
        [Tooltip("Run the enemy option's Spawn Effects (popup, delay, ...) like a random spawn would.")]
        public bool playSpawnEffects = true;

        [FoldoutGroup(G), PropertyOrder(43)]
        [EnumToggleButtons]
        [LabelText("Position")]
        public ScheduledSpawnPosition spawnPosition = ScheduledSpawnPosition.Default;

        [FoldoutGroup(G), PropertyOrder(44)]
        [ShowIf(nameof(spawnPosition), ScheduledSpawnPosition.AroundPlayer)]
        [MinValue(0f)]
        [LabelText("Radius Min")]
        [Tooltip("Units from the player.")]
        public float radiusMin = 20f;

        [FoldoutGroup(G), PropertyOrder(45)]
        [ShowIf(nameof(spawnPosition), ScheduledSpawnPosition.AroundPlayer)]
        [MinValue(0f)]
        [LabelText("Radius Max")]
        [Tooltip("Units from the player.")]
        public float radiusMax = 25f;

        #endregion

        #region Helpers

        public bool IsUsable => enabled && !string.IsNullOrWhiteSpace(enemyId);

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
        [Title("Timing", "Seconds since the run started")]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Start At")]
        [Tooltip("First moment this rule may spawn. 0 = from the start of the game.")]
        public float startAt;

        [FoldoutGroup(G), PropertyOrder(11)]
        [GUIColor("@this.useExpire ? Color.green : Color.red")]
        [LabelText("Use Expire")]
        [Tooltip("Stop spawning after a set time. Off = keeps spawning until the run ends (or Rush takes over).")]
        public bool useExpire;

        [FoldoutGroup(G), PropertyOrder(12)]
        [ShowIf(nameof(useExpire))]
        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Expire At")]
        [Tooltip("No new spawns after this time. Enemies already alive stay.")]
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
        [Title("Timing", "Position in the list sets the start time")]
        [GUIColor("@this.useCustomDuration ? Color.green : Color.red")]
        [LabelText("Custom Duration")]
        [Tooltip("Off = this entry lasts the list's Step. On = its own length, pushing later entries back.")]
        public bool useCustomDuration;

        [FoldoutGroup(G), PropertyOrder(11)]
        [ShowIf(nameof(useCustomDuration))]
        [Unit(Units.Second), MinValue(1f)]
        [LabelText("Duration")]
        public float customDuration = 30f;

        protected override string TimingSummary => useCustomDuration ? $"{customDuration:0.##}s slot" : "step slot";
    }

    [Serializable]
    public class SequentialSpawnSettings
    {
        [Unit(Units.Second), MinValue(1f)]
        [LabelText("Step")]
        [Tooltip("How long each entry lasts before the next one starts (entries can override it).")]
        public float step = 30f;

        [Unit(Units.Second), MinValue(0f)]
        [LabelText("Start Offset")]
        [Tooltip("When the first entry starts. 0 = right at the start of the game.")]
        public float startOffset;

        [LabelText("Stop Previous")]
        [Tooltip("On: when the next entry starts, the previous one stops spawning (a hand-over). "
                 + "Off: entries stack up - everything that has started keeps spawning.")]
        public bool stopPrevious = true;

        [ShowIf("@this.stopPrevious && !this.loop")]
        [LabelText("Last Entry Runs Forever")]
        [Tooltip("Keep the last entry spawning after its slot instead of the map going quiet.")]
        public bool lastEntryRunsForever = true;

        [LabelText("Loop")]
        [Tooltip("Start again from the first entry after the last one (good for Endless maps). Max Total "
                 + "counts reset every loop.")]
        public bool loop;

        [ListDrawerSettings(ShowIndexLabels = true, ShowPaging = false)]
        [LabelText("Entries (in order)")]
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
    }

    /// <summary>Turns spawn settings into a flat list of timed rules. Shared by runtime and previews.</summary>
    public static class EnemySpawnScheduleResolver
    {
        public const string FallbackText = "FALLS BACK TO CONDITIONS (random spawner as before).";

        /// <summary>
        /// Picks what runs this run: the selected milestone's rules when it has Override Map Spawn ON and at
        /// least one usable rule, otherwise the map's own mode. An empty result = behave as Conditions.
        /// </summary>
        public static SpawnScheduleSource ResolveForRun(MapDataSO map, ChallengeDataSO milestone, int milestoneIndex)
        {
            if (milestone != null && milestone.overrideMapSpawn)
            {
                var fromMilestone = ResolveMilestone(milestone);
                fromMilestone.Label = $"milestone {milestoneIndex} override ({milestone.name})";
                if (!fromMilestone.IsEmpty) return fromMilestone;
            }

            return ResolveMap(map);
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

        /// <summary>Readable timeline of a milestone ChallengeDataSO's own Spawn Rules.</summary>
        /// <param name="knownIds">Enemy ids that exist on the milestone's map(s); null/empty = don't warn.</param>
        public static string BuildMilestonePreview(ChallengeDataSO milestone, ICollection<string> knownIds)
        {
            if (milestone == null) return "";
            if (!milestone.overrideMapSpawn)
                return "Override Map Spawn is OFF - this milestone uses the map's own Enemy Spawn Mode.";

            var source = ResolveMilestone(milestone);
            if (source.IsEmpty)
                return "Override is ON but there are no usable rules -> this milestone uses the map's own Enemy Spawn Mode.";
            return BuildSourcePreview(source, knownIds);
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
    /// </summary>
    public static class EnemySpawnEditorLookup
    {
#if UNITY_EDITOR
        private static int _cacheKey;
        private static double _cacheTime;
        private static List<string> _cacheIds = new();
#endif

        /// <summary>Enemy ids for whatever is open in the Inspector (MapDataSO or milestone ChallengeDataSO).</summary>
        public static List<string> EnemyIdsForSelection()
        {
#if UNITY_EDITOR
            var sel = UnityEditor.Selection.activeObject;
            if (sel == null) return new List<string>();

            // Odin redraws often; the asset scan below is not free.
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            if (sel.GetInstanceID() == _cacheKey && now - _cacheTime < 2.0) return _cacheIds;

            var ids = new List<string>();
            if (sel is MapDataSO map)
                ids = IdsOf(map).ToList();
            else if (sel is ChallengeDataSO challenge)
                ids = EnemyIdsForMapIds(MapIdsForMilestone(challenge)).ToList();

            _cacheKey = sel.GetInstanceID();
            _cacheTime = now;
            _cacheIds = ids;
            return ids;
#else
            return new List<string>();
#endif
        }

        /// <summary>Map ids this milestone belongs to: MilestoneContainer first, then its Auto Selected / Map Filter lists.</summary>
        public static HashSet<string> MapIdsForMilestone(ChallengeDataSO challenge)
        {
            var result = new HashSet<string>();
#if UNITY_EDITOR
            if (challenge == null) return result;
            foreach (var container in LoadAll<MilestoneDataContainer>())
            {
                if (container.milestoneList == null) continue;
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
#endif
            return result;
        }

        /// <summary>Union of Enemy Option ids of every MapDataSO whose mapId is in the set.</summary>
        public static HashSet<string> EnemyIdsForMapIds(ICollection<string> mapIds)
        {
            var result = new HashSet<string>();
#if UNITY_EDITOR
            if (mapIds == null || mapIds.Count == 0) return result;
            foreach (var map in LoadAll<MapDataSO>())
                if (map != null && mapIds.Contains(map.mapId))
                    result.UnionWith(IdsOf(map));
#endif
            return result;
        }

        /// <summary>The milestone ChallengeDataSO a map would use at this index (first MilestoneDataContainer that lists the map).</summary>
        public static ChallengeDataSO FindMilestoneForMap(MapDataSO map, int milestoneIndex)
        {
#if UNITY_EDITOR
            if (map == null) return null;
            foreach (var container in LoadAll<MilestoneDataContainer>())
            {
                var found = MilestoneSpawnLookup.FindMilestone(container, map.mapId, milestoneIndex);
                if (found != null) return found;
            }
#endif
            return null;
        }

        /// <summary>All milestone ChallengeDataSOs listed for this map, in order.</summary>
        public static List<ChallengeDataSO> MilestonesForMap(MapDataSO map)
        {
            var result = new List<ChallengeDataSO>();
#if UNITY_EDITOR
            if (map == null) return result;
            foreach (var container in LoadAll<MilestoneDataContainer>())
            {
                var cat = container.milestoneList?.Find(c => c != null && c.mapID == map.mapId);
                if (cat?.milestoneEntries == null) continue;
                result.AddRange(cat.milestoneEntries);
                break;
            }
#endif
            return result;
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