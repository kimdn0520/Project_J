using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapSystem
{
    /// <summary>
    /// Base class for scene-specific controllers.
    /// Handles automatic registration and unregistration of dialogue events to prevent memory leaks.
    /// </summary>
    public abstract class MapControllerBase : MonoBehaviour
    {
        private readonly List<(string eventId, Action callback)> registeredEvents = new List<(string, Action)>();

        protected virtual void OnEnable()
        {
            RegisterSceneEvents();
        }

        protected virtual void OnDisable()
        {
            UnregisterAllSceneEvents();
        }

        /// <summary>
        /// Implement this method to register scene-specific dialogue events using RegisterEvent.
        /// </summary>
        protected abstract void RegisterSceneEvents();

        /// <summary>
        /// Registers a dialogue event and tracks it for automatic cleanup when the scene unloads.
        /// </summary>
        protected void RegisterEvent(string eventId, Action callback)
        {
            if (string.IsNullOrEmpty(eventId) || callback == null) return;

            DialogSystem.DialogueEventDispatcher.Register(eventId, callback);
            registeredEvents.Add((eventId, callback));
        }

        private void UnregisterAllSceneEvents()
        {
            foreach (var evt in registeredEvents)
            {
                DialogSystem.DialogueEventDispatcher.Unregister(evt.eventId, evt.callback);
            }
            registeredEvents.Clear();
        }
    }
}
