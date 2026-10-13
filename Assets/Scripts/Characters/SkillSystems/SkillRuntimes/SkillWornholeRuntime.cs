using System.Collections.Generic;
using System.Threading;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GameControl.Controller;
using GameControl.EventMap;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Runtime for Bright2's "Wornhole" (phases 2-3): the boss tears open the map and drops a handful of
    /// the map's own content back onto it.
    /// <para/>
    /// A cast rolls one kind - enemy patterns OR map events - then spawns between Spawn Count Min and Max
    /// of it, one per interval. Nothing here is owned by the boss: patterns come from the map's pattern
    /// list and events from whatever the map has registered, so what appears is always map content.
    /// </summary>
    public class SkillWornholeRuntime : BaseSkillRuntime<SkillWornholeDataSo>
    {
        private readonly List<string> _eventIdBuffer = new();

        protected override void OnSkillStart()
        {
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            int count = Random.Range(skillData.MinSpawnCount, skillData.MaxSpawnCount + 1);
            if (count <= 0) return;

            bool useMapEvents = Random.Range(0f, 100f) < skillData.MapEventChance;

            if (useMapEvents)
            {
                BuildAvailableEventIds();

                // Nothing registered (or the filter matched nothing) - fall back rather than waste the cast.
                if (_eventIdBuffer.Count == 0) useMapEvents = false;
            }

            if (skillData.DelayBeforeFirstSpawn > 0f)
                await UniTask.WaitForSeconds(skillData.DelayBeforeFirstSpawn, cancellationToken: cancelToken);

            // Both kinds are paced the same way - one per interval, never all at once.
            for (int i = 0; i < count; i++)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (useMapEvents) SpawnRandomMapEvent();
                else SpawnEnemyPatterns(1);

                if (i < count - 1 && skillData.IntervalBetweenSpawns > 0f)
                    await UniTask.WaitForSeconds(skillData.IntervalBetweenSpawns, cancellationToken: cancelToken);
            }
        }

        protected override void OnSkillExit()
        {
            _eventIdBuffer.Clear();
        }

        private void SpawnEnemyPatterns(int count)
        {
            var patternController = SpawnerStateController.Instance
                ? SpawnerStateController.Instance.EnemyPatternController
                : null;

            if (patternController == null)
            {
                Debug.LogWarning("[Wornhole] No EnemyPatternController available - nothing spawned.", this);
                return;
            }

            // Triggers them as a one-off; the map's standing rotation is deliberately left alone.
            patternController.TriggerRandomPatterns(count, skillData.EnemyPatternFilter);
        }

        /// <summary>
        /// Collects the ids this cast may use: everything the map has registered, narrowed by the filter
        /// when one is set. An empty filter is the doc's "all available at that time".
        /// </summary>
        private void BuildAvailableEventIds()
        {
            _eventIdBuffer.Clear();
            if (!MapEventManager.Instance) return;

            var filter = skillData.MapEventIdFilter;
            bool hasFilter = filter != null && filter.Count > 0;

            foreach (var id in MapEventManager.Instance.AvailableEventIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (hasFilter && !filter.Contains(id)) continue;

                _eventIdBuffer.Add(id);
            }
        }

        private void SpawnRandomMapEvent()
        {
            if (_eventIdBuffer.Count == 0 || !MapEventManager.Instance) return;

            string id = _eventIdBuffer[Random.Range(0, _eventIdBuffer.Count)];
            MapEventManager.Instance.RunEvent(id, null);
        }
    }
}
