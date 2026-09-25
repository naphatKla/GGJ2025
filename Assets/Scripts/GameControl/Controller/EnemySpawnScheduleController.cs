using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.SO;
using UnityEngine;

namespace GameControl.Controller
{
    /// <summary>
    /// Runtime side of Enemy Spawn Mode: a map's Sequential list and/or a milestone's Override rules, run as up to
    /// two phases (<see cref="SpawnSchedulePlan"/>). Ticked by <see cref="SpawnerState.SpawningState"/> next to
    /// the old random spawner.
    /// <para/>
    /// With nothing scheduled (Conditions map, no milestone override - every existing map) it stays inactive,
    /// never spawns and never touches the random spawner, so the game behaves exactly as before.
    /// </summary>
    public class EnemySpawnScheduleController
    {
        private class RuleRuntime
        {
            public ResolvedSpawnRule Resolved;
            public float NextWaveAt = -1f; // -1 = not started yet
            public int SpawnedTotal;

            // ---- debug counters (shown by DebugSummary / the Spawn Schedule box on SpawnerStateController) ----
            public int Waves;
            public int WavesSkippedByChance;
            public int Blocked;
            public string Status = "not ticked yet";
            public string LastBlockReason;

            public void Reset()
            {
                NextWaveAt = -1f;
                SpawnedTotal = 0;
                Waves = 0;
                WavesSkippedByChance = 0;
                Blocked = 0;
                Status = "reset";
                LastBlockReason = null;
            }

            /// <summary>Done for good: past its Expire or out of Max Total.</summary>
            public bool IsFinished(float t) =>
                (Resolved.Expire >= 0f && t >= Resolved.Expire)
                || (Resolved.Rule.maxTotal > 0 && SpawnedTotal >= Resolved.Rule.maxTotal);
        }

        private class Phase
        {
            public SpawnScheduleSource Source;
            public readonly List<RuleRuntime> Rules = new();
            public readonly HashSet<string> Ids = new();
            public int CycleIndex;

            public void Reset()
            {
                CycleIndex = 0;
                foreach (var rt in Rules) rt.Reset();
            }
        }

        private readonly MapDataSO _map;
        private readonly SpawnSchedulePlan _plan;
        private readonly EnemySpawnerController _spawner;
        private readonly SpawnerStateController _state;
        private readonly bool _debug;
        private readonly Phase _first;
        private readonly Phase _second; // null = single phase

        private float _lastElapsed = -1f;
        private float _handedOverAt = -1f; // Then Map Mode: elapsed time of the hand-over, -1 = not yet
        private HashSet<string> _togetherIds; // Together: ids of both phases, built on first use

        /// <summary>True when anything is scheduled at all (otherwise the random spawner runs as before).</summary>
        public bool IsScheduleActive { get; }

        public int MilestoneIndex { get; }

        public string SourceLabel => _plan?.Label;

        private bool IsThenMapMode => _second != null && _plan.Flow == MilestoneSpawnFlow.ThenMapMode;
        private bool IsTogether => _second != null && _plan.Flow == MilestoneSpawnFlow.Together;
        private bool HandedOver => _handedOverAt >= 0f;

        /// <summary>Whether the old random spawner may run right now (outside Rush - Rush always allows it).</summary>
        public bool AllowsRandomSpawner
        {
            get
            {
                if (!IsScheduleActive) return true;
                if (IsTogether) return _first.Source.RandomSpawnerOn || _second.Source.RandomSpawnerOn;
                return CurrentPhase.Source.RandomSpawnerOn;
            }
        }

        private Phase CurrentPhase => IsThenMapMode && HandedOver ? _second : _first;

        /// <param name="plan">From <see cref="EnemySpawnScheduleResolver.ResolveForRun"/>.</param>
        public EnemySpawnScheduleController(MapDataSO map, EnemySpawnerController spawner,
            SpawnerStateController state, SpawnSchedulePlan plan, int milestoneIndex, bool debug)
        {
            _map = map;
            _plan = plan ?? new SpawnSchedulePlan { First = new SpawnScheduleSource { Label = "none" }, Label = "none" };
            _spawner = spawner;
            _state = state;
            _debug = debug;
            MilestoneIndex = milestoneIndex;

            _first = BuildPhase(_plan.First);
            if (_plan.Second != null) _second = BuildPhase(_plan.Second);

            IsScheduleActive = map != null && spawner != null
                               && (_first.Rules.Count > 0 || (_second != null && _second.Rules.Count > 0));

            if (map == null) return;
            if (!IsScheduleActive)
            {
                if (_plan.HasAnyRules)
                    Debug.Log($"[SpawnSchedule] {map.name}: {_plan.Label} has no usable rules - falling back to Conditions.");
            }
            else if (_debug)
            {
                Debug.Log($"[SpawnSchedule] {map.name}: {_plan.Label} ({_plan.Flow}), "
                          + $"{_first.Rules.Count}+{_second?.Rules.Count ?? 0} rule(s)");
            }
        }

