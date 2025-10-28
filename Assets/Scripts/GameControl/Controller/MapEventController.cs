using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameControl.EventMap;
using GameControl.SO;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameControl.Controller
{
    public class MapEventController
    {
        private MapDataSO _mapdata;
        private readonly SpawnerStateController _state;
        private readonly bool _debug;
        private readonly HashSet<string> _activeEventGroups = new();
        private readonly Dictionary<string, int> _lastFireFrame = new();
        
        public MapEventController(MapDataSO mapData, SpawnerStateController state, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _debug = debug;
        }
        
        #region Public Method
        public void ReloadAllEvents()
        {
            if (_mapdata?.eventmapOptions == null || _mapdata.eventmapOptions.Count == 0) return;
            CancelAllEventGroups();
            var startTotal = GameTimer.Instance.StartTimerNumber;
            var remainingNow = GameTimer.Instance.GlobalTimer;
            var elapsedNow = Mathf.Max(0f, startTotal - remainingNow);
            
            foreach (var opt in _mapdata.eventmapOptions)
            {
                if (opt == null || !opt.enableThisMapEvent) continue;
                ScheduleCategoryForwardFromNow(opt, elapsedNow, remainingNow);
            }

            if (_debug) Debug.Log("[MapEventController] ReloadAllEvents -> rebuilt schedules from 'now'.");
        }

        public void PlayRandomCategory()
        {
            foreach (var opt in _mapdata.eventmapOptions)
            {
                if (opt == null || !opt.enableThisMapEvent) continue;
                PlayMapEventCatagory(opt);
            }
        }

        public float CalculateMapEventInterval(MapDataSO.EventMapOption eventOption, float elapsedTime)
        {
            if (!eventOption.intervalCanModify)
                return eventOption.playInterval;

            var stepCount = Mathf.FloorToInt(elapsedTime / eventOption.intervalModify);
            var interval = eventOption.playInterval + eventOption.rateModify * stepCount;
            var minInterval = eventOption.minPlayInterval > 0 ? eventOption.minPlayInterval : 0.1f;

            return Mathf.Max(minInterval, interval);
        }

        public void PlaySpecificCategoryName(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                if (_debug) Debug.LogWarning("[PlaySpecificCategoryName] categoryName is null or empty.");
                return;
            }

            var eventOption = _mapdata.eventmapOptions.FirstOrDefault(e =>
                e.enableThisMapEvent && e.catagolyMapEvent.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (eventOption == null)
            {
                if (_debug)
                    Debug.LogWarning(
                        $"[PlaySpecificCategoryName] No enabled event category found with name '{categoryName}'.");
                return;
            }

            if (_debug) Debug.Log($"[PlaySpecificCategoryName] Playing event category '{categoryName}'.");
            PlayMapEventCatagory(eventOption);
        }
        #endregion

        #region Private Method

        private void PlayMapEventCatagory(MapDataSO.EventMapOption eventOption)
        {
            if (!IsEventChanceSuccessful(eventOption.eventMapChance)) return;

            var (elapsedNow, remainingNow) = GetTimeNow();
            var fired = new HashSet<string>();
            var candidates = eventOption.allMapEventID
                .Where(kv => IsKvEligible(kv, elapsedNow, remainingNow))
                .ToList();

            var weighted = candidates.Where(ev => ev.useWeightRandom).ToList();
            var nonWeighted = candidates.Where(ev => !ev.useWeightRandom).ToList();

            //Weighted
            var selected = SelectEventByChance(weighted);
            if (!string.IsNullOrEmpty(selected) && fired.Add(selected))
            {
                var idx = weighted.FindIndex(e => e.mapEventID == selected);
                if (idx >= 0)
                {
                    var kv = weighted[idx];
                    TriggerMapEvent(selected, kv);
                }
            }

            //Non-weighted
            foreach (var ev in nonWeighted)
                if (ev.chance >= 100f && fired.Add(ev.mapEventID))
                    TriggerMapEvent(ev.mapEventID, ev);
        }

        private bool IsEventChanceSuccessful(float eventChance)
        {
            var chanceRoll = Random.Range(0f, 100f);
            return chanceRoll <= eventChance;
        }
        
        private string SelectEventByChance(List<MapDataSO.EventMapOption.MapEventKv> events)
        {
            var totalChance = 0f;
            foreach (var ev in events)
                totalChance += ev.chance;

            var roll = Random.Range(0f, totalChance);
            var accum = 0f;
            foreach (var ev in events)
            {
                accum += ev.chance;
                if (roll <= accum)
                    return ev.mapEventID;
            }

            return null;
        }
        
        private void TriggerMapEvent(string eventID,MapDataSO.EventMapOption.MapEventKv kv)
        {
            int frame = Time.frameCount;
            if (_lastFireFrame.TryGetValue(eventID, out var last) && last == frame) return;
            _lastFireFrame[eventID] = frame;
            if (_debug) Debug.Log($"[TriggerMapEvent] {eventID} at remain={GameTimer.Instance.GlobalTimer:F2}s");

            if (kv.overrideData)
            {
                MapEventManager.Instance.RunEvent(eventID, o => o
                    .ForEntry(e =>
                    {
                        e.damage = kv.damageMap;
                    }));
            }
            else
            {
                MapEventManager.Instance.RunEvent(eventID, null);
            }
        }

        private static string BuildGroupId(MapDataSO.EventMapOption opt)
        {
            return $"EVENT::{opt.catagolyMapEvent}";
        }

        private void CancelAllEventGroups()
        {
            foreach (var g in _activeEventGroups)
                GameTimer.Instance.CancelGroup(g);
            _activeEventGroups.Clear();
        }
        
        private void ScheduleCategoryForwardFromNow(MapDataSO.EventMapOption opt, float elapsedStart,
            float remainingStart)
        {
            if (opt == null || remainingStart <= 0f) return;

            var group = BuildGroupId(opt);
            GameTimer.Instance.CancelGroup(group);
            _activeEventGroups.Add(group);
            
            var elapsed = Mathf.Max(0f, elapsedStart);
            var remaining = Mathf.Max(0f, remainingStart);

            while (remaining > 0f)
            {
                var interval = Mathf.Max(0.0001f, CalculateMapEventInterval(opt, elapsed));
                var nextRemain = remaining - interval;
                if (nextRemain < 0f) break;

                var fireAtRemain = nextRemain;

                GameTimer.Instance.ScheduleTrigger(
                    fireAtRemain,
                    () =>
                    {
                        if (_debug) Debug.Log($"[MapEventController] (Reload) '{opt.catagolyMapEvent}' fire at remain={GameTimer.Instance.GlobalTimer:F2}s");
                        PlayMapEventCatagory(opt);
                    },
                    false,
                    group
                );

                elapsed += interval;
                remaining = nextRemain;
            }
        }
        #endregion
        
        #region Condition Helper
        
        private bool IsKvEligible(MapDataSO.EventMapOption.MapEventKv kv, float elapsed, float remaining)
        {
            if (!kv.useCondition || kv.mapEventCondition == null || kv.mapEventCondition.Count == 0) return true;
            foreach (var cond in kv.mapEventCondition)
                if (!IsConditionPass(cond, elapsed, remaining)) return false;
            return true;
        }
        
        private (float elapsed, float remaining) GetTimeNow()
        {
            var timer = GameTimer.Instance;
            float remaining = Mathf.Max(timer.GlobalTimer, 0f);
            float elapsed = (_mapdata != null && _mapdata.endlessMode)
                ? remaining  // endless
                : Mathf.Max(timer.StartTimerNumber - remaining, 0f); // not endless

            return (elapsed, remaining);
        }
        
        private bool IsConditionPass(MapDataSO.EventMapOption.MapEventKv.MapEventConditionStruct cond, float elapsed, float remaining)
        {
            switch (cond.conditionType)
            {
                case MapDataSO.EventMapOption.MapEventKv.MapEventConditionType.TimeCondition:
                    if (elapsed < cond.startAfter) return false;
                    if (cond.endAt >= 0f && elapsed > cond.endAt) return false;
                    return true;

                default:
                    return true;
            }
        }


        #endregion
    }
}