using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SaveSystem.UI
{
    /// <summary>
    /// Handles visual presentation and interaction for a single save file slot.
    /// Works with standard Unity UI components and TMPro.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex = 1;

        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private TextMeshProUGUI mapNameText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private TextMeshProUGUI saveTimeText;
        [SerializeField] private TextMeshProUGUI rulesText;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("UI State Panels")]
        [SerializeField] private GameObject occupiedPanel;
        [SerializeField] private GameObject emptyPanel;

        [Header("Buttons")]
        [SerializeField] private Button actionButton;

        private System.Action<int> onClickCallback;

        public int SlotIndex => slotIndex;

        /// <summary>
        /// Registers a callback when this slot is clicked and prepares text elements.
        /// </summary>
        public void Setup(System.Action<int> clickCallback)
        {
            onClickCallback = clickCallback;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => onClickCallback?.Invoke(slotIndex));

            if (slotNumberText != null)
            {
                slotNumberText.text = $"SLOT {slotIndex}";
            }

            RefreshUI();
        }

        /// <summary>
        /// Reads save file preview from SaveManager and updates visual components.
        /// </summary>
        public void RefreshUI()
        {
            if (SaveManager.Instance == null) return;

            if (SaveManager.Instance.HasSaveData(slotIndex))
            {
                SaveData data = SaveManager.Instance.GetSaveDataPreview(slotIndex);
                if (data != null)
                {
                    ShowOccupied(data);
                }
                else
                {
                    ShowEmpty();
                }
            }
            else
            {
                ShowEmpty();
            }
        }

        private void ShowOccupied(SaveData data)
        {
            if (occupiedPanel != null) occupiedPanel.SetActive(true);
            if (emptyPanel != null) emptyPanel.SetActive(false);

            if (mapNameText != null)
            {
                mapNameText.text = string.IsNullOrEmpty(data.mapDisplayName) ? data.sceneName : data.mapDisplayName;
            }

            if (playTimeText != null)
            {
                System.TimeSpan t = System.TimeSpan.FromSeconds(data.playTime);
                playTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            }

            if (saveTimeText != null)
            {
                saveTimeText.text = data.saveDateTime;
            }

            if (rulesText != null)
            {
                rulesText.text = $"규칙 해결: {data.solvedRulesCount} / {data.totalRulesCount}";
            }

            if (healthText != null)
            {
                healthText.text = $"체력: {data.currentHealth} / {data.maxHealth}";
            }
        }

        private void ShowEmpty()
        {
            if (occupiedPanel != null) occupiedPanel.SetActive(false);
            if (emptyPanel != null) emptyPanel.SetActive(true);

            if (mapNameText != null) mapNameText.text = "---";
            if (playTimeText != null) playTimeText.text = "--:--:--";
            if (saveTimeText != null) saveTimeText.text = "저장된 데이터 없음";
            if (rulesText != null) rulesText.text = "";
            if (healthText != null) healthText.text = "";
        }
    }
}
