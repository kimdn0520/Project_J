using UnityEngine;
using System.Collections.Generic;

namespace DialogSystem
{
    /// <summary>
    /// Demonstration component showing how to bind DialogueManager delegates (flag check, item check, flag set)
    /// and register listeners to custom dialogue events.
    /// </summary>
    public class DialogueSystemDemo : MonoBehaviour
    {
        [Header("Dialogue JSON File")]
        [SerializeField] private TextAsset dialogueJsonFile;

        // Mock Inventory & Flags for demonstration purposes
        private readonly List<string> mockInventory = new List<string>();
        private readonly Dictionary<string, bool> mockFlags = new Dictionary<string, bool>();

        private void Start()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[Demo] Please ensure DialogueManager is in the scene.");
                return;
            }

            // 1. Load the dialogue database
            if (dialogueJsonFile != null)
            {
                DialogueManager.Instance.LoadDialogues(dialogueJsonFile.text);
            }

            // 2. Bind external delegates (Decoupled linking to your inventory/quest system)
            DialogueManager.Instance.OnCheckFlag = CheckFlag;
            DialogueManager.Instance.OnSetFlag = SetFlag;
            DialogueManager.Instance.OnCheckItem = CheckItem;

            // 3. Register custom in-dialogue event listeners (Decoupled triggers)
            DialogueEventDispatcher.Register("light_flicker", TriggerLightFlicker);
            DialogueEventDispatcher.Register("camera_shake", TriggerCameraShake);
            DialogueEventDispatcher.Register("gain_key_hint", AddKeyToInventory);
            DialogueEventDispatcher.Register("game_over_win", TriggerWinState);

            Debug.Log("[Demo] Dialogue System initialized. Starting 'start_game' sequence...");
            
            // 4. Start the dialogue
            DialogueManager.Instance.StartDialogue("start_game");
        }

        private void OnDestroy()
        {
            // Unregister to avoid memory leaks
            DialogueEventDispatcher.Unregister("light_flicker", TriggerLightFlicker);
            DialogueEventDispatcher.Unregister("camera_shake", TriggerCameraShake);
            DialogueEventDispatcher.Unregister("gain_key_hint", AddKeyToInventory);
            DialogueEventDispatcher.Unregister("game_over_win", TriggerWinState);
        }

        // --- DECOUPLED DELEGATE IMPLEMENTATIONS ---

        private bool CheckFlag(string flagName)
        {
            mockFlags.TryGetValue(flagName, out bool val);
            Debug.Log($"[Inventory/Quest System] Checked Flag '{flagName}': {val}");
            return val;
        }

        private void SetFlag(string flagName, bool value)
        {
            mockFlags[flagName] = value;
            Debug.Log($"[Inventory/Quest System] Set Flag '{flagName}' to: {value}");
        }

        private bool CheckItem(string itemId)
        {
            bool hasItem = mockInventory.Contains(itemId);
            Debug.Log($"[Inventory/Quest System] Checked Item '{itemId}': {hasItem}");
            return hasItem;
        }

        // --- DECOUPLED EVENT LISTENERS ---

        private void TriggerLightFlicker()
        {
            Debug.Log("<color=yellow>[EVENT] Lights are flickering! The room gets dark.</color>");
            // Implement light flickering animations here
        }

        private void TriggerCameraShake()
        {
            Debug.Log("<color=orange>[EVENT] Shaking the camera! Thud!</color>");
            // Implement camera shake here (e.g. CameraShake.Instance.Shake())
        }

        private void AddKeyToInventory()
        {
            Debug.Log("[EVENT] You realized where the key is, and picked up the 'mansion_key'. Added to inventory.");
            mockInventory.Add("mansion_key");
        }

        private void TriggerWinState()
        {
            Debug.Log("<color=green>[EVENT] Game Cleared! Ending Scene triggered.</color>");
            // Implement scene transition here
        }
    }
}
