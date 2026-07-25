using System.Collections.Generic;
using UnityEngine;
using DialogSystem;

namespace Player
{
    /// <summary>
    /// Attached to the child "Trigger" GameObject under Player root.
    /// Manages OnTriggerEnter2D / OnTriggerExit2D tracking for interactable objects.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerTriggerZone : MonoBehaviour
    {
        private readonly List<IInteractable> currentInteractables = new List<IInteractable>();

        private void OnTriggerEnter2D(Collider2D collision)
        {
            IInteractable interactable = collision.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = collision.GetComponentInParent<IInteractable>();
            }

            if (interactable != null && !currentInteractables.Contains(interactable))
            {
                currentInteractables.Add(interactable);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            IInteractable interactable = collision.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = collision.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                currentInteractables.Remove(interactable);
            }
        }

        /// <summary>
        /// Returns the primary interactable target currently inside the trigger box.
        /// </summary>
        public IInteractable GetTargetInteractable()
        {
            // Remove any null or destroyed references
            currentInteractables.RemoveAll(item => item == null || (item is MonoBehaviour mb && mb == null));

            if (currentInteractables.Count > 0)
            {
                return currentInteractables[0];
            }

            return null;
        }

        public bool HasInteractable => GetTargetInteractable() != null;
    }
}
