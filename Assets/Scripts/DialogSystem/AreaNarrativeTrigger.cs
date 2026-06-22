using UnityEngine;

namespace DialogSystem
{
    /// <summary>
    /// Triggers narration or event dialogues automatically when a Player enters a collider area.
    /// Supports both 2D and 3D triggers, as well as trigger-once and conditional gating.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Or Collider2D, handles both
    public class AreaNarrativeTrigger : MonoBehaviour
    {
        [Header("Dialogue Configuration")]
        [SerializeField] private string dialogueNodeId;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;
        
        [Header("Conditions (Optional)")]
        [SerializeField] private string requiredFlag;
        [SerializeField] private string requiredItem;

        private bool hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                TryTriggerDialogue();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                TryTriggerDialogue();
            }
        }

        private void TryTriggerDialogue()
        {
            if (triggerOnce && hasTriggered) return;
            if (DialogueManager.Instance.IsDialogueActive) return;

            // Check flag condition if specified
            if (!string.IsNullOrEmpty(requiredFlag))
            {
                if (!DialogueManager.Instance.OnCheckFlag(requiredFlag)) return;
            }

            // Check item condition if specified
            if (!string.IsNullOrEmpty(requiredItem))
            {
                if (!DialogueManager.Instance.OnCheckItem(requiredItem)) return;
            }

            hasTriggered = true;
            DialogueManager.Instance.StartDialogue(dialogueNodeId);
        }

        /// <summary>
        /// Resets the trigger so it can be activated again.
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
        }
    }
}
