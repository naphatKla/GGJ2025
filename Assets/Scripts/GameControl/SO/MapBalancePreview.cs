using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Challenge;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameControl.SO
{
    /// <summary>
    /// Read-only balance read-out for a MapDataSO: simulates one run the way the runtime schedules it -
    /// GameTimer loops (SpawnerStateController.RescheduleAllFromNow), Enemy Point refills, spawn-timer / chance /
    /// cost growth, the pattern rotation and batches, map events (MapEventController), Rush (RushDataSO.ApplyInto +
    /// OnMapModified) and the selected milestone's spawn plan. Touches no data.
    /// <para/>
    /// The run is cut into periods wherever something changes (an enemy / pattern / map-event time window, a
    /// scheduled rule, the hand-over, Rush). Each period shows who spawns and how often. What only exists while
    /// playing (kills, random rolls, which pattern was drawn) is shown as odds and rules, not outcomes.
    /// Keep this in step with the runtime classes named above.
    /// </summary>
    public static class MapBalancePreview
    {
        #region Options / report model

        /// <summary>Inspector settings for the preview (not saved in the asset).</summary>
        [Serializable]
        public class Options
        {
            [CustomValueDrawer(nameof(DrawMilestone))]
            [LabelText("Milestone"), LabelWidth(175)]
            [Tooltip("จำลองรอบที่เล่น milestone นี้: รวม Spawn Rules (Override Map Spawn), Time Modify และ Enemy Modify ของ milestone\n"
                     + "None = ค่าของแมพอย่างเดียว\n(ไม่รวม challenge ที่ผู้เล่นเลือกเพิ่มเอง)")]
            public int milestoneIndex = -1;

            [MinValue(0), Unit(Units.Second), LabelText("Split Every"), LabelWidth(175)]
            [Tooltip("ปกติจะแบ่งช่วงเฉพาะตอนที่มีอะไรเปลี่ยน (ศัตรู/Pattern/Map Event เริ่มหรือหยุด, Rush, ส่งต่อ milestone)\n"
                     + "ใส่ตัวเลข เช่น 60 = แบ่งเพิ่มทุก 60 วิ เพื่อดูค่าที่โตขึ้นเรื่อยๆ ละเอียดขึ้น\n0 = ไม่แบ่งเพิ่ม")]
            public float splitEvery;

            [MinValue(30), Unit(Units.Second), LabelText("Endless Length"), LabelWidth(175)]
            [Tooltip("แมพ Endless ไม่มีเวลาจบ: จำลองถึงวินาทีนี้")]
            public float endlessLength = 600f;

            [LabelText("Event Log"), LabelWidth(175)]
            [Tooltip("แสดงทุกเหตุการณ์ตามวินาทีจริง (ติ๊กด้านล่างเลือกว่าจะแสดงอะไร)")]
            public bool eventLog = true;

            [ShowIf(nameof(eventLog)), LabelText("   Growth / Points"), LabelWidth(175)]
            [Tooltip("Enemy Point เพิ่ม, Spawn Timer ลด, Chance / ราคาศัตรูโต, Pattern ห่างกันน้อยลง")]
            public bool logGrowth = true;

            [ShowIf(nameof(eventLog)), LabelText("   Patterns"), LabelWidth(175)]
            [Tooltip("Pattern ถูกเพิ่มเข้า rotation และทุกชุดที่เล่น")]
            public bool logPatterns = true;

            [ShowIf(nameof(eventLog)), LabelText("   Map Events"), LabelWidth(175)]
            [Tooltip("ทุกครั้งที่ Map Event ทอยเล่น พร้อมโอกาสและตัวที่สุ่มได้")]
            public bool logMapEvents = true;

            [LabelText("Reference Tables"), LabelWidth(175)]
            [Tooltip("ตารางค่าตั้งต้นของศัตรู / Pattern / Map Event ทุกตัว")]
            public bool details = true;

            public string Key =>
                $"{milestoneIndex}|{splitEvery}|{endlessLength}|{eventLog}|{logGrowth}|{logPatterns}|{logMapEvents}|{details}";

            private int DrawMilestone(int value, GUIContent label)
            {
#if UNITY_EDITOR
                var map = UnityEditor.Selection.activeObject as MapDataSO;
                var milestones = EnemySpawnEditorLookup.MilestonesForMap(map);
                var names = new List<string> { "None (map only)" };
                for (int i = 0; i < milestones.Count; i++)
                    names.Add($"[{i}] {(milestones[i] != null ? milestones[i].name : "<missing>")}");
                int current = Mathf.Clamp(value + 1, 0, names.Count - 1);
                int picked = UnityEditor.EditorGUILayout.Popup(label != null ? label.text : "", current, names.ToArray());
                return picked - 1;
#else
                return value;
#endif
            }
        }

        public class Report
        {
            public string Title;
            public readonly List<KeyValuePair<string, string>> Summary = new();
            public readonly List<string> Warnings = new();
            public readonly List<Period> Periods = new();
            public string Log;
            public string Reference;
            public float RunLength;
            public bool Endless;
        }

        public class Period
        {
            public float From, To;
            public bool Rush, AfterHandOver;
            public string Spawner;
            public readonly List<EnemyLine> Random = new();
            public readonly List<string> NotYet = new();
            public readonly List<string> Scheduled = new();
            public bool ScheduleActive;
            public string Points;
            public string PatternHead;
            public readonly List<string> Patterns = new();
            public string PatternEnemies;
            public readonly List<EventLine> MapEvents = new();
        }

        public class EnemyLine
        {
            public string Id;
            public float From, To;
            public string Note;
        }

        public class EventLine
        {
            public string Name;
            public string Rule;
            public string Picks;
            public readonly List<float> Times = new();
        }

        #endregion

        #region Simulation state

        private const int MaxLoopFires = 5000;
        private const float Eps = 0.0001f;

        private class Enemy
        {
            public MapDataSO.EnemyOption Src;
            public string Id;
            public float Weight;
            public float Cost;
        }

        /// <summary>Everything that changes during the run (map values, then Rush values).</summary>
        private class State
        {
            public bool Rush;
            public float Granted;
            public float RefillRate;

            public float SpawnTimer;
            public float SpawnTimerCeil;
            public List<Enemy> Enemies = new();
            public int ChanceUps, CostUps;

            public List<MapDataSO.PatternOption> Patterns;
            public float PatternMax;
            public bool CanDuplicate;
            public float TriggerTime;
            public bool DecCan;
            public float DecRate, DecMin;
            public float PlayAllIn;
            public int Rotation, Batches;

            public List<MapDataSO.EventMapOption> MapEvents;
        }

        private class Ev
        {
            public float T;
            public int Seq;
            public int Kind; // 0 core, 1 growth, 2 pattern, 3 map event
            public object Tag; // map event category, or BatchTag
            public Func<State, float, string> Apply;
        }

        private static readonly object BatchTag = new();

        /// <summary>One schedule phase with the rules the runtime keeps (unknown ids are dropped there too).</summary>
        private class Phase
        {
            public SpawnScheduleSource Src;
            public readonly List<ResolvedSpawnRule> Rules = new();
            public readonly HashSet<string> Ids = new();
            public bool Empty => Rules.Count == 0;
        }

        /// <summary>Mirror of EnemySpawnScheduleController's phase logic, as a pure function of time.</summary>
        private class Sched
        {
            public SpawnSchedulePlan Plan;
            public Phase First, Second;
            public bool Active, ThenMap, Together;
            public float HandOver = -1f; // ThenMapMode: elapsed of the hand-over, -1 = never / unknown
            public HashSet<string> UnionIds;

            public bool HandedOver(float t) => ThenMap && HandOver >= 0f && t >= HandOver;
            public Phase Current(float t) => HandedOver(t) ? Second : First;

            public bool RandomOn(float t)
            {
                if (!Active) return true;
                if (Together) return First.Src.RandomSpawnerOn || Second.Src.RandomSpawnerOn;
                return Current(t).Src.RandomSpawnerOn;
            }

            public Func<string, bool> RandomFilter(float t, out RandomSpawnerPool poolUsed)
            {
                poolUsed = RandomSpawnerPool.AllMapEnemies;
                if (!Active) return null;
                bool needs(Phase p) => p != null && !p.Empty && p.Src.MixWithConditions && p.Src.RandomPool != RandomSpawnerPool.AllMapEnemies;
                if (!needs(First) && !needs(Second)) return null;

                SpawnScheduleSource src;
                HashSet<string> ids;
                if (Together) { src = First.Src.MixWithConditions ? First.Src : Second.Src; ids = UnionIds; }
                else { var p = Current(t); src = p.Src; ids = p.Ids; }

                if (src.IsEmpty || !src.MixWithConditions) return null;
                poolUsed = src.RandomPool;
                return src.RandomPool switch
                {
                    RandomSpawnerPool.OnlyScheduledEnemies => id => ids.Contains(id),
                    RandomSpawnerPool.ExcludeScheduledEnemies => id => !ids.Contains(id),
                    _ => null
                };
            }

            public PatternEnemyPool PatternPool(float t, out HashSet<string> ids)
            {
                ids = null;
                if (!Active) return PatternEnemyPool.AllMapEnemies;
                bool restricts(Phase p) => p != null && !p.Empty && p.Src.PatternPool != PatternEnemyPool.AllMapEnemies;
                if (!restricts(First) && !restricts(Second)) return PatternEnemyPool.AllMapEnemies;

                var phase = Together ? First : Current(t);
                if (phase == null || phase.Empty) return PatternEnemyPool.AllMapEnemies;
                ids = Together ? UnionIds : phase.Ids;
                return phase.Src.PatternPool;
            }

            public IEnumerable<ResolvedSpawnRule> ActiveRules(float t)
            {
                if (!Active) yield break;
                if (Together)
                {
                    foreach (var r in On(First, t)) yield return r;
                    foreach (var r in On(Second, t)) yield return r;
                }
                else if (HandedOver(t))
                {
                    foreach (var r in On(Second, SecondTime(t))) yield return r;
                }
                else
                {
                    foreach (var r in On(First, t)) yield return r;
                }
            }

            public float SecondTime(float t) => ThenMap && Plan.SecondStartsAtHandOver && HandOver >= 0f ? t - HandOver : t;

            /// <summary>Elapsed time (absolute) a rule of the given phase starts / stops, for cutting periods.</summary>
            public IEnumerable<float> RuleCuts()
            {
                if (!Active) yield break;
                foreach (var r in First?.Rules ?? new List<ResolvedSpawnRule>())
                {
                    yield return r.Start;
                    if (r.Expire >= 0f) yield return r.Expire;
                }
                if (Second == null) yield break;
                float offset = ThenMap && Plan.SecondStartsAtHandOver && HandOver >= 0f ? HandOver : 0f;
                foreach (var r in Second.Rules)
                {
                    yield return r.Start + offset;
                    if (r.Expire >= 0f) yield return r.Expire + offset;
                }
            }

            private static IEnumerable<ResolvedSpawnRule> On(Phase p, float t)
            {
                if (p == null) yield break;
                float c = t;
                if (p.Src.CycleLength > 0f && t >= p.Src.CycleOffset)
                    c = p.Src.CycleOffset + (t - p.Src.CycleOffset) % p.Src.CycleLength;
                foreach (var r in p.Rules)
                    if (c >= r.Start && (r.Expire < 0f || c < r.Expire))
                        yield return r;
            }
        }

        #endregion

        #region Build

        public static Report Build(MapDataSO map, Options o)
        {
            o ??= new Options();
            var report = new Report();
            if (map == null) return report;
            try
            {
                BuildInternal(map, o, report);
            }
            catch (Exception e)
            {
                report.Warnings.Add("Preview error: " + e.Message);
                Debug.LogException(e);
            }
            return report;
        }

        private static void BuildInternal(MapDataSO map, Options o, Report report)
        {
            var warnings = report.Warnings;
            bool endless = map.endlessMode;

            var milestone = o.milestoneIndex >= 0 ? EnemySpawnEditorLookup.FindMilestoneForMap(map, o.milestoneIndex) : null;
            float timeMod = milestone != null ? milestone.timeModify : 0f;
            float T = endless ? Mathf.Max(30f, o.endlessLength) : Mathf.Max(1f, map.mapGlobalTime + timeMod);
            report.RunLength = T;
            report.Endless = endless;

            var rush = map.rushData;
            bool hasRush = !endless && rush != null;
            float R = hasRush ? Mathf.Max(0f, T - map.rushTime) : float.PositiveInfinity;

            var sched = BuildSched(map, milestone, Mathf.Max(0, o.milestoneIndex), warnings);

            // ---------------- events ----------------
            var events = new List<Ev>();
            int seq = 0;
            void Add(float t, int kind, object tag, Func<State, float, string> apply) =>
                events.Add(new Ev { T = t, Seq = seq++, Kind = kind, Tag = tag, Apply = apply });

            // Start (SpawnerStateController.SetupMapAndEnemy): points, first pattern.
            Add(0f, 0, null, (s, t) =>
            {
                AddPattern(s, t);
                return $"Start: Enemy Point {s.Granted:0.#} | Spawn Timer {s.SpawnTimer:0.##}s | {s.Rotation} pattern(s) in rotation";
            });

            float normalEnd = hasRush ? R : T;
            AddPhaseLoops(Add, map.triggerTimeCanDecrease, map.patternDecreaseInterval, map.playAllPatternIn,
                map.addPatternInterval, map.amountToAdd, map.canGrowth ? map.decreaseInterval : 0f, map.intervalIncreaseEnemyPoint,
                map.intervalEnemyChanceUpgrade, map.intervalEnemyPointRatioUpgrade, map, 0f, normalEnd, T, endless, warnings);

            // Map events: none in Endless (MapEventController schedules from "remaining", which is 0 there).
            bool rushHasEvents = hasRush && rush.eventmapOptions != null && rush.eventmapOptions.Count > 0;
            if (endless)
            {
                if (map.eventmapOptions != null && map.eventmapOptions.Any(e => e != null && e.enableThisMapEvent))
                    warnings.Add("Endless map: Map Events NEVER play (MapEventController schedules by remaining time, which is 0 in Endless).");
            }
            else
            {
                AddMapEventLoops(Add, map.eventmapOptions, 0f, rushHasEvents ? R : T, T, warnings);
            }

            if (!endless && T > 62f) Add(T - 62f, 0, null, (s, t) => "Rush warning popup");

            if (hasRush)
            {
                Add(R, 0, BatchTag, (s, t) => EnterRush(s, t, map, rush, milestone));
                AddPhaseLoops(Add, rush.triggerTimeCanDecrease, rush.patternDecreaseInterval, rush.playAllPatternIn,
                    rush.addPatternInterval, rush.amountToAdd, map.canGrowth ? map.decreaseInterval : 0f, map.intervalIncreaseEnemyPoint,
                    rush.intervalEnemyChanceUpgrade, rush.intervalEnemyPointRatioUpgrade, map, R, T, T, false, warnings);
                if (rushHasEvents) AddMapEventLoops(Add, rush.eventmapOptions, R, T, T, warnings);
            }

            events = events.OrderBy(e => e.T).ThenBy(e => e.Seq).ToList();

            // ---------------- state ----------------
            var state = new State
            {
                Granted = map.startEnemyPoint,
                RefillRate = map.rateIncreaseEnemyPoint,
                SpawnTimer = map.defaultEnemySpawnTimer,
                SpawnTimerCeil = map.defaultEnemySpawnTimer,
                Patterns = map.PatternOptions,
                PatternMax = map.patternMax,
                CanDuplicate = map.canDuplicateAfterHaveAllPattern,
                TriggerTime = map.triggerAllPatternIn,
                DecCan = map.triggerTimeCanDecrease,
                DecRate = map.patternDecreaseRate,
                DecMin = map.patternDecreaseMinimum,
                PlayAllIn = map.playAllPatternIn,
                MapEvents = map.eventmapOptions,
                Enemies = CopyEnemies(map.EnemyOptions, milestone, false),
            };

            CollectStaticWarnings(map, rush, endless, T, warnings);
            FillSummary(report, map, milestone, o.milestoneIndex, sched, T, R, hasRush, endless, timeMod);

            // ---------------- periods ----------------
            var cuts = BuildCuts(map, rush, sched, T, R, hasRush, o.splitEvery);
            var log = new List<(float t, int kind, string text)>();
            int ei = 0;

            for (int i = 0; i < cuts.Count - 1; i++)
            {
                float a = cuts[i], b = cuts[i + 1];
                bool last = i == cuts.Count - 2;
                var p = new Period { From = a, To = b };
                var eventTimes = new Dictionary<MapDataSO.EventMapOption, List<float>>();
                var batchTimes = new List<float>();

                void Run(Ev ev)
                {
                    string text = ev.Apply(state, ev.T);
                    if (!string.IsNullOrEmpty(text)) log.Add((ev.T, ev.Kind, text));
                    if (ev.Tag == BatchTag) batchTimes.Add(ev.T);
                    else if (ev.Tag is MapDataSO.EventMapOption cat && text != null)
                    {
                        if (!eventTimes.TryGetValue(cat, out var list)) eventTimes[cat] = list = new List<float>();
                        list.Add(ev.T);
                    }
                }

                while (ei < events.Count && events[ei].T <= a + Eps) Run(events[ei++]);

                float probe = Mathf.Min(a + 0.01f, b);
                var start = Snap(state, sched, probe);
                int rotStart = state.Rotation;
                float triggerStart = state.TriggerTime;
                float pointsStart = state.Granted;
                var eventCats = state.MapEvents?.Where(e => e != null && e.enableThisMapEvent).ToList()
                                ?? new List<MapDataSO.EventMapOption>();

                while (ei < events.Count && (events[ei].T < b - Eps || (last && events[ei].T <= b + Eps))) Run(events[ei++]);

                var end = Snap(state, sched, Mathf.Max(b - 0.01f, probe));
                FillPeriod(p, state, sched, start, end, probe, rotStart, state.Rotation, triggerStart, state.TriggerTime,
                    pointsStart, state.Granted, batchTimes, eventCats, eventTimes, T, endless);
                report.Periods.Add(p);
            }

            if (o.eventLog)
            {
                var sb = new StringBuilder();
                foreach (var group in log
                             .Where(l => l.kind == 0 || (l.kind == 1 && o.logGrowth) || (l.kind == 2 && o.logPatterns) || (l.kind == 3 && o.logMapEvents))
                             .GroupBy(l => Mathf.Round(l.t * 100f) / 100f))
                    sb.AppendLine($"{Stamp(group.Key, T, endless)}   {string.Join("   |   ", group.Select(g => g.text))}");
                report.Log = sb.ToString().TrimEnd();
            }

            if (o.details) report.Reference = BuildReference(map, rush, milestone);
        }

        private static List<float> BuildCuts(MapDataSO map, RushDataSO rush, Sched sched, float T, float R, bool hasRush, float splitEvery)
        {
            var raw = new List<float> { 0f, T };
            if (hasRush) raw.Add(R);
            if (sched.ThenMap && sched.HandOver >= 0f) raw.Add(sched.HandOver);
            raw.AddRange(sched.RuleCuts());

            void enemyCuts(List<MapDataSO.EnemyOption> list)
            {
                if (list == null) return;
                foreach (var e in list)
                    if (e != null && e.useSpawnConditions && e.spawnConditions != null)
                        foreach (var c in e.spawnConditions.OfType<TimeWindowEnemySpawnCondition>())
                        {
                            raw.Add(c.startAfter);
                            if (c.endAt >= 0f) raw.Add(c.endAt);
                        }
            }

            void patternCuts(List<MapDataSO.PatternOption> list)
            {
                if (list == null) return;
                foreach (var p in list)
                    if (p != null && p.enableThisPattern && p.useCondition && p.patternCondition != null)
                        foreach (var c in p.patternCondition.Where(c => c != null))
                        {
                            raw.Add(c.startAfter);
                            if (c.endAt >= 0f) raw.Add(c.endAt);
                        }
            }

            void eventCuts(List<MapDataSO.EventMapOption> list)
            {
                if (list == null) return;
                foreach (var cat in list.Where(c => c != null && c.enableThisMapEvent && c.allMapEventID != null))
                    foreach (var kv in cat.allMapEventID.Where(k => k.useCondition && k.mapEventCondition != null))
                        foreach (var c in kv.mapEventCondition.Where(c => c != null))
                        {
                            raw.Add(c.startAfter);
                            if (c.endAt >= 0f) raw.Add(c.endAt);
                        }
            }

            enemyCuts(map.EnemyOptions);
            patternCuts(map.PatternOptions);
            eventCuts(map.eventmapOptions);
            if (rush != null)
            {
                enemyCuts(rush.enemyOptions);
                patternCuts(rush.patternOptions);
                eventCuts(rush.eventmapOptions);
            }

            if (splitEvery >= 5f)
                for (float t = splitEvery; t < T; t += splitEvery) raw.Add(t);

            var cuts = new List<float>();
            foreach (float c in raw.Where(c => c >= 0f && c <= T).OrderBy(c => c))
                if (cuts.Count == 0 || c - cuts[cuts.Count - 1] >= 1f) cuts.Add(c);
            if (cuts[cuts.Count - 1] < T) cuts[cuts.Count - 1] = T; // last cut snaps to the end
            if (cuts.Count < 2) cuts.Add(T);
            return cuts;
        }

        #endregion

        #region Event generation

        private static void AddPhaseLoops(Action<float, int, object, Func<State, float, string>> add,
            bool decCan, float decInterval, float playAllIn, float addInterval, int addAmount,
            float timerInterval, float pointInterval, float chanceInterval, float ratioInterval,
            MapDataSO map, float start, float end, float T, bool endless, List<string> warnings)
        {
            if (decCan && decInterval > 0f)
                foreach (float t in LoopTimes(start, decInterval, end, T, endless, warnings, "Pattern Decrease Interval"))
                    add(t, 1, null, (s, now) =>
                    {
                        if (!s.DecCan || s.TriggerTime <= s.DecMin) return null;
                        float old = s.TriggerTime;
                        s.TriggerTime = Mathf.Max(s.DecMin, s.TriggerTime - s.DecRate);
                        return $"Pattern spread {old:0.##}s -> {s.TriggerTime:0.##}s";
                    });

            if (playAllIn > 0f)
                foreach (float t in LoopTimes(start, playAllIn, end, T, endless, warnings, "Play All Pattern In"))
                    add(t, 2, BatchTag, PlayBatch);

            if (addInterval > 0f)
                foreach (float t in LoopTimes(start, addInterval, end, T, endless, warnings, "Add Pattern Interval"))
                    add(t, 2, null, (s, now) =>
                    {
                        int before = s.Rotation;
                        for (int i = 0; i < addAmount; i++) AddPattern(s, now);
                        return before == s.Rotation ? null : $"Pattern added: rotation {before} -> {s.Rotation}/{PatternCap(s)}";
                    });

            if (timerInterval > 0f)
                foreach (float t in LoopTimes(start, timerInterval, end, T, endless, warnings, "Decrease Interval"))
                    add(t, 1, null, (s, now) =>
                    {
                        float old = s.SpawnTimer;
                        s.SpawnTimer = Mathf.Clamp(s.SpawnTimer - map.decreaseAmount, map.decreaseMinimum, s.SpawnTimerCeil);
                        return Mathf.Approximately(old, s.SpawnTimer) ? null : $"Spawn Timer {old:0.###}s -> {s.SpawnTimer:0.###}s";
                    });

            if (pointInterval > 0f)
                foreach (float t in LoopTimes(start, pointInterval, end, T, endless, warnings, "Interval Increase Enemy Point"))
                    add(t, 1, null, (s, now) =>
                    {
                        s.Granted += s.RefillRate;
                        return $"Enemy Point +{s.RefillRate:0.#} (total {s.Granted:0.#})";
                    });

            if (chanceInterval > 0f)
                foreach (float t in LoopTimes(start, chanceInterval, end, T, endless, warnings, "Interval Enemy Chance Upgrade"))
                    add(t, 1, null, (s, now) =>
                    {
                        if (!s.Enemies.Any(e => e.Src.enemyChanceCanGrowth)) return null;
                        foreach (var e in s.Enemies)
                            if (e.Src.enemyChanceCanGrowth) e.Weight += e.Src.enemyChanceGrowthRate;
                        Normalize(s.Enemies);
                        s.ChanceUps++;
                        return $"Enemy chance growth #{s.ChanceUps}";
                    });

            if (ratioInterval > 0f)
                foreach (float t in LoopTimes(start, ratioInterval, end, T, endless, warnings, "Interval Enemy Point Ratio Upgrade"))
                    add(t, 1, null, (s, now) =>
                    {
                        if (!s.Enemies.Any(e => e.Src.enemyPointCanGrowth)) return null;
                        foreach (var e in s.Enemies)
                            if (e.Src.enemyPointCanGrowth) e.Cost *= 1f + e.Src.enemyPointGrowthRate / 100f;
                        s.CostUps++;
                        return $"Enemy cost growth #{s.CostUps}";
                    });
        }

        private static void AddMapEventLoops(Action<float, int, object, Func<State, float, string>> add,
            List<MapDataSO.EventMapOption> options, float start, float end, float T, List<string> warnings)
        {
            if (options == null) return;
            foreach (var opt in options)
            {
                if (opt == null || !opt.enableThisMapEvent) continue;
                if (opt.intervalCanModify && opt.intervalModify <= 0f)
                    warnings.Add($"Map Event '{opt.catagolyMapEvent}': Interval Can Modify is ON but Interval Modify = {opt.intervalModify} (divide by zero at runtime).");

                var local = opt;
                foreach (float t in MapEventTimes(opt, start, end, T))
                    add(t, 3, local, (s, now) =>
                        $"Map Event '{local.catagolyMapEvent}' rolls {local.eventMapChance:0.#}%: {MapEventPicks(local, now)}");
            }
        }

        /// <summary>GameTimer.ScheduleLoopingFromNow: countdown fires 1s early (it starts from remaining + 1).</summary>
        private static IEnumerable<float> LoopTimes(float start, float interval, float end, float T, bool endless,
            List<string> warnings, string what)
        {
            interval = Mathf.Max(0.01f, interval);
            if (endless)
            {
                for (int i = 1; start + i * interval <= end; i++)
                {
                    if (i > MaxLoopFires) { warnings.Add($"{what} ({interval}s) fires too often - preview cut at {MaxLoopFires}."); yield break; }
                    yield return start + i * interval;
                }
                yield break;
            }

            float startRemaining = (T - start) + 1f;
            if (startRemaining <= 0f) yield break;
            int count = Mathf.FloorToInt(startRemaining / interval);
            if (count > MaxLoopFires) { warnings.Add($"{what} ({interval}s) fires too often - preview cut at {MaxLoopFires}."); count = MaxLoopFires; }
            for (int i = 1; i <= count; i++)
            {
                float t = Mathf.Max(start, T - Mathf.Max(startRemaining - i * interval, 0f));
                if (t > end + Eps) yield break;
                yield return t;
            }
        }

        /// <summary>MapEventController.ScheduleCategoryForwardFromNow.</summary>
        private static IEnumerable<float> MapEventTimes(MapDataSO.EventMapOption opt, float start, float end, float T)
        {
            float elapsed = start;
            float remaining = T - start;
            for (int guard = 0; remaining > 0f && guard < MaxLoopFires; guard++)
            {
                float interval = Mathf.Max(0.0001f, MapEventInterval(opt, elapsed));
                float next = remaining - interval;
                if (next < 0f) yield break;
                float at = T - next;
                if (at > end + Eps) yield break;
                yield return at;
                elapsed += interval;
                remaining = next;
            }
        }

        private static float MapEventInterval(MapDataSO.EventMapOption opt, float elapsed)
        {
            if (!opt.intervalCanModify || opt.intervalModify <= 0f) return opt.playInterval;
            int steps = Mathf.FloorToInt(elapsed / opt.intervalModify);
            float min = opt.minPlayInterval > 0 ? opt.minPlayInterval : 0.1f;
            return Mathf.Max(min, opt.playInterval + opt.rateModify * steps);
        }

        #endregion

        #region State changes (mirror the runtime)

        private static int PatternCap(State s) => Mathf.Max(1, Mathf.RoundToInt(s.PatternMax));

        /// <summary>EnemyPatternController.AddRandomPattern (rotation size only - which pattern is random).</summary>
        private static void AddPattern(State s, float t)
        {
            if (s.Patterns == null || s.Patterns.Count == 0) return;
            if (s.Rotation >= PatternCap(s)) s.Rotation--; // EnsurePatternCapacity drops the oldest first
            int eligible = s.Patterns.Count(p => PatternEligible(p, t));
            if (s.Rotation < eligible || (s.CanDuplicate && eligible > 0)) s.Rotation++;
        }

        private static string PlayBatch(State s, float t)
        {
            s.Batches++;
            int n = s.Rotation;
            if (n == 0) return $"Pattern batch #{s.Batches}: rotation EMPTY - nothing plays";
            float gap = Mathf.Max(0f, s.TriggerTime) / n;
            float last = gap * (n - 1);
            string text = $"Pattern batch #{s.Batches}: {n} pattern(s), 1 every {gap:0.##}s (last at +{last:0.##}s)";
            if (s.PlayAllIn > 0f && last > s.PlayAllIn)
                text += $" !! longer than Play All Pattern In ({s.PlayAllIn:0.#}s) - batches queue up and drift late";
            return text;
        }

        /// <summary>GameStateController.EnterRush -> RushDataSO.ApplyInto -> SpawnerStateController.OnMapModified.</summary>
        private static string EnterRush(State s, float t, MapDataSO map, RushDataSO rush, ChallengeDataSO milestone)
        {
            s.Rush = true;
            s.Enemies = CopyEnemies(rush.enemyOptions, milestone, true);
            s.ChanceUps = s.CostUps = 0;
            s.SpawnTimer = s.SpawnTimerCeil = rush.defaultEnemySpawnTimer;
            s.RefillRate = map.rateIncreaseEnemyPoint;

            s.Patterns = rush.patternOptions;
            s.PatternMax = rush.patternMax;
            s.CanDuplicate = rush.canDuplicateAfterHaveAllPattern;
            s.TriggerTime = Mathf.Max(0.01f, rush.triggerAllPatternIn);
            s.DecCan = rush.triggerTimeCanDecrease;
            s.DecRate = rush.patternDecreaseRate;
            s.DecMin = rush.patternDecreaseMinimum;
            s.PlayAllIn = rush.playAllPatternIn;
            if (rush.eventmapOptions != null && rush.eventmapOptions.Count > 0) s.MapEvents = rush.eventmapOptions;

            // ReloadPatterns keeps min(max, current), then OnMapModified adds patternMax more, then triggers all.
            int keep = Mathf.Min(PatternCap(s), s.Rotation);
            s.Rotation = 0;
            for (int i = 0; i < keep; i++) AddPattern(s, t);
            for (int i = 0; i < (int)rush.patternMax; i++) AddPattern(s, t);

            return $"=== RUSH ({rush.name}) === Spawn Timer {s.SpawnTimer:0.##}s, schedule stops, random spawner ON | " + PlayBatch(s, t);
        }

        private static List<Enemy> CopyEnemies(List<MapDataSO.EnemyOption> options, ChallengeDataSO milestone, bool rush)
        {
            var list = new List<Enemy>();
            if (options == null) return list;
            foreach (var opt in options)
            {
                if (opt == null || string.IsNullOrWhiteSpace(opt.id)) continue;
                var e = new Enemy { Src = opt, Id = opt.id, Weight = opt.chance, Cost = opt.spawnPoint };
                // Rush: a milestone's Rush Set Chance replaces the chance (GameStateController.ApplyRushEnemyOption).
                if (rush && milestone != null && TryMilestonePercent(milestone, opt.id, EnemyStat.RushSetChance, out float set))
                    e.Weight = set;
                list.Add(e);
            }
            return list;
        }

        /// <summary>EnemySpawnerController.NormalizeChancesRespectingFlags.</summary>
        private static void Normalize(List<Enemy> list)
        {
            foreach (var e in list) e.Weight = Mathf.Max(0f, e.Weight);
            float fixedSum = list.Where(e => e.Src.disableNormalize).Sum(e => e.Weight);
            float varSum = list.Where(e => !e.Src.disableNormalize).Sum(e => e.Weight);
            float remaining = Mathf.Max(0f, 100f - fixedSum);
            if (varSum <= 0f || remaining <= 0f) return;
            foreach (var e in list)
                if (!e.Src.disableNormalize) e.Weight = e.Weight / varSum * remaining;
        }

        #endregion

        #region Snapshots -> periods

        private class Snapshot
        {
            public bool RandomOn;
            public RandomSpawnerPool Pool;
            public readonly Dictionary<string, float> Share = new();
            public readonly Dictionary<string, float> Cost = new();
            public float SpawnTimer;
            public float AvgInterval;
        }

        private static Snapshot Snap(State s, Sched sched, float t)
        {
            var snap = new Snapshot { SpawnTimer = s.SpawnTimer };
            snap.RandomOn = s.Rush || sched.RandomOn(t);
            var filter = s.Rush ? null : sched.RandomFilter(t, out snap.Pool);
            var picks = s.Enemies.Where(e => EnemyEligible(e.Src, t) && (filter == null || filter(e.Id))).ToList();
            float sum = picks.Sum(e => Mathf.Max(0f, e.Weight));
            foreach (var e in picks)
            {
                float share = sum > 0f ? Mathf.Max(0f, e.Weight) / sum : 1f / picks.Count;
                snap.Share[e.Id] = share;
                snap.Cost[e.Id] = e.Cost;
                snap.AvgInterval += share * (e.Src.useCustomInterval ? Mathf.Max(0.01f, e.Src.customInterval) : s.SpawnTimer);
            }
            return snap;
        }

        private static void FillPeriod(Period p, State s, Sched sched, Snapshot a, Snapshot b, float probe,
            int rotA, int rotB, float trigA, float trigB, float ptsA, float ptsB, List<float> batches,
            List<MapDataSO.EventMapOption> cats, Dictionary<MapDataSO.EventMapOption, List<float>> eventTimes,
            float T, bool endless)
        {
            p.Rush = s.Rush;
            p.AfterHandOver = !s.Rush && sched.HandedOver(probe);

            // ---- random spawner ----
            if (!a.RandomOn)
            {
                p.Spawner = "OFF (schedule only)";
            }
            else
            {
                string timer = Mathf.Approximately(a.SpawnTimer, b.SpawnTimer)
                    ? $"{a.SpawnTimer:0.##}s" : $"{a.SpawnTimer:0.##}s -> {b.SpawnTimer:0.##}s";
                string perMin = a.AvgInterval > 0f ? $" (~{60f / a.AvgInterval:0.#} per minute while points > 0)" : "";
                string pool = a.Pool switch
                {
                    RandomSpawnerPool.OnlyScheduledEnemies => " | only scheduled enemies",
                    RandomSpawnerPool.ExcludeScheduledEnemies => " | excluding scheduled enemies",
                    _ => ""
                };
                p.Spawner = $"1 enemy every {timer}{perMin}{pool}";

                foreach (var kv in a.Share.OrderByDescending(x => x.Value))
                {
                    var e = s.Enemies.First(x => x.Id == kv.Key);
                    b.Share.TryGetValue(kv.Key, out float to);
                    var notes = new List<string> { $"{a.Cost[kv.Key]:0.##} pt" };
                    if (e.Src.useCustomInterval) notes.Add($"next check {e.Src.customInterval:0.##}s");
                    if (e.Src.useMaxperEnemy && e.Src.maximumPerEnemy > 0) notes.Add($"max alive {e.Src.maximumPerEnemy:0}");
                    p.Random.Add(new EnemyLine { Id = kv.Key, From = kv.Value, To = to, Note = string.Join(" · ", notes) });
                }
            }

            foreach (var e in s.Enemies.Where(e => !EnemyEligible(e.Src, probe)))
                p.NotYet.Add($"{e.Id} ({EnemyWindowThai(e.Src, T, endless)})");

            // ---- schedule ----
            p.ScheduleActive = !s.Rush && sched.Active;
            if (p.ScheduleActive)
                foreach (var r in sched.ActiveRules(probe))
                    p.Scheduled.Add(RuleThai(r, s.SpawnTimer, sched, probe, T, endless));

            // ---- points ----
            p.Points = Mathf.Approximately(ptsA, ptsB)
                ? $"{ptsA:0.#} granted"
                : $"{ptsA:0.#} -> {ptsB:0.#} granted (+{s.RefillRate:0.#} per refill)";

            // ---- patterns ----
            var pool2 = s.Rush ? PatternEnemyPool.AllMapEnemies : sched.PatternPool(probe, out _);
            string rot = rotA == rotB ? $"{rotA}" : $"{rotA} -> {rotB}";
            float gapA = rotA > 0 ? Mathf.Max(0f, trigA) / rotA : 0f;
            float gapB = rotB > 0 ? Mathf.Max(0f, trigB) / rotB : 0f;
            string gap = Mathf.Approximately(gapA, gapB) ? $"{gapA:0.##}" : $"{gapA:0.##} -> {gapB:0.##}";
            string when = batches.Count == 0 ? "no batch in this period"
                : $"{batches.Count} batch(es) at {TimeList(batches, T, endless)}";
            p.PatternHead = s.PlayAllIn > 0f || batches.Count > 0
                ? $"every {s.PlayAllIn:0.#}s: {when} | {rot} per batch, {gap}s apart"
                : "never (Play All Pattern In = 0)";

            var playable = s.Patterns?.Where(x => PatternEligible(x, probe)).ToList() ?? new List<MapDataSO.PatternOption>();
            foreach (var x in playable)
                p.Patterns.Add($"{PatternName(x)} — {x.patternPoint:0.#} pt{PatternCounts(x, s, sched, probe)}");

            var pe = PatternEnemies(s, sched, probe, false);
            p.PatternEnemies = pool2 == PatternEnemyPool.NoEnemies ? "none (Pattern Enemy Pool = No Enemies)"
                : pe.Count == 0 ? "none can spawn"
                : string.Join(", ", pe.Select(e => e.Id))
                  + (pool2 == PatternEnemyPool.OnlyScheduledEnemies ? " (scheduled enemies only)" : "");

            // ---- map events ----
            foreach (var c in cats)
            {
                float iA = MapEventInterval(c, p.From), iB = MapEventInterval(c, p.To);
                var line = new EventLine
                {
                    Name = c.catagolyMapEvent,
                    Rule = (Mathf.Approximately(iA, iB) ? $"every {iA:0.##}s" : $"every {iA:0.##}s -> {iB:0.##}s")
                           + $", {c.eventMapChance:0.#}% to play",
                    Picks = MapEventPicks(c, probe)
                };
                if (eventTimes.TryGetValue(c, out var times)) line.Times.AddRange(times);
                p.MapEvents.Add(line);
            }
        }

        private static string RuleThai(ResolvedSpawnRule r, float timer, Sched sched, float probe, float T, bool endless)
        {
            var rule = r.Rule;
            string amount = rule.amountMax > rule.amountMin ? $"x{rule.amountMin}-{rule.amountMax}" : $"x{Mathf.Max(1, rule.amountMin)}";
            if (rule.maxAlive > 0)
                amount = rule.fillToMaxEachWave ? $"keep {rule.maxAlive} alive" : amount + $" (max alive {rule.maxAlive})";
            if (rule.maxTotal > 0) amount += $" (max {rule.maxTotal} total)";
            if (rule.EffectiveChance < 100f) amount += $" {rule.EffectiveChance:0.#}% per wave";

            string interval = rule.intervalMode switch
            {
                SpawnIntervalMode.Custom => $"every {rule.customInterval:0.##}s",
                SpawnIntervalMode.RandomRange => $"every {rule.intervalMin:0.##}-{rule.intervalMax:0.##}s",
                _ => $"every {timer:0.##}s (Spawn Timer)"
            };

            string until = "";
            if (r.Expire >= 0f)
            {
                float abs = r.Expire;
                if (sched.HandedOver(probe)) abs = r.Expire + (probe - sched.SecondTime(probe));
                until = $" until {Clock(abs)}";
            }
            return $"{rule.enemyId} {amount}, {interval}{until}";
        }

        private static List<Enemy> PatternEnemies(State s, Sched sched, float t, bool bypass)
        {
            HashSet<string> ids = null;
            var pool = s.Rush ? PatternEnemyPool.AllMapEnemies : sched.PatternPool(t, out ids);
            if (pool == PatternEnemyPool.NoEnemies) return new List<Enemy>();

            bool inPool(Enemy e) => pool != PatternEnemyPool.OnlyScheduledEnemies || ids == null || ids.Contains(e.Id);
            var list = s.Enemies.Where(e => !e.Src.disableInPattern && (bypass || EnemyEligible(e.Src, t)) && inPool(e)).ToList();
            if (list.Count == 0 && pool == PatternEnemyPool.OnlyScheduledEnemies)
                list = s.Enemies.Where(e => !e.Src.disableInPattern && inPool(e)).ToList(); // runtime fallback
            return list;
        }

        private static string PatternCounts(MapDataSO.PatternOption p, State s, Sched sched, float t)
        {
            var enemies = PatternEnemies(s, sched, t, p.bypassSpawnCondition);
            if (p.enableSpecificEnemy && p.specificEnemyList != null && p.specificEnemyList.Count > 0)
            {
                var ids = p.specificEnemyList.Where(k => k.chance > 0f && !string.IsNullOrWhiteSpace(k.enemyID))
                    .Select(k => k.enemyID.Trim().ToLowerInvariant()).ToHashSet();
                var specific = enemies.Where(e => ids.Contains(e.Id.Trim().ToLowerInvariant())).ToList();
                if (specific.Count > 0) enemies = specific;
            }
            if (enemies.Count == 0) return " -> no enemy";
            return " -> " + string.Join(" or ", enemies.Take(3).Select(e => e.Cost > 0f
                       ? $"{e.Id} x{Mathf.FloorToInt(p.patternPoint / e.Cost)}" : $"{e.Id} x0 (cost 0)"))
                   + (enemies.Count > 3 ? " ..." : "");
        }

        #endregion

        #region Conditions

        private static bool EnemyEligible(MapDataSO.EnemyOption opt, float t)
        {
            if (!opt.useSpawnConditions || opt.spawnConditions == null || opt.spawnConditions.Count == 0) return true;
            bool all = opt.conditionLogic == MapDataSO.EnemyOption.ConditionLogic.All;
            foreach (var c in opt.spawnConditions)
            {
                if (c == null) continue;
                bool pass = !(c is TimeWindowEnemySpawnCondition w) || (t >= w.startAfter && (w.endAt < 0f || t <= w.endAt));
                if (all && !pass) return false;
                if (!all && pass) return true;
            }
            return all;
        }

        private static string EnemyWindow(MapDataSO.EnemyOption opt)
        {
            if (!opt.useSpawnConditions || opt.spawnConditions == null || opt.spawnConditions.Count == 0) return "always";
            var parts = opt.spawnConditions.Where(c => c != null).Select(c => c is TimeWindowEnemySpawnCondition w
                ? $"{Clock(w.startAfter)}-{(w.endAt < 0f ? "end" : Clock(w.endAt))}"
                : c.GetType().Name + "(?)").ToList();
            if (parts.Count == 0) return "always";
            return string.Join(opt.conditionLogic == MapDataSO.EnemyOption.ConditionLogic.All ? " AND " : " OR ", parts);
        }

        private static string EnemyWindowThai(MapDataSO.EnemyOption opt, float T, bool endless) => "window " + EnemyWindow(opt);

        private static bool PatternEligible(MapDataSO.PatternOption p, float t)
        {
            if (p == null || !p.enableThisPattern || p.pattern == null) return false;
            if (!p.useCondition || p.patternCondition == null || p.patternCondition.Count == 0) return true;
            foreach (var c in p.patternCondition)
            {
                if (c == null) continue;
                if (t < c.startAfter) return false;
                if (c.endAt >= 0f && t > c.endAt) return false;
            }
            return true;
        }

        private static string PatternWindow(MapDataSO.PatternOption p)
        {
            if (!p.useCondition || p.patternCondition == null || p.patternCondition.Count == 0) return "always";
            return string.Join(" AND ", p.patternCondition.Where(c => c != null)
                .Select(c => $"{Clock(c.startAfter)}-{(c.endAt < 0f ? "end" : Clock(c.endAt))}"));
        }

        private static bool KvEligible(MapDataSO.EventMapOption.MapEventKv kv, float t)
        {
            if (!kv.useCondition || kv.mapEventCondition == null || kv.mapEventCondition.Count == 0) return true;
            foreach (var c in kv.mapEventCondition)
            {
                if (c == null) continue;
                if (t < c.startAfter) return false;
                if (c.endAt >= 0f && t > c.endAt) return false;
            }
            return true;
        }

        private static string KvWindow(MapDataSO.EventMapOption.MapEventKv kv)
        {
            if (!kv.useCondition || kv.mapEventCondition == null || kv.mapEventCondition.Count == 0) return "always";
            return string.Join(" AND ", kv.mapEventCondition.Where(c => c != null)
                .Select(c => $"{Clock(c.startAfter)}-{(c.endAt < 0f ? "end" : Clock(c.endAt))}"));
        }

        /// <summary>MapEventController.PlayMapEventCatagory: one weighted pick + every non-weighted entry at 100%.</summary>
        private static string MapEventPicks(MapDataSO.EventMapOption opt, float t)
        {
            var kvs = opt.allMapEventID?.Where(k => KvEligible(k, t)).ToList() ?? new List<MapDataSO.EventMapOption.MapEventKv>();
            var weighted = kvs.Where(k => k.useWeightRandom).ToList();
            float total = weighted.Sum(k => k.chance);
            var parts = new List<string>();
            if (weighted.Count > 0)
                parts.Add(total > 0f
                    ? "pick 1 of " + string.Join(" / ", weighted.Select(k => $"{k.mapEventID} {k.chance / total * 100f:0.#}%"))
                    : "weighted entries all 0% - nothing");
            var always = kvs.Where(k => !k.useWeightRandom && k.chance >= 100f).Select(k => k.mapEventID).ToList();
            if (always.Count > 0) parts.Add("+ always " + string.Join(", ", always));
            var never = kvs.Where(k => !k.useWeightRandom && k.chance < 100f).Select(k => k.mapEventID).ToList();
            if (never.Count > 0) parts.Add($"(never: {string.Join(", ", never)} - non-weighted needs chance 100)");
            return parts.Count > 0 ? string.Join(" ", parts) : "nothing eligible now";
        }

        #endregion

        #region Summary / warnings / reference

        private static void FillSummary(Report r, MapDataSO map, ChallengeDataSO milestone, int milestoneIndex,
            Sched sched, float T, float R, bool hasRush, bool endless, float timeMod)
        {
            r.Title = $"{map.name}" + (milestone != null ? $"  +  Milestone [{milestoneIndex}] {milestone.name}" : "  (map only)");
            void add(string k, string v) => r.Summary.Add(new KeyValuePair<string, string>(k, v));

            add("Run", endless ? $"Endless (simulated to {Clock(T)})"
                : $"{Clock(T)}" + (Mathf.Approximately(timeMod, 0f) ? "" : $" (incl. milestone Time Modify {timeMod:+0.#;-0.#}s)"));
            add("Rush", hasRush ? $"at {Clock(R)} (clock {Clock(T - R)}) using {map.rushData.name}"
                : endless ? "none (Endless never reaches Rush)" : "none (no Rush Data)");
            add("Spawning", sched.Active ? sched.Plan.Label : "random spawner only (nothing scheduled)");
            if (sched.ThenMap)
                add("Hand-over", sched.HandOver >= 0f ? Clock(sched.HandOver) : "never / depends on Max Total (preview keeps the milestone rules)");
            add("Enemy Point", $"start {map.startEnemyPoint:0.#}, +{map.rateIncreaseEnemyPoint:0.#} every {map.intervalIncreaseEnemyPoint:0.#}s, cap {map.maxEnemyPoint:0.#}. "
                               + "Random and pattern spawns refund their cost on death; scheduled spawns cost nothing.");
            add("Spawn Timer", map.canGrowth
                ? $"{map.defaultEnemySpawnTimer:0.###}s, -{map.decreaseAmount:0.###}s every {map.decreaseInterval:0.#}s, min {map.decreaseMinimum:0.###}s"
                : $"{map.defaultEnemySpawnTimer:0.###}s fixed (Can Growth OFF)");

            if (milestone != null)
            {
                var mods = MilestoneModLines(milestone);
                if (mods.Count > 0) add("Enemy Modify", string.Join("\n", mods));
            }
            add("Times", "m:ss = time since start; clock = the countdown the player sees");
        }

        private static void CollectStaticWarnings(MapDataSO map, RushDataSO rush, bool endless, float T, List<string> warnings)
        {
            if (!endless && rush != null && map.rushTime >= T)
                warnings.Add($"Rush Time ({map.rushTime:0.#}s) >= run length - Rush starts immediately.");
            if (map.playAllPatternIn > 0f && (map.PatternOptions == null || !map.PatternOptions.Any(p => p != null && p.enableThisPattern)))
                warnings.Add("Patterns are scheduled but no pattern is enabled.");

            int distinct = map.PatternOptions?.Count(p => p != null && p.enableThisPattern && p.pattern != null) ?? 0;
            if (!map.canDuplicateAfterHaveAllPattern && map.patternMax > distinct && distinct > 0)
                warnings.Add($"Pattern Max {map.patternMax:0} but only {distinct} enabled pattern(s) and duplicates are off - rotation never fills.");

            void costCheck(List<MapDataSO.EnemyOption> list, string where)
            {
                if (list == null) return;
                foreach (var e in list.Where(e => e != null && e.spawnPoint <= 0f))
                    warnings.Add($"{where} enemy '{e.id}' costs {e.spawnPoint} points - patterns spawn 0 of it.");
            }
            costCheck(map.EnemyOptions, "Map");
            if (rush != null) costCheck(rush.enemyOptions, "Rush");

            if (map.PatternOptions != null && map.EnemyOptions != null)
            {
                var cheapest = map.EnemyOptions.Where(e => e != null && !e.disableInPattern && e.spawnPoint > 0f)
                    .Select(e => e.spawnPoint).DefaultIfEmpty(0f).Min();
                foreach (var p in map.PatternOptions.Where(p => p != null && p.enableThisPattern && cheapest > 0f && p.patternPoint < cheapest))
                    warnings.Add($"Pattern '{PatternName(p)}' has {p.patternPoint:0.#}pt - less than the cheapest enemy ({cheapest:0.#}pt), it always spawns 0.");
            }
        }

        private static string BuildReference(MapDataSO map, RushDataSO rush, ChallengeDataSO milestone)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ENEMIES (map)");
            AppendEnemyTable(sb, map.EnemyOptions, milestone, false);
            if (rush != null)
            {
                sb.AppendLine();
                sb.AppendLine($"ENEMIES (Rush - {rush.name})");
                AppendEnemyTable(sb, rush.enemyOptions, milestone, true);
            }

            sb.AppendLine();
            sb.AppendLine("PATTERNS (map)");
            AppendPatternTable(sb, map.PatternOptions, map.EnemyOptions);
            if (rush != null)
            {
                sb.AppendLine();
                sb.AppendLine($"PATTERNS (Rush) - batch every {rush.playAllPatternIn:0.#}s, spread {rush.triggerAllPatternIn:0.#}s, max {rush.patternMax:0.#}");
                AppendPatternTable(sb, rush.patternOptions, rush.enemyOptions);
            }

            sb.AppendLine();
            sb.AppendLine("MAP EVENTS (map)");
            AppendEventTable(sb, map.eventmapOptions);
            if (rush != null)
            {
                sb.AppendLine();
                sb.AppendLine("MAP EVENTS (Rush)");
                if (rush.eventmapOptions == null || rush.eventmapOptions.Count == 0)
                    sb.AppendLine("  (none - the map's events keep running during Rush)");
                else AppendEventTable(sb, rush.eventmapOptions);
            }
            return sb.ToString().TrimEnd();
        }

        private static void AppendEnemyTable(StringBuilder sb, List<MapDataSO.EnemyOption> options, ChallengeDataSO milestone, bool rush)
        {
            if (options == null || options.Count == 0) { sb.AppendLine("  (none)"); return; }
            foreach (var e in options.Where(x => x != null))
            {
                var bits = new List<string>
                {
                    $"chance {e.chance:0.##}" + (e.disableNormalize ? " (fixed)" : ""),
                    $"cost {e.spawnPoint:0.##}pt"
                };
                if (e.enemyChanceCanGrowth)
                {
                    float growth = e.enemyChanceGrowthRate;
                    if (milestone != null && !rush && TryMilestonePercent(milestone, e.id, EnemyStat.SpawnChance, out float sc))
                        growth = Mathf.Max(0f, growth * (1f + sc / 100f));
                    bits.Add($"chance +{growth:0.##}/upgrade");
                }
                if (e.enemyPointCanGrowth) bits.Add($"cost +{e.enemyPointGrowthRate:0.##}%/upgrade");
                if (e.useCustomInterval) bits.Add($"next check {e.customInterval:0.##}s");
                if (e.useMaxperEnemy && e.maximumPerEnemy > 0) bits.Add($"max alive {e.maximumPerEnemy:0}");
                bits.Add("window " + EnemyWindow(e));
                if (e.disableInPattern) bits.Add("NOT in patterns");
                if (rush && milestone != null && TryMilestonePercent(milestone, e.id, EnemyStat.RushSetChance, out float set))
                    bits.Add($"milestone sets chance {set:0.##}");
                sb.AppendLine($"  {e.id}: " + string.Join(" | ", bits));
            }
        }

        private static void AppendPatternTable(StringBuilder sb, List<MapDataSO.PatternOption> patterns, List<MapDataSO.EnemyOption> enemies)
        {
            if (patterns == null || patterns.Count == 0) { sb.AppendLine("  (none)"); return; }
            var usable = enemies?.Where(e => e != null && !e.disableInPattern && e.spawnPoint > 0f).ToList() ?? new List<MapDataSO.EnemyOption>();
            foreach (var p in patterns.Where(x => x != null))
            {
                var bits = new List<string> { p.enableThisPattern ? "ON" : "OFF", $"{p.patternPoint:0.#}pt", "window " + PatternWindow(p) };
                if (usable.Count > 0)
                {
                    var cheap = usable.OrderBy(e => e.spawnPoint).First();
                    var dear = usable.OrderByDescending(e => e.spawnPoint).First();
                    bits.Add($"{cheap.id} x{Mathf.FloorToInt(p.patternPoint / cheap.spawnPoint)} .. {dear.id} x{Mathf.FloorToInt(p.patternPoint / dear.spawnPoint)}");
                }
                if (p.enableSpecificEnemy && p.specificEnemyList != null && p.specificEnemyList.Count > 0)
                    bits.Add("specific " + string.Join(", ", p.specificEnemyList.Select(k => $"{k.enemyID} {k.chance:0.#}")));
                if (p.bypassSpawnCondition) bits.Add("ignores spawn windows");
                bits.Add($"delay {p.delayBetweenEnemy:0.##}s/enemy {p.delayBetweenRows:0.##}s/row, frozen {p.enableMovementAfter:0.##}s");
                sb.AppendLine($"  {PatternName(p)}: " + string.Join(" | ", bits));
            }
        }

        private static void AppendEventTable(StringBuilder sb, List<MapDataSO.EventMapOption> events)
        {
            if (events == null || events.Count == 0) { sb.AppendLine("  (none)"); return; }
            foreach (var c in events.Where(x => x != null))
            {
                string interval = c.intervalCanModify
                    ? $"every {c.playInterval:0.##}s, {c.rateModify:+0.##;-0.##}s per {c.intervalModify:0.#}s (min {(c.minPlayInterval > 0 ? c.minPlayInterval : 0.1f):0.##}s)"
                    : $"every {c.playInterval:0.##}s";
                sb.AppendLine($"  {c.catagolyMapEvent}: {(c.enableThisMapEvent ? "ON" : "OFF")} | {interval} | {c.eventMapChance:0.#}% to play");
                if (c.allMapEventID == null) continue;
                foreach (var k in c.allMapEventID)
                    sb.AppendLine($"      - {k.mapEventID}: {(k.useWeightRandom ? "weight" : "fixed")} {k.chance:0.#} | window {KvWindow(k)}"
                                  + (k.overrideData ? $" | damage {k.damageMap:0.##}" : ""));
            }
        }

        #endregion

        #region Schedule / milestone helpers

        private static Sched BuildSched(MapDataSO map, ChallengeDataSO milestone, int index, List<string> warnings)
        {
            var plan = EnemySpawnScheduleResolver.ResolveForRun(map, milestone, index);
            var known = new HashSet<string>(map.EnemyOptions?.Where(e => e != null && !string.IsNullOrWhiteSpace(e.id)).Select(e => e.id)
                                             ?? Enumerable.Empty<string>());

            Phase build(SpawnScheduleSource src)
            {
                if (src == null) return null;
                var p = new Phase { Src = src };
                foreach (var r in src.Rules)
                {
                    if (!known.Contains(r.Rule.enemyId))
                    {
                        warnings.Add($"Scheduled enemy '{r.Rule.enemyId}' ({src.Label}) is not in the map's Enemy Options - skipped at runtime.");
                        continue;
                    }
                    p.Rules.Add(r);
                    p.Ids.Add(r.Rule.enemyId);
                }
                return p;
            }

            var s = new Sched { Plan = plan, First = build(plan.First), Second = build(plan.Second) };
            s.Active = (s.First != null && !s.First.Empty) || (s.Second != null && !s.Second.Empty);
            s.ThenMap = s.Second != null && plan.Flow == MilestoneSpawnFlow.ThenMapMode;
            s.Together = s.Second != null && plan.Flow == MilestoneSpawnFlow.Together;
            s.UnionIds = new HashSet<string>(s.First?.Ids ?? new HashSet<string>());
            if (s.Second != null) s.UnionIds.UnionWith(s.Second.Ids);

            if (s.ThenMap)
            {
                if (plan.HandOver == MilestoneHandOver.AtTime) s.HandOver = plan.HandOverAt;
                else
                {
                    float finish = EnemySpawnScheduleResolver.EstimateRulesFinish(plan.First);
                    s.HandOver = finish >= 0f && !float.IsNaN(finish) ? finish : -1f;
                }
            }
            return s;
        }

        private static float StatOf(EnemyStatBundle b, EnemyStat stat) => stat switch
        {
            EnemyStat.MaxHP => b.maxHP,
            EnemyStat.Damage => b.damage,
            EnemyStat.MoveSpeed => b.moveSpeed,
            EnemyStat.SpawnChance => b.spawnChance,
            EnemyStat.RushMaxHP => b.rushMaxHP,
            EnemyStat.RushDamage => b.rushDamage,
            EnemyStat.RushMoveSpeed => b.rushMoveSpeed,
            EnemyStat.RushSetChance => b.rushSetChance,
            _ => 0f
        };

        private static bool IsGlobalGroup(EnemyGroupMod g) =>
            g.enemyIds == null || g.enemyIds.Count == 0 || g.enemyIds.Any(r => string.IsNullOrWhiteSpace(r) || r.Trim() == "*");

        /// <summary>ChallengeManager aggregation for one milestone: global groups (unless excluded) + groups naming the id.</summary>
        private static bool TryMilestonePercent(ChallengeDataSO ch, string id, EnemyStat stat, out float value)
        {
            value = 0f;
            if (ch?.enemyGroups == null || string.IsNullOrEmpty(id)) return false;
            float global = 0f, specific = 0f;
            bool excluded = false;
            foreach (var g in ch.enemyGroups)
            {
                if (g == null || g.stats.IsZero) continue;
                float v = StatOf(g.stats, stat);
                if (IsGlobalGroup(g))
                {
                    global += v;
                    if (g.enemyNotincluded != null && g.enemyNotincluded.Any(x => x?.Trim() == id)) excluded = true;
                }
                else if (g.enemyIds.Any(x => x?.Trim() == id)) specific += v;
            }
            value = (excluded ? 0f : global) + specific;
            return !Mathf.Approximately(value, 0f);
        }

        private static List<string> MilestoneModLines(ChallengeDataSO ch)
        {
            var lines = new List<string>();
            if (ch.enemyGroups == null) return lines;
            foreach (var g in ch.enemyGroups)
            {
                if (g == null || g.stats.IsZero) continue;
                var b = g.stats;
                var parts = new List<string>();
                void add(string name, float v) { if (!Mathf.Approximately(v, 0f)) parts.Add($"{name} {v:+0.#;-0.#}%"); }
                add("HP", b.maxHP); add("DMG", b.damage); add("Speed", b.moveSpeed); add("Chance growth", b.spawnChance);
                add("Rush HP", b.rushMaxHP); add("Rush DMG", b.rushDamage); add("Rush Speed", b.rushMoveSpeed);
                if (!Mathf.Approximately(b.rushSetChance, 0f)) parts.Add($"Rush chance = {b.rushSetChance:0.#}");
                string who = IsGlobalGroup(g) ? "ALL enemies" : string.Join(", ", g.enemyIds);
                if (IsGlobalGroup(g) && g.enemyNotincluded != null && g.enemyNotincluded.Count > 0)
                    who += " except " + string.Join(", ", g.enemyNotincluded);
                lines.Add($"{who}: {string.Join(", ", parts)}");
            }
            return lines;
        }

        #endregion

        #region Formatting / text export

        private static string PatternName(MapDataSO.PatternOption p) => p?.pattern != null ? p.pattern.name : "<no pattern>";

        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        private static string Stamp(float t, float T, bool endless) =>
            endless ? Clock(t) : $"{Clock(t)} (clock {Clock(T - t)})";

        private static string TimeList(List<float> times, float T, bool endless, int max = 10)
        {
            var shown = times.Take(max).Select(Clock);
            return string.Join(", ", shown) + (times.Count > max ? $" ... (+{times.Count - max})" : "");
        }

        public static string PeriodTitle(Period p, Report r)
        {
            string range = $"{Clock(p.From)} - {Clock(p.To)}";
            string clock = r.Endless ? "" : $"   (clock {Clock(r.RunLength - p.From)} - {Clock(r.RunLength - p.To)})";
            string tag = p.Rush ? "RUSH" : p.AfterHandOver ? "MAP MODE (after hand-over)" : "NORMAL";
            return $"{range}  [{tag}]{clock}";
        }

        public static string EnemyPercent(EnemyLine e) =>
            Mathf.Abs(e.From - e.To) < 0.0005f ? $"{e.From * 100f:0.#}%" : $"{e.From * 100f:0.#}% -> {e.To * 100f:0.#}%";

        /// <summary>The same report as plain text (Copy / Log buttons).</summary>
        public static string ToText(Report r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BALANCE PREVIEW - " + r.Title);
            foreach (var kv in r.Summary) sb.AppendLine($"{kv.Key}: {kv.Value.Replace("\n", "\n    ")}");
            if (r.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("CHECK THESE:");
                foreach (var w in r.Warnings.Distinct()) sb.AppendLine(" ! " + w);
            }

            int i = 1;
            foreach (var p in r.Periods)
            {
                sb.AppendLine();
                sb.AppendLine($"=== Period {i++}: {PeriodTitle(p, r)} ===");
                sb.AppendLine($"Random spawner: {p.Spawner}");
                foreach (var e in p.Random) sb.AppendLine($"   {e.Id}: {EnemyPercent(e)}  ({e.Note})");
                if (p.NotYet.Count > 0) sb.AppendLine($"Not now: {string.Join(", ", p.NotYet)}");
                if (p.ScheduleActive)
                {
                    sb.AppendLine("Scheduled: " + (p.Scheduled.Count == 0 ? "- (no rule active)" : ""));
                    foreach (var s in p.Scheduled) sb.AppendLine("   " + s);
                }
                sb.AppendLine($"Enemy Point: {p.Points}");
                sb.AppendLine($"Pattern: {p.PatternHead}");
                foreach (var s in p.Patterns) sb.AppendLine("   " + s);
                sb.AppendLine($"   Pattern enemies: {p.PatternEnemies}");
                foreach (var ev in p.MapEvents)
                {
                    sb.AppendLine($"Map Event {ev.Name}: {ev.Rule} | {(ev.Times.Count == 0 ? "no roll in this period" : $"{ev.Times.Count} roll(s) at {TimeList(ev.Times, r.RunLength, r.Endless)}")}");
                    sb.AppendLine($"   {ev.Picks}");
                }
            }

            if (!string.IsNullOrEmpty(r.Log))
            {
                sb.AppendLine();
                sb.AppendLine("=== Event Log ===");
                sb.AppendLine(r.Log);
            }
            if (!string.IsNullOrEmpty(r.Reference))
            {
                sb.AppendLine();
                sb.AppendLine("=== Reference ===");
                sb.AppendLine(r.Reference);
            }
            return sb.ToString().TrimEnd();
        }

        #endregion

        #region Inspector drawing
#if UNITY_EDITOR
        /// <summary>Foldout / scroll state of the drawn report (kept by the MapDataSO, not saved).</summary>
        public class UiState
        {
            public bool log, reference;
            public readonly HashSet<int> collapsed = new();
            public Vector2 logScroll, refScroll;
        }

        private static GUIStyle _rich, _head, _title, _small;

        private static void EnsureStyles()
        {
            if (_rich != null) return;
            _rich = new GUIStyle(UnityEditor.EditorStyles.wordWrappedLabel) { richText = true };
            _small = new GUIStyle(_rich) { fontSize = Mathf.Max(9, _rich.fontSize - 1) };
            _head = new GUIStyle(UnityEditor.EditorStyles.foldout) { richText = true, fontStyle = FontStyle.Bold };
            _title = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { fontSize = 13, wordWrap = true };
        }

        public static void DrawGUI(Report r, UiState ui)
        {
            if (r == null) return;
            EnsureStyles();

            GUILayout.Label(r.Title, _title);
            foreach (var kv in r.Summary)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{kv.Key}</b>", _rich, GUILayout.Width(95));
                GUILayout.Label(kv.Value, _rich);
                GUILayout.EndHorizontal();
            }

            foreach (var w in r.Warnings.Distinct())
                UnityEditor.EditorGUILayout.HelpBox(w, UnityEditor.MessageType.Warning);

            GUILayout.Space(6);
            for (int i = 0; i < r.Periods.Count; i++) DrawPeriod(r, r.Periods[i], i, ui);

            if (!string.IsNullOrEmpty(r.Log))
                DrawTextFoldout(ref ui.log, ref ui.logScroll, "Event Log (exact times)", r.Log);
            if (!string.IsNullOrEmpty(r.Reference))
                DrawTextFoldout(ref ui.reference, ref ui.refScroll, "Reference Tables (base values)", r.Reference);
        }

        private static void DrawPeriod(Report r, Period p, int index, UiState ui)
        {
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = p.Rush ? new Color(1f, 0.55f, 0.55f) : p.AfterHandOver ? new Color(0.65f, 0.85f, 1f) : Color.white;
            UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
            GUI.backgroundColor = oldBg;

            bool open = !ui.collapsed.Contains(index);
            bool now = UnityEditor.EditorGUILayout.Foldout(open, $"Period {index + 1}:  {PeriodTitle(p, r)}", true, _head);
            if (now != open) { if (now) ui.collapsed.Remove(index); else ui.collapsed.Add(index); }

            if (now)
            {
                Section("Random", p.Spawner);
                foreach (var e in p.Random) DrawEnemyBar(e);
                if (p.NotYet.Count > 0) Section("Not now", string.Join(", ", p.NotYet), _small);

                if (p.ScheduleActive)
                    Section("Scheduled", p.Scheduled.Count == 0 ? "- (no rule active)" : "• " + string.Join("\n• ", p.Scheduled));

                Section("Enemy Point", p.Points);
                Section("Pattern", p.PatternHead
                                   + (p.Patterns.Count > 0 ? "\n• " + string.Join("\n• ", p.Patterns) : "\n• no pattern can play in this period")
                                   + $"\nPattern enemies: {p.PatternEnemies}");

                foreach (var ev in p.MapEvents)
                {
                    string times = ev.Times.Count == 0 ? "no roll in this period" : $"{ev.Times.Count} roll(s) at {TimeList(ev.Times, r.RunLength, r.Endless)}";
                    Section("Map Event", $"<b>{ev.Name}</b> — {ev.Rule}\n{times}\n{ev.Picks}");
                }
            }

            UnityEditor.EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private static void Section(string label, string text, GUIStyle style = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{label}</b>", _rich, GUILayout.Width(95));
            GUILayout.Label(text ?? "-", style ?? _rich);
            GUILayout.EndHorizontal();
        }

        private static void DrawEnemyBar(EnemyLine e)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(99);
            GUILayout.Label(e.Id, _rich, GUILayout.Width(130));
            var rect = GUILayoutUtility.GetRect(60, 14, GUILayout.Width(110));
            rect.y += 3;
            UnityEditor.EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            var fill = rect;
            fill.width = rect.width * Mathf.Clamp01(e.From);
            UnityEditor.EditorGUI.DrawRect(fill, new Color(0.35f, 0.75f, 0.45f));
            if (Mathf.Abs(e.To - e.From) >= 0.0005f)
            {
                var mark = rect;
                mark.x = rect.x + rect.width * Mathf.Clamp01(e.To) - 1;
                mark.width = 2;
                UnityEditor.EditorGUI.DrawRect(mark, new Color(1f, 0.85f, 0.2f));
            }
            GUILayout.Label($" {EnemyPercent(e)}", _rich, GUILayout.Width(110));
            GUILayout.Label(e.Note, _small);
            GUILayout.EndHorizontal();
        }

        private static void DrawTextFoldout(ref bool open, ref Vector2 scroll, string title, string text)
        {
            open = UnityEditor.EditorGUILayout.Foldout(open, title, true, _head);
            if (!open) return;
            var style = new GUIStyle(UnityEditor.EditorStyles.textArea) { wordWrap = true, richText = false };
            float width = UnityEditor.EditorGUIUtility.currentViewWidth - 60f;
            float height = style.CalcHeight(new GUIContent(text), width);
            scroll = UnityEditor.EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Min(400f, height + 10f)));
            UnityEditor.EditorGUILayout.SelectableLabel(text, style, GUILayout.Height(height));
            UnityEditor.EditorGUILayout.EndScrollView();
        }
#endif
        #endregion
    }
}
