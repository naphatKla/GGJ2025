using System;
using System.Collections.Generic;

namespace EventManager
{
    public static class EventManager<TEventArgs>
    {
        private static Dictionary<string, Action<TEventArgs>> _eventDictionary =
            new Dictionary<string, Action<TEventArgs>>();

        public static void RegisterEvent(string eventType, Action<TEventArgs> eventHandler)
        {
            if (!_eventDictionary.ContainsKey(eventType))
            {
                _eventDictionary[eventType] = eventHandler;
                return;
            }

            _eventDictionary[eventType] += eventHandler;
        }
        
        public static void UnRegisterEvent(string eventType, Action<TEventArgs> eventHandler)
        {
            if (!_eventDictionary.ContainsKey(eventType)) return;
            _eventDictionary[eventType] -= eventHandler;
        }

        public static void TriggerEvent(string eventType, TEventArgs eventArgs)
        {
            if (!_eventDictionary.ContainsKey(eventType)) return;
            _eventDictionary[eventType]?.Invoke(eventArgs);
        }
    }
}
