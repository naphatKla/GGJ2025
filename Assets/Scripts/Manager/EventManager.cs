using System;
using System.Collections.Generic;

namespace Manager
{
    /// <summary>
    /// A generic static event dispatcher that decouples senders and receivers.
    /// Supports type-safe event arguments and enum-based keys for flexibility.
    /// </summary>
    /// <typeparam name="TEventArgs">The argument type passed to the subscribed event handlers.</typeparam>
    public static class EventManager<TEventArgs>
    {
        #region Inspector & Variables
        
        /// <summary>
        /// Internal dictionary storing event handlers associated with specific event keys.
        /// </summary>
        private static readonly Dictionary<EventKey, Action<TEventArgs>> _eventDictionary =
            new Dictionary<EventKey, Action<TEventArgs>>();
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Subscribes a handler to the specified event key.
        /// </summary>
        /// <param name="eventKey">The key representing the type of event.</param>
        /// <param name="eventHandler">The method to be invoked when the event is triggered.</param>
        public static void RegisterEvent(EventKey eventKey, Action<TEventArgs> eventHandler)
        {
            if (!_eventDictionary.ContainsKey(eventKey))
            {
                _eventDictionary[eventKey] = eventHandler;
                return;
            }

            _eventDictionary[eventKey] += eventHandler;
        }

        /// <summary>
        /// Unsubscribes a handler from the specified event key.
        /// </summary>
        /// <param name="eventKey">The key representing the type of event.</param>
        /// <param name="eventHandler">The method that should no longer respond to this event.</param>
        public static void UnRegisterEvent(EventKey eventKey, Action<TEventArgs> eventHandler)
        {
            if (!_eventDictionary.ContainsKey(eventKey)) return;
            _eventDictionary[eventKey] -= eventHandler;
        }

        /// <summary>
        /// Triggers the event associated with the given key, invoking all registered handlers.
        /// </summary>
        /// <param name="eventKey">The event key to trigger.</param>
        /// <param name="eventArgs">The arguments passed to all event handlers.</param>
        public static void TriggerEvent(EventKey eventKey, TEventArgs eventArgs)
        {
            if (!_eventDictionary.ContainsKey(eventKey)) return;
            _eventDictionary[eventKey]?.Invoke(eventArgs);
        }
        
        #endregion
    }

    /// <summary>
    /// Enum that defines various global event keys used throughout the system.
    /// Can be extended to support different event categories such as gameplay, UI, audio, etc.
    /// </summary>
    public enum EventKey
    {
        /// <summary>
        /// Indicates a feedback (e.g. visual/audio) should start playing.
        /// </summary>
        PlayFeedback,

        /// <summary>
        /// Indicates a previously triggered feedback should stop.
        /// </summary>
        StopFeedback,
        
        /// <summary>
        /// Despawn Enemy
        /// </summary>
        DespawnEnemy
    }
}
