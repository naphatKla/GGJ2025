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
    /// Runtime side of Enemy Spawn Mode (a map's Sequential list, or a milestone's Override rules). Ticked by
    /// <see cref="SpawnerState.SpawningState"/> next to the old random spawner.
    /// <para/>
    /// In Conditions mode - or when the schedule resolves to nothing, the agreed fallback - it stays inactive,
    /// never spawns and never touches the random spawner, so existing maps behave exactly as before.
    /// </summary>
    public class EnemySpawnScheduleController
    {
        private class RuleRuntime
        {
            public ResolvedSpawnRule Resolved;
            public float NextWaveAt = -1f; // -1 = not started yet
            public int SpawnedTotal;

            public void Reset()
            {
                NextWaveAt = -1f;
                SpawnedTotal = 0;
            }
        }

        private readonly MapDataSO _map;
        private readonly SpawnScheduleSource _source;
        private readonly EnemySpawnerController _spawner;
        private readonly SpawnerStateController _state;
        private readonly bool _debug;
        private readonly List<RuleRuntime> _rules = new();
        private readonly HashSet<string> _scheduledIds = new();

        private int _cycleIndex;
        private float _lastElapsed = -1f;

        /// <summary>True when a schedule is actually running (not Conditions / not fallen back).</summary>
        public bool IsScheduleActive { get; }

        public int MilestoneIndex { get; }

        /// <summary>Where the running rules came from (map mode or a milestone override).</summary>
        public string SourceLabel => _source?.Label;

        /// <summary>Whether the old random spawner may run (outside Rush - Rush always allows it).</summary>
        public bool AllowsRandomSpawner => !IsScheduleActive || _source.MixWithConditions;

        /// <param name="source">From <see cref="EnemySpawnScheduleResolver.ResolveForRun"/> - map mode or milestone override.</param>
        public EnemySpawnScheduleController(MapDataSO map, EnemySpawnerController spawner,
            SpawnerStateController state, SpawnScheduleSource source, int milestoneIndex, bool debug)
        {
            _map = map;
            _source = source ?? new SpawnScheduleSource { Label = "none" };
            _spawner = spawner;
            _state = state;
            _debug = debug;
            MilestoneIndex = milestoneIndex;

            if (map == null || spawner == null || _source.IsEmpty) return;

            foreach (var r in _source.Rules)
            {
                if (!spawner.HasEnemyOption(r.Rule.enemyId))
                {
                    Debug.LogWarning($"[SpawnSchedule] '{r.Rule.enemyId}' is not in {map.name}'s Enemy Options - skipped.");
                    continue;
                }

                _rules.Add(new RuleRuntime { Resolved = r });
                _scheduledIds.Add(r.Rule.enemyId);
            }

            IsScheduleActive = _rules.Count > 0;

            if (!IsScheduleActive)
                Debug.Log($"[SpawnSchedule] {map.name}: {_source.Label} has no usable rules - falling back to Conditions.");
            else if (_debug)
                Debug.Log($"[SpawnSchedule] {map.name}: {_source.Label}, {_rules.Count} rule(s), mix={_source.MixWithConditions}");
        }

        /// <summary>Filter for the random spawner while mixing, or null when it should not be filtered at all.</summary>
        public Func<MapDataSO.EnemyOption, bool> BuildRandomPoolFilter()
        {
            if (!IsScheduleActive || !_source.MixWithConditions) return null;

            return _source.RandomPool switch
            {
                RandomSpawnerPool.OnlyScheduledEnemies => opt => InRush() || _scheduledIds.Contains(opt.id),
                RandomSpawnerPool.ExcludeScheduledEnemies => opt => InRush() || !_scheduledIds.Contains(opt.id),
                _ => null
            };
        }
        /// <summary>Called every frame while the spawner is in its Spawning state and the map is NOT in Rush.</summary>
        public void Tick()
        {
            if (!IsScheduleActive) return;

            float elapsed = GetElapsed();

            // Timer went backwards = a new run on the same map (retry). Start the schedule over.
            if (_lastElapsed >= 0f && elapsed + 0.5f < _lastElapsed)
            {
                _cycleIndex = 0;
                foreach (var rt in _rules) rt.Reset();
            }
            _lastElapsed = elapsed;

            float localElapsed = ToCycleTime(elapsed);

            foreach (var rt in _rules)
                TickRule(rt, localElapsed);
        }

        private void TickRule(RuleRuntime rt, float t)
        {
            var r = rt.Resolved;
            var rule = r.Rule;

            if (t < r.Start) return;
            if (r.Expire >= 0f && t >= r.Expire) return;
            if (rule.maxTotal > 0 && rt.SpawnedTotal >= rule.maxTotal) return;

            if (rt.NextWaveAt < 0f)
                rt.NextWaveAt = rule.spawnOnStart ? r.Start : r.Start + rule.RollInterval(_state.EnemySpawnTimer);

            if (t < rt.NextWaveAt) return;

            rt.NextWaveAt = t + rule.RollInterval(_state.EnemySpawnTimer);

            if (rule.chance < 100f && UnityEngine.Random.Range(0f, 100f) >= rule.chance)
            {
                if (_debug) Debug.Log($"[SpawnSchedule] {rule.enemyId}: wave skipped by chance at {t:0.0}s");
                return;
            }

            int amount = rule.RollAmount();
            if (rule.maxTotal > 0) amount = Mathf.Min(amount, rule.maxTotal - rt.SpawnedTotal);
            if (amount <= 0) return;

            if (rule.burstDelay > 0f && amount > 1)
                SpawnBurstAsync(rt, amount).Forget();
            else
                for (int i = 0; i < amount; i++) SpawnOne(rt);

            if (_debug) Debug.Log($"[SpawnSchedule] {rule.enemyId}: wave x{amount} at {t:0.0}s (total {rt.SpawnedTotal})");
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
                    rule.playSpawnEffects, position))
                rt.SpawnedTotal++;
        }

        /// <summary>Sequential + Loop: map elapsed time into one pass of the list, resetting counters per pass.</summary>
        private float ToCycleTime(float elapsed)
        {
            if (_source.CycleLength <= 0f) return elapsed;

            float offset = _source.CycleOffset;
            if (elapsed < offset) return elapsed;

            int cycle = Mathf.FloorToInt((elapsed - offset) / _source.CycleLength);
            if (cycle != _cycleIndex)
            {
                _cycleIndex = cycle;
                foreach (var rt in _rules) rt.Reset();
            }

            return offset + (elapsed - offset) - cycle * _source.CycleLength;
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
                return $"inactive = Conditions ({_source.Label}, milestone {MilestoneIndex})";
            return $"{_source.Label} | mix {(_source.MixWithConditions ? "on" : "off")}\n" + string.Join("\n", _rules.Select(rt =>
                $"{rt.Resolved.Rule.enemyId}: {rt.Resolved.Start:0.#}s-{(rt.Resolved.Expire < 0 ? "end" : rt.Resolved.Expire.ToString("0.#") + "s")} "
                + $"spawned {rt.SpawnedTotal}, next {(rt.NextWaveAt < 0 ? "-" : rt.NextWaveAt.ToString("0.0"))}"));
        }
    }
}
