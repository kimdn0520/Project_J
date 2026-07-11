using UnityEngine;
using UnityEngine.Events;

namespace MapSystem
{
    /// <summary>
    /// A self-contained component that listens to a specific dialogue event
    /// and triggers UnityEvents (e.g., animations, sound playback, active states) in response.
    /// Eliminates the need to write custom code for simple object-level spooky events.
    /// </summary>
    public class SelfEventObject : MonoBehaviour
    {
        [Header("Event Settings")]
        [SerializeField] private string targetEventId;
        
        [Header("Actions to Trigger")]
        [SerializeField] private UnityEvent onEventTriggered;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(targetEventId))
            {
                DialogSystem.DialogueEventDispatcher.Register(targetEventId, OnTriggerEvent);
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(targetEventId))
            {
                DialogSystem.DialogueEventDispatcher.Unregister(targetEventId, OnTriggerEvent);
            }
        }

        private void OnTriggerEvent()
        {
            onEventTriggered?.Invoke();
        }
    }
}