        private Phase BuildPhase(SpawnScheduleSource source)
        {
            var phase = new Phase { Source = source ?? new SpawnScheduleSource { Label = "none" } };
            if (_spawner == null) return phase;

            foreach (var r in phase.Source.Rules)
            {
                if (!_spawner.HasEnemyOption(r.Rule.enemyId))
                {
                    Debug.LogWarning($"[SpawnSchedule] '{r.Rule.enemyId}' is not in {_map?.name}'s Enemy Options - skipped.");
                    continue;
                }

                phase.Rules.Add(new RuleRuntime { Resolved = r });
                phase.Ids.Add(r.Rule.enemyId);
            }

            return phase;
        }

        /// <summary>
        /// Filter for the random spawner. It follows whichever phase is active, so it is installed once and
        /// stays valid across the hand-over. Null = the random spawner is never filtered by this plan.
        /// </summary>
        public Func<MapDataSO.EnemyOption, bool> BuildRandomPoolFilter()
        {
            if (!IsScheduleActive) return null;

            bool NeedsFilter(Phase p) => p != null && !p.Source.IsEmpty && p.Source.MixWithConditions
                                         && p.Source.RandomPool != RandomSpawnerPool.AllMapEnemies;
            if (!NeedsFilter(_first) && !NeedsFilter(_second)) return null;

            return opt => InRush() || RandomPoolAllows(opt.id);
        }

        /// <summary>
        /// Filter for Enemy Patterns (and Wornhole) following the active phase's Pattern Enemy Pool, or null
        /// when no phase restricts patterns. Rush always lifts it.
        /// </summary>
        public Func<MapDataSO.EnemyOption, bool> BuildPatternPoolFilter()
        {
            if (!IsScheduleActive) return null;

            bool Restricts(Phase p) => p != null && !p.Source.IsEmpty && p.Source.PatternPool != PatternEnemyPool.AllMapEnemies;
            if (!Restricts(_first) && !Restricts(_second)) return null;

            return opt => InRush() || PatternPoolAllows(opt.id);
        }

        private bool PatternPoolAllows(string id)
        {
            Phase phase = IsTogether ? _first : CurrentPhase;
            if (phase == null || phase.Source.IsEmpty) return true;

            switch (phase.Source.PatternPool)
            {
                case PatternEnemyPool.NoEnemies:
                    return false;
                case PatternEnemyPool.OnlyScheduledEnemies:
                    if (IsTogether)
                    {
                        _togetherIds ??= new HashSet<string>(_first.Ids.Concat(_second.Ids));
                        return _togetherIds.Contains(id);
                    }
                    return phase.Ids.Contains(id);
                default:
                    return true;
            }
        }

        private bool RandomPoolAllows(string id)
        {
            SpawnScheduleSource source;
            HashSet<string> ids;

            if (IsTogether)
            {
                // The milestone's pool wins while it mixes; otherwise the map's.
                bool useFirst = _first.Source.MixWithConditions;
                source = useFirst ? _first.Source : _second.Source;
                if (_togetherIds == null)
                {
                    _togetherIds = new HashSet<string>(_first.Ids);
                    _togetherIds.UnionWith(_second.Ids);
                }
                ids = _togetherIds;
            }
            else
            {
                source = CurrentPhase.Source;
                ids = CurrentPhase.Ids;
            }

            if (source.IsEmpty || !source.MixWithConditions) return true;
            return source.RandomPool switch
            {
                RandomSpawnerPool.OnlyScheduledEnemies => ids.Contains(id),
                RandomSpawnerPool.ExcludeScheduledEnemies => !ids.Contains(id),
                _ => true
            };
        }

        /// <summary>Called every frame while the spawner is in its Spawning state and the map is NOT in Rush.</summary>
        public void Tick()
        {
            if (!IsScheduleActive) return;

            float elapsed = GetElapsed();

            // Timer went backwards = a new run on the same map (retry). Start everything over.
            if (_lastElapsed >= 0f && elapsed + 0.5f < _lastElapsed)
            {
                _first.Reset();
                _second?.Reset();
                _handedOverAt = -1f;
            }
            _lastElapsed = elapsed;

            if (IsTogether)
            {
                TickPhase(_first, elapsed);
                TickPhase(_second, elapsed);
                return;
            }

            if (IsThenMapMode && !HandedOver && ShouldHandOver(elapsed))
            {
                _handedOverAt = elapsed;
                if (_debug) Debug.Log($"[SpawnSchedule] Hand-over at {elapsed:0.0}s -> {_second.Source.Label}");
            }

            if (IsThenMapMode && HandedOver)
            {
                float t = _plan.SecondStartsAtHandOver ? elapsed - _handedOverAt : elapsed;
                TickPhase(_second, t);
            }
            else
            {
                TickPhase(_first, elapsed);
            }
        }

