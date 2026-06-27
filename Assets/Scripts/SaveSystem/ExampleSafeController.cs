using UnityEngine;
using DialogSystem;

namespace SaveSystem
{
    /// <summary>
    /// An example of an interactive Safe that uses SaveSystem.UniqueId 
    /// and implements DialogSystem.IInteractable to manage its state dynamically.
    /// </summary>
    [RequireComponent(typeof(UniqueId))]
    public class ExampleSafeController : MonoBehaviour, IInteractable
    {
        [Header("Visual References")]
        [SerializeField] private GameObject closedVisual;
        [SerializeField] private GameObject openedVisual;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private string soundKey = "safe_open";

        private UniqueId uniqueId;
        private bool isOpened = false;
        private string saveKey;

        private void Awake()
        {
            uniqueId = GetComponent<UniqueId>();
            // Key format: "safe_opened_[GUID]"
            saveKey = $"safe_opened_{uniqueId.Id}";
        }

        private void Start()
        {
            // 1. Check if this specific safe was already opened in the active game session
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
            {
                isOpened = SaveManager.Instance.CurrentSaveData.HasFlag(saveKey);
            }

            UpdateVisual();
        }

        /// <summary>
        /// Implements IInteractable.Interact. Called when the player interacts with the safe.
        /// </summary>
        public void Interact()
        {
            if (isOpened)
            {
                Debug.Log("[ExampleSafeController] Already opened.");
                return;
            }

            OpenSafe();
        }

        private void OpenSafe()
        {
            isOpened = true;
            UpdateVisual();

            // Play sound using SoundManager (Try clip first, fallback to string key)
            if (openSound != null)
            {
                SoundManager.Instance.PlaySFX(openSound);
            }
            else if (!string.IsNullOrEmpty(soundKey))
            {
                SoundManager.Instance.PlaySFX(soundKey);
            }

            // 2. Immediately update the in-memory save data
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
            {
                SaveManager.Instance.CurrentSaveData.SetFlag(saveKey, true);
                Debug.Log($"[ExampleSafeController] Safe opened. In-memory flag set for key: {saveKey}");
            }
            
            // Give player rewards, trigger next narrative, etc.
        }

        private void UpdateVisual()
        {
            if (closedVisual != null) closedVisual.SetActive(!isOpened);
            if (openedVisual != null) openedVisual.SetActive(isOpened);
        }
    }
}
