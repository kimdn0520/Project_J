using System.Collections.Generic;
using UnityEngine;

namespace DialogSystem
{
    /// <summary>
    /// In-memory database of dialogues loaded from JSON data.
    /// Provides O(1) direct retrieval of dialogue nodes during gameplay.
    /// </summary>
    public class DialogueDatabase
    {
        private readonly Dictionary<string, DialogueNode> dialogueMap = new Dictionary<string, DialogueNode>();

        /// <summary>
        /// Parses the JSON text and stores all dialogue nodes in a dictionary.
        /// </summary>
        public void LoadFromJSON(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("[DialogueDatabase] JSON text is empty or null.");
                return;
            }

            try
            {
                DialogueData data = JsonUtility.FromJson<DialogueData>(jsonText);
                if (data == null || data.dialogues == null)
                {
                    Debug.LogError("[DialogueDatabase] Failed to deserialize JSON. Format might be incorrect.");
                    return;
                }

                foreach (var node in data.dialogues)
                {
                    if (string.IsNullOrEmpty(node.id))
                    {
                        Debug.LogWarning("[DialogueDatabase] Dialogue node contains an empty ID. Skipping.");
                        continue;
                    }

                    if (!dialogueMap.ContainsKey(node.id))
                    {
                        dialogueMap.Add(node.id, node);
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueDatabase] Duplicate Dialogue Node ID found: '{node.id}'. Overwriting.");
                        dialogueMap[node.id] = node;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DialogueDatabase] Parsing exception: {e.Message}");
            }
        }

        /// <summary>
        /// Retrieves a dialogue node by its ID.
        /// </summary>
        public DialogueNode GetNode(string nodeId)
        {
            if (dialogueMap.TryGetValue(nodeId, out var node))
            {
                return node;
            }
            Debug.LogWarning($"[DialogueDatabase] Dialogue node with ID '{nodeId}' not found.");
            return null;
        }

        /// <summary>
        /// Adds or overwrites a dialogue node in the database.
        /// </summary>
        public void AddNode(DialogueNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.id)) return;
            dialogueMap[node.id] = node;
        }

        /// <summary>
        /// Clears all loaded dialogue nodes.
        /// </summary>
        public void Clear()
        {
            dialogueMap.Clear();
        }
    }
}