        private bool ShouldHandOver(float elapsed)
        {
            if (_plan.HandOver == MilestoneHandOver.AtTime) return elapsed >= _plan.HandOverAt;
            return _first.Rules.Count == 0 || _first.Rules.All(rt => rt.IsFinished(elapsed));
        }

        private void TickPhase(Phase phase, float elapsed)
        {
            if (phase == null || phase.Rules.Count == 0) return;
            float t = ToCycleTime(phase, elapsed);
            foreach (var rt in phase.Rules)
                TickRule(rt, t);
        }

        private void TickRule(RuleRuntime rt, float t)
        {
            var r = rt.Resolved;
            var rule = r.Rule;

            if (t < r.Start)
            {
                rt.Status = $"waiting for Start At ({r.Start - t:0.0}s left)";
                return;
            }
            if (rt.IsFinished(t))
            {
                rt.Status = r.Expire >= 0f && t >= r.Expire ? "done (Expire At passed)" : "done (Max Total reached)";
                return;
            }

            if (rt.NextWaveAt < 0f)
                rt.NextWaveAt = rule.spawnOnStart ? r.Start : r.Start + rule.RollInterval(_state.EnemySpawnTimer);

            if (t < rt.NextWaveAt)
            {
                if (!rt.Status.StartsWith("wave skipped") && !rt.Status.StartsWith("BLOCKED"))
                    rt.Status = $"waiting next wave ({rt.NextWaveAt - t:0.0}s)";
                return;
            }

            rt.NextWaveAt = t + rule.RollInterval(_state.EnemySpawnTimer);
            rt.Waves++;

            float waveChance = rule.EffectiveChance;
            if (waveChance < 100f && UnityEngine.Random.Range(0f, 100f) >= waveChance)
            {
                rt.WavesSkippedByChance++;
                rt.Status = $"wave skipped by Wave Chance {waveChance:0.##}% ({rt.WavesSkippedByChance}/{rt.Waves} skipped)";
                if (_debug) Debug.Log($"[SpawnSchedule] {rule.enemyId}: wave skipped by chance ({waveChance:0.##}%) at {t:0.0}s");
                return;
            }

            int amount = rule.RollAmount();
            if (rule.maxAlive > 0)
            {
                int room = rule.maxAlive - _spawner.CountScheduledAlive(rt);
                amount = rule.fillToMaxEachWave ? room : Mathf.Min(amount, room);
                if (amount <= 0)
                {
                    rt.Status = $"Max Alive full ({rule.maxAlive}/{rule.maxAlive}) - refills when one dies";
                    return;
                }
            }
            if (rule.maxTotal > 0) amount = Mathf.Min(amount, rule.maxTotal - rt.SpawnedTotal);
            if (amount <= 0) return;

            rt.Status = $"spawning wave x{amount}";
            if (rule.burstDelay > 0f && amount > 1)
                SpawnBurstAsync(rt, amount).Forget();
            else
                for (int i = 0; i < amount; i++) SpawnOne(rt);

            if (_debug) Debug.Log($"[SpawnSchedule] {rule.enemyId}: wave x{amount} at {t:0.0}s (total {rt.SpawnedTotal})"
                                  + (rt.LastBlockReason != null && rt.Status.StartsWith("BLOCKED") ? $" - {rt.Status}" : ""));
        }
        private async UniTaskVoid SpawnBurstAsync(RuleRuntime rt, int amount)
        {
            var token = GameStateController.Instance?.sceneCts?.Token ?? CancellationToken.None;
            try
            {
                for (int i = 0; i < amount; i++)
                {
                    if (i > 0)
                        await UniTask.Delay(TimeSpan.FromSeconds(rt.Resolved.Rule.burstDelay), DelayType.DeltaTime,
                            cancellationToken: token);
                    if (InRush()) return; // Rush took over mid-wave
                    SpawnOne(rt);
                }
            }
            catch (OperationCanceledException)
            {
                // scene / run ended
            }
        }

