using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameControl.SO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameControl.EventMap;
using Random = UnityEngine.Random;

namespace GameControl.Controller
{
    public class MapEventController
    {
        private readonly MapDataSO _mapdata;
        private readonly SpawnerStateController _state;
        private bool _debug;
        
        public MapEventController(MapDataSO mapData, SpawnerStateController state, bool debug)
        {
            _mapdata = mapData;
            _state = state;
            _debug = debug;
        }

        public float CalculateMapEventInterval(MapDataSO.EventMapOption eventOption, float elapsedTime)
        {
            if (!eventOption.intervalCanModify)
                return eventOption.playInterval;

            int stepCount = Mathf.FloorToInt(elapsedTime / eventOption.intervalModify);
            float interval = eventOption.playInterval + eventOption.rateModify * stepCount;
            float minInterval = eventOption.minPlayInterval > 0 ? eventOption.minPlayInterval : 0.1f;

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
                if (_debug) Debug.LogWarning($"[PlaySpecificCategoryName] No enabled event category found with name '{categoryName}'.");
                return;
            }

            if (_debug) Debug.Log($"[PlaySpecificCategoryName] Playing event category '{categoryName}'.");
            PlayMapEventCatagory(eventOption);
        }

        
        public void ScheduleAllTriggersUpfront(float maxTime)
        {
            foreach(var eventOption in _mapdata.eventmapOptions)
            {
                if (!eventOption.enableThisMapEvent) continue;

                float elapsedTime = 0f;
                float nextTriggerTime = 0f;
                float startTimer = GameTimer.Instance.StartTimerNumber;

                if (_debug) Debug.Log($"[ScheduleAllTriggersUpfront] Start scheduling for event category '{eventOption.catagolyMapEvent}' with maxTime {maxTime}");

                while (nextTriggerTime < maxTime)
                {
                    float interval = CalculateMapEventInterval(eventOption, elapsedTime);
                    nextTriggerTime += interval;
            
                    float triggerTime = startTimer - nextTriggerTime;

                    if(triggerTime < 0) break;

                    if (_debug) Debug.Log($"[ScheduleAllTriggersUpfront] Scheduling '{eventOption.catagolyMapEvent}' trigger at timer = {triggerTime:F2}s (interval = {interval:F2}s, elapsed = {elapsedTime:F2}s)");

                    GameTimer.Instance.ScheduleTrigger(triggerTime, () =>
                    {
                        if (_debug) Debug.Log($"[PlayMapEventCatagory] Triggered event category '{eventOption.catagolyMapEvent}' at timer = {GameTimer.Instance.GlobalTimer:F2}s");
                        PlayMapEventCatagory(eventOption);
                    });

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
            float chanceRoll = Random.Range(0f, 100f);
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

            string selectedEventID = SelectEventByChance(weightedEvents);
            if (!string.IsNullOrEmpty(selectedEventID))
            {
                TriggerMapEvent(selectedEventID);
            }
        }

        private void TriggerNonWeightedEvents(List<MapDataSO.EventMapOption.MapEventKv> nonWeightedEvents)
        {
            foreach (var ev in nonWeightedEvents)
            {
                if (ev.chance >= 100f)
                {
                    TriggerMapEvent(ev.mapEventID);
                }
            }
        }

        private string SelectEventByChance(List<MapDataSO.EventMapOption.MapEventKv> events)
        {
            float totalChance = 0f;
            foreach (var ev in events)
                totalChance += ev.chance;

            float roll = Random.Range(0f, totalChance);
            float accum = 0f;
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
    }
}
