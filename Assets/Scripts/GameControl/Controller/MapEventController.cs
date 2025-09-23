using System;
using System.Collections.Generic;
using System.Linq;
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


        public MapEventController(MapDataSO mapData, SpawnerStateController state, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _debug = debug;
        }

        public void ReloadMapData(MapDataSO newMap)
        {
            CancelAllEventGroups();
            _mapdata = newMap;
            ReloadAllEvents();
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

        public void ScheduleAllTriggersUpfront(float maxTime)
        {
            Debug.Log($"{maxTime} Reload Schedule");
            foreach (var eventOption in _mapdata.eventmapOptions)
            {
                if (!eventOption.enableThisMapEvent) continue;

                var elapsedTime = 0f;
                var nextTriggerTime = 0f;
                var startTimer = GameTimer.Instance.StartTimerNumber;

                if (_debug) Debug.Log($"[ScheduleAllTriggersUpfront] Start scheduling for event category '{eventOption.catagolyMapEvent}' with maxTime {maxTime}");

                while (nextTriggerTime < maxTime)
                {
                    var interval = CalculateMapEventInterval(eventOption, elapsedTime);
                    nextTriggerTime += interval;

                    var triggerTime = startTimer - nextTriggerTime;

                    if (triggerTime < 0) break;

                    if (_debug)
                        Debug.Log(
                            $"[ScheduleAllTriggersUpfront] Scheduling '{eventOption.catagolyMapEvent}' trigger at timer = {triggerTime:F2}s (interval = {interval:F2}s, elapsed = {elapsedTime:F2}s)");

                    GameTimer.Instance.ScheduleTrigger(triggerTime, () =>
                    {
                        if (_debug)
                            Debug.Log(
                                $"[PlayMapEventCatagory] Triggered event category '{eventOption.catagolyMapEvent}' at timer = {GameTimer.Instance.GlobalTimer:F2}s");
                        PlayMapEventCatagory(eventOption);
                    }, false);

                    elapsedTime = nextTriggerTime;
                }
            }
        }

        private void PlayMapEventCatagory(MapDataSO.EventMapOption eventOption)
        {
            if (!IsEventChanceSuccessful(eventOption.eventMapChance)) return;

            var weightedEvents = GetWeightedEvents(eventOption);
            var nonWeightedEvents = GetNonWeightedEvents(eventOption);

            TriggerWeightedEvent(weightedEvents);
            TriggerNonWeightedEvents(nonWeightedEvents);
        }

        private bool IsEventChanceSuccessful(float eventChance)
        {
            var chanceRoll = Random.Range(0f, 100f);
            return chanceRoll <= eventChance;
        }

        private List<MapDataSO.EventMapOption.MapEventKv> GetWeightedEvents(MapDataSO.EventMapOption eventOption)
        {
            return eventOption.allMapEventID.Where(ev => ev.useWeightRandom).ToList();
        }

        private List<MapDataSO.EventMapOption.MapEventKv> GetNonWeightedEvents(MapDataSO.EventMapOption eventOption)
        {
            return eventOption.allMapEventID.Where(ev => !ev.useWeightRandom).ToList();
        }

        private void TriggerWeightedEvent(List<MapDataSO.EventMapOption.MapEventKv> weightedEvents)
        {
            if (weightedEvents.Count == 0) return;

            var selectedEventID = SelectEventByChance(weightedEvents);
            if (!string.IsNullOrEmpty(selectedEventID)) TriggerMapEvent(selectedEventID);
        }

        private void TriggerNonWeightedEvents(List<MapDataSO.EventMapOption.MapEventKv> nonWeightedEvents)
        {
            foreach (var ev in nonWeightedEvents)
                if (ev.chance >= 100f)
                    TriggerMapEvent(ev.mapEventID);
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


        private void TriggerMapEvent(string eventID)
        {
            if (_debug) Debug.Log($"[TriggerMapEvent] Triggering map event ID: {eventID} at timer = {GameTimer.Instance.GlobalTimer:F2}s");
            MapEventManager.Instance.RunEvent(eventID);
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
        
        private void ScheduleCategoryForwardFromNow(MapDataSO.EventMapOption opt, float elapsedStart,
            float remainingStart)
        {
            if (opt == null || remainingStart <= 0f) return;

            var group = BuildGroupId(opt);
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
    }
}