        private void SpawnOne(RuleRuntime rt)
        {
            var rule = rt.Resolved.Rule;
            if (rule.maxTotal > 0 && rt.SpawnedTotal >= rule.maxTotal) return;

            Vector2? position = null;
            if (rule.spawnPosition == ScheduledSpawnPosition.AroundPlayer && PlayerController.Instance)
            {
                float min = Mathf.Min(rule.radiusMin, rule.radiusMax);
                float max = Mathf.Max(rule.radiusMin, rule.radiusMax);
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                position = (Vector2)PlayerController.Instance.transform.position + dir * UnityEngine.Random.Range(min, max);
            }

            if (_spawner.TrySpawnScheduled(rule.enemyId, rule.respectPerEnemyMax, rule.checkSpawnConditions,
                    rule.playSpawnEffects, position, rt, out string failReason))
            {
                rt.SpawnedTotal++;
                rt.Status = $"spawned (total {rt.SpawnedTotal})";
                return;
            }

            rt.Blocked++;
            rt.LastBlockReason = failReason;
            rt.Status = $"BLOCKED: {failReason}";
        }
        /// <summary>Sequential + Loop: map a phase's time into one pass of its list, resetting counters per pass.</summary>
        private static float ToCycleTime(Phase phase, float elapsed)
        {
            float cycleLength = phase.Source.CycleLength;
            if (cycleLength <= 0f) return elapsed;

            float offset = phase.Source.CycleOffset;
            if (elapsed < offset) return elapsed;

            int cycle = Mathf.FloorToInt((elapsed - offset) / cycleLength);
            if (cycle != phase.CycleIndex)
            {
                phase.CycleIndex = cycle;
                foreach (var rt in phase.Rules) rt.Reset();
            }

            return offset + (elapsed - offset) - cycle * cycleLength;
        }

        /// <summary>Seconds since the run started - same formula as TimeWindowEnemySpawnCondition (endless-aware).</summary>
        private float GetElapsed()
        {
            var timer = GameTimer.Instance;
            if (timer == null) return 0f;
            return _map.endlessMode
                ? Mathf.Max(timer.GlobalTimer, 0f)
                : Mathf.Max(timer.StartTimerNumber - timer.GlobalTimer, 0f);
        }

        private static bool InRush() =>
            GameStateController.Instance != null && GameStateController.Instance.MapState == MapState.Rush;

        public string DebugSummary()
        {
            if (!IsScheduleActive)
                return $"inactive = Conditions ({_plan.Label}, milestone {MilestoneIndex})";

            string header = $"t={_lastElapsed:0.0}s | {_plan.Label} | {_plan.Flow}";
            if (IsThenMapMode) header += HandedOver ? $" | handed over at {_handedOverAt:0.0}s" : " | before hand-over";
            header += $" | random spawner {(AllowsRandomSpawner ? "ON" : "OFF")}";
            var patternPhase = IsTogether ? _first : CurrentPhase;
            header += $" | patterns {(patternPhase == null || patternPhase.Source.IsEmpty ? PatternEnemyPool.AllMapEnemies : patternPhase.Source.PatternPool)}";

            var lines = new List<string> { header };
            string firstNote = IsThenMapMode && HandedOver ? "stopped (handed over)" : null;
            string secondNote = IsThenMapMode && !HandedOver ? "waiting for hand-over" : null;
            AddLines(lines, _first, IsThenMapMode ? "1)" : "", firstNote);
            if (_second != null) AddLines(lines, _second, IsThenMapMode ? "2)" : "+", secondNote);
            return string.Join("\n", lines);
        }

        /// <summary>Writes the full per-rule report to the Console (button on SpawnerStateController).</summary>
        public void LogReport() => Debug.Log("[SpawnSchedule] Report\n" + DebugSummary());

        private void AddLines(List<string> lines, Phase phase, string prefix, string phaseNote)
        {
            if (phase.Rules.Count == 0)
            {
                lines.Add($"{prefix} {phase.Source.Label}: random spawner (Conditions)".Trim());
                return;
            }

            foreach (var rt in phase.Rules)
            {
                var rule = rt.Resolved.Rule;
                string window = $"{rt.Resolved.Start:0.#}s-{(rt.Resolved.Expire < 0 ? "end" : rt.Resolved.Expire.ToString("0.#") + "s")}";
                string alive = $"alive {_spawner.CountScheduledAlive(rt)}{(rule.maxAlive > 0 ? "/" + rule.maxAlive : "")}, ";
                string counts = alive + $"spawned {rt.SpawnedTotal}{(rule.maxTotal > 0 ? "/" + rule.maxTotal : "")}, "
                                + $"waves {rt.Waves} (chance-skipped {rt.WavesSkippedByChance}), blocked {rt.Blocked}";
                lines.Add($"{prefix} {rule.enemyId} [{window}] {counts} | {phaseNote ?? rt.Status}".Trim());
            }
        }
    }
}