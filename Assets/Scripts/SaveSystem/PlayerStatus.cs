using UnityEngine;
using System.Collections.Generic;

namespace SaveSystem
{
    /// <summary>
    /// Manages the player's core gameplay status (Health/Hearts, Collected Rules, Solved Rules).
    /// Acts as a singleton for easy global access.
    /// </summary>
    public class PlayerStatus : SingletonMonoBehaviour<PlayerStatus>
    {
        [Header("Health Settings (Hearts)")]
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private int currentHealth = 5;

        [Header("Rules Settings")]
        [SerializeField] private int totalRulesCount = 10;
        [SerializeField] private List<string> collectedRuleIds = new List<string>();
        [SerializeField] private List<string> solvedRuleIds = new List<string>();
        
        [Header("Inventory Settings")]
        [SerializeField] private List<string> inventoryItemIds = new List<string>();

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int TotalRulesCount => totalRulesCount;
        public int SolvedRulesCount => solvedRuleIds.Count;

        public List<string> CollectedRuleIds => collectedRuleIds;
        public List<string> SolvedRuleIds => solvedRuleIds;
        public List<string> InventoryItemIds => inventoryItemIds;

        public void SetHealth(int amount)
        {
            currentHealth = Mathf.Clamp(amount, 0, maxHealth);
        }

        public void Heal(int amount)
        {
            SetHealth(currentHealth + amount);
        }

        public void TakeDamage(int amount)
        {
            SetHealth(currentHealth - amount);
        }

        public void CollectRule(string ruleId)
        {
            if (!collectedRuleIds.Contains(ruleId))
            {
                collectedRuleIds.Add(ruleId);
            }
        }

        public void SolveRule(string ruleId)
        {
            if (!solvedRuleIds.Contains(ruleId))
            {
                solvedRuleIds.Add(ruleId);
            }
            // A solved rule is also a collected rule
            CollectRule(ruleId);
        }

        public void AddItem(string itemId)
        {
            if (!inventoryItemIds.Contains(itemId))
            {
                inventoryItemIds.Add(itemId);
            }
        }

        public void RemoveItem(string itemId)
        {
            inventoryItemIds.Remove(itemId);
        }

        public bool HasItem(string itemId) => inventoryItemIds.Contains(itemId);
        public bool IsRuleSolved(string ruleId) => solvedRuleIds.Contains(ruleId);
        public bool IsRuleCollected(string ruleId) => collectedRuleIds.Contains(ruleId);

        /// <summary>
        /// Resets the status to default values.
        /// </summary>
        public void ResetStatus()
        {
            currentHealth = maxHealth;
            collectedRuleIds.Clear();
            solvedRuleIds.Clear();
            inventoryItemIds.Clear();
        }

        /// <summary>
        /// Populates SaveData with current status values.
        /// </summary>
        public void PopulateSaveData(SaveData saveData)
        {
            saveData.currentHealth = currentHealth;
            saveData.maxHealth = maxHealth;
            saveData.totalRulesCount = totalRulesCount;
            saveData.solvedRulesCount = SolvedRulesCount;
            
            saveData.collectedRuleIds = new List<string>(collectedRuleIds);
            saveData.solvedRuleIds = new List<string>(solvedRuleIds);
            saveData.inventoryItemIds = new List<string>(inventoryItemIds);
        }

        /// <summary>
        /// Restores current status values from SaveData.
        /// </summary>
        public void LoadFromSaveData(SaveData saveData)
        {
            currentHealth = saveData.currentHealth;
            maxHealth = saveData.maxHealth;
            totalRulesCount = saveData.totalRulesCount;

            collectedRuleIds = new List<string>(saveData.collectedRuleIds);
            solvedRuleIds = new List<string>(saveData.solvedRuleIds);
            inventoryItemIds = new List<string>(saveData.inventoryItemIds);
        }
    }
}
