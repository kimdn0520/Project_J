using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// Data structure representing all saveable states of the game.
    /// This is serialized into JSON and encrypted for local file storage.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [Header("1. UI / Header Preview Info")]
        public int slotIndex;
        public string saveDateTime;      // Real-world timestamp ("yyyy-MM-dd HH:mm:ss")
        public float playTime;            // Total accumulated playtime in seconds
        public string mapDisplayName;     // User-friendly map name (e.g. "Main Lobby 1F")
        public int currentHealth;         // Current player health (hearts)
        public int maxHealth;             // Maximum player health (hearts)
        public int solvedRulesCount;      // Number of rules solved/cleared
        public int totalRulesCount;       // Total number of rules in the game

        [Header("2. Location & Coordinate Data")]
        public string sceneName;          // Active Unity scene name
        public string spawnPointId;       // Target SpawnPoint ID
        public float[] playerPosition;    // Serialized Vector3 [x, y, z]
        public float[] playerRotation;    // Serialized Quaternion [x, y, z, w]

        [Header("3. Items & Rules Inventory")]
        public List<string> collectedRuleIds = new List<string>();
        public List<string> solvedRuleIds = new List<string>();
        public List<string> inventoryItemIds = new List<string>();

        [Header("4. Extensible World & Event Flags")]
        public List<string> gameFlags = new List<string>(); // Set of triggered event/puzzle IDs
        public List<StringKeyValuePair> customStates = new List<StringKeyValuePair>(); // Custom key-values

        public void SetCustomState(string key, string value)
        {
            var pair = customStates.Find(p => p.key == key);
            if (pair != null)
            {
                pair.value = value;
            }
            else
            {
                customStates.Add(new StringKeyValuePair { key = key, value = value });
            }
        }

        public string GetCustomState(string key, string defaultValue = "")
        {
            var pair = customStates.Find(p => p.key == key);
            return pair != null ? pair.value : defaultValue;
        }

        public bool HasFlag(string flag)
        {
            return gameFlags.Contains(flag);
        }

        public void SetFlag(string flag, bool value)
        {
            if (value)
            {
                if (!gameFlags.Contains(flag)) gameFlags.Add(flag);
            }
            else
            {
                gameFlags.Remove(flag);
            }
        }
    }

    [Serializable]
    public class StringKeyValuePair
    {
        public string key;
        public string value;
    }
}
