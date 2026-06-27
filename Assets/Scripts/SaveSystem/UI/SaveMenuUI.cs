using UnityEngine;
using System.Collections.Generic;

namespace SaveSystem.UI
{
    public enum SaveMenuMode
    {
        Save,
        Load
    }

    /// <summary>
    /// Coordinates all save slots in the save/load menu.
    /// Manages opening/closing the panel and switching modes (Save vs Load).
    /// </summary>
    public class SaveMenuUI : MonoBehaviour
    {
        [Header("Menu Configuration")]
        [SerializeField] private SaveMenuMode menuMode = SaveMenuMode.Load;
        [SerializeField] private GameObject menuPanel;

        [Header("Slots")]
        [SerializeField] private List<SaveSlotUI> saveSlots = new List<SaveSlotUI>();

        private void OnEnable()
        {
            InitializeSlots();
        }

        /// <summary>
        /// Opens the menu panel and configures slots in either Save or Load mode.
        /// </summary>
        public void OpenMenu(SaveMenuMode mode)
        {
            menuMode = mode;
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }
            InitializeSlots();
        }

        /// <summary>
        /// Closes the menu panel.
        /// </summary>
        public void CloseMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }

        private void InitializeSlots()
        {
            foreach (var slot in saveSlots)
            {
                if (slot != null)
                {
                    slot.Setup(OnSlotClicked);
                }
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (SaveManager.Instance == null) return;

            if (menuMode == SaveMenuMode.Save)
            {
                // Trigger save
                SaveManager.Instance.SaveGame(slotIndex);
                
                // Refresh all slots to update timestamps/locations
                RefreshAllSlots();
            }
            else if (menuMode == SaveMenuMode.Load)
            {
                if (SaveManager.Instance.HasSaveData(slotIndex))
                {
                    SaveManager.Instance.LoadGame(slotIndex);
                    CloseMenu();
                }
                else
                {
                    Debug.Log($"[SaveMenuUI] Slot {slotIndex} is empty. Cannot load.");
                }
            }
        }

        /// <summary>
        /// Re-fetches save file statuses and refreshes the slot views.
        /// </summary>
        public void RefreshAllSlots()
        {
            foreach (var slot in saveSlots)
            {
                if (slot != null)
                {
                    slot.RefreshUI();
                }
            }
        }
    }
}
