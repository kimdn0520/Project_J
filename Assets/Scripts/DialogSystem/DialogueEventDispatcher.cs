using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogSystem
{
    /// <summary>
    /// Decoupled event dispatcher for dialogue events.
    /// Allows external gameplay systems (e.g., quest managers, puzzle mechanisms, jump scare systems)
    /// to listen for event IDs triggered by dialogues or choices.
    /// </summary>
    public static class DialogueEventDispatcher
    {
        private static readonly Dictionary<string, Action> eventMap = new Dictionary<string, Action>();

        /// <summary>
        /// Registers a callback for a specific dialogue event ID.
        /// </summary>
        public static void Register(string eventId, Action callback)
        {
            if (string.IsNullOrEmpty(eventId)) return;

            if (!eventMap.ContainsKey(eventId))
            {
                eventMap[eventId] = callback;
            }
            else
            {
                eventMap[eventId] += callback;
            }
        }

        /// <summary>
        /// Unregisters a callback for a specific dialogue event ID.
        /// </summary>
        public static void Unregister(string eventId, Action callback)
        {
            if (string.IsNullOrEmpty(eventId)) return;

            if (eventMap.ContainsKey(eventId))
            {
                eventMap[eventId] -= callback;
                if (eventMap[eventId] == null)
                {
                    eventMap.Remove(eventId);
                }
            }
        }

        /// <summary>
        /// Dispatches/triggers the event associated with the event ID.
        /// </summary>
        public static void Dispatch(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;

            if (eventMap.TryGetValue(eventId, out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing dialogue event '{eventId}': {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Dialogue event '{eventId}' was dispatched but has no registered listeners.");
            }
        }
        
        /// <summary>
        /// Clears all event registrations.
        /// </summary>
        public static void Clear()
        {
            eventMap.Clear();
        }
    }
}
