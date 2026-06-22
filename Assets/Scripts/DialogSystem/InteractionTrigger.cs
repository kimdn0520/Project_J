using System.Collections.Generic;
using UnityEngine;

namespace DialogSystem
{
    /// <summary>
    /// Component that can be attached to any interactable game object (NPC, Drawer, Locked Door, etc.).
    /// Implements IInteractable to trigger dialogues.
    /// Supports condition-based dialogue overrides directly set in the inspector.
    /// </summary>
    public class InteractionTrigger : MonoBehaviour, IInteractable
    {
        [System.Serializable]
        public struct DialogueOverride
        {
            [Tooltip("Dialogue Node ID to trigger if condition is met")]
            public string nodeId;
            
            [Tooltip("Required game flag (checked via DialogueManager.OnCheckFlag)")]
            public string requiredFlag;

            [Tooltip("Required item ID (checked via DialogueManager.OnCheckItem)")]
            public string requiredItem;
        }

        [Header("Default Dialogue")]
        [SerializeField] private string defaultDialogueNodeId;

        [Header("Overrides (First matching condition triggers)")]
        [SerializeField] private List<DialogueOverride> overrides;

        [Header("Options")]
        [SerializeField] private bool disableAfterInteract = false;
        private bool wasInteracted = false;

        public void Interact()
        {
            if (disableAfterInteract && wasInteracted) return;

            string targetNodeId = defaultDialogueNodeId;

            // Check overrides in order
            if (overrides != null)
            {
                foreach (var dialogueOverride in overrides)
                {
                    bool flagConditionMet = string.IsNullOrEmpty(dialogueOverride.requiredFlag) || 
                                           DialogueManager.Instance.OnCheckFlag(dialogueOverride.requiredFlag);
                                           
                    bool itemConditionMet = string.IsNullOrEmpty(dialogueOverride.requiredItem) || 
                                           DialogueManager.Instance.OnCheckItem(dialogueOverride.requiredItem);

                    if (flagConditionMet && itemConditionMet)
                    {
                        targetNodeId = dialogueOverride.nodeId;
                        break; // Found matching override, stop checking
                    }
                }
            }

            if (!string.IsNullOrEmpty(targetNodeId))
            {
                wasInteracted = true;
                DialogueManager.Instance.StartDialogue(targetNodeId);
            }
            else
            {
                Debug.LogWarning($"[InteractionTrigger] No valid dialogue Node ID to start on {gameObject.name}.");
            }
        }

        /// <summary>
        /// Resets the interaction state (useful if you want to reactivate it).
        /// </summary>
        public void ResetTrigger()
        {
            wasInteracted = false;
        }
    }
}
