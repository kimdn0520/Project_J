using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DialogSystem.Editor
{
    /// <summary>
    /// Editor utility that packages all visual Dialogue Graph Assets (ScriptableObjects)
    /// into a single, encrypted binary file for builds.
    /// </summary>
    public static class DialogueBinaryExporter
    {
        [MenuItem("Tools/Dialogue/Export Graphs to Binary")]
        public static void ExportToBinary()
        {
            // 1. Find all DialogueContainerSO assets in the project
            string[] guids = AssetDatabase.FindAssets("t:DialogueContainerSO");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Dialogue Exporter", "No DialogueGraph (DialogueContainerSO) assets found in the project.", "OK");
                return;
            }

            List<DialogueNode> runtimeNodes = new List<DialogueNode>();

            foreach (string assetGuid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                DialogueContainerSO graphAsset = AssetDatabase.LoadAssetAtPath<DialogueContainerSO>(path);

                if (graphAsset == null) continue;

                // Create GUID-to-ID mapping dictionary
                Dictionary<string, string> guidToIdMap = new Dictionary<string, string>();
                
                // Register all nodes in the map
                foreach (var node in graphAsset.dialogueNodes)
                {
                    if (string.IsNullOrEmpty(node.guid)) continue;
                    
                    // Fallback to GUID if Game ID is empty
                    string gameId = string.IsNullOrEmpty(node.id) ? node.guid : node.id;
                    guidToIdMap[node.guid] = gameId;
                }

                // Resolve links and convert to runtime DialogueNode format
                foreach (var node in graphAsset.dialogueNodes)
                {
                    DialogueNode runtimeNode = new DialogueNode
                    {
                        id = string.IsNullOrEmpty(node.id) ? node.guid : node.id,
                        speaker = node.speaker,
                        text = node.text,
                        triggerEvent = node.triggerEvent,
                        choices = new List<DialogueChoice>()
                    };

                    // Resolve linear next node via links
                    string linearNextNodeId = "";
                    
                    // Find explicit line/out port connection
                    var linearLink = graphAsset.nodeLinks.Find(l => l.baseNodeGuid == node.guid && l.portName == "Next");
                    if (linearLink != null && guidToIdMap.TryGetValue(linearLink.targetNodeGuid, out var nextId))
                    {
                        linearNextNodeId = nextId;
                    }
                    // Fallback to direct field if link not found but direct reference set
                    else if (!string.IsNullOrEmpty(node.nextNodeGuid) && guidToIdMap.TryGetValue(node.nextNodeGuid, out var fallbackId))
                    {
                        linearNextNodeId = fallbackId;
                    }

                    runtimeNode.nextNodeId = linearNextNodeId;

                    // Resolve choice links
                    for (int i = 0; i < node.choices.Count; i++)
                    {
                        var choice = node.choices[i];
                        string targetChoiceNodeId = "";

                        // Find matching link for this choice port
                        var choiceLink = graphAsset.nodeLinks.Find(l => l.baseNodeGuid == node.guid && l.portName == $"Choice_{i}");
                        if (choiceLink != null && guidToIdMap.TryGetValue(choiceLink.targetNodeGuid, out var choiceNextId))
                        {
                            targetChoiceNodeId = choiceNextId;
                        }
                        else if (!string.IsNullOrEmpty(choice.nextNodeGuid) && guidToIdMap.TryGetValue(choice.nextNodeGuid, out var fallbackChoiceNextId))
                        {
                            targetChoiceNodeId = fallbackChoiceNextId;
                        }

                        runtimeNode.choices.Add(new DialogueChoice
                        {
                            text = choice.text,
                            nextNodeId = targetChoiceNodeId,
                            requiredFlag = choice.requiredFlag,
                            requiredItem = choice.requiredItem,
                            setFlag = choice.setFlag,
                            triggerEvent = choice.triggerEvent
                        });
                    }

                    runtimeNodes.Add(runtimeNode);
                }
            }

            // Create unified DialogueData container
            DialogueData fullData = new DialogueData { dialogues = runtimeNodes };

            // Serialize to JSON text
            string jsonString = JsonUtility.ToJson(fullData, true);

            // Obfuscate to binary bytes
            byte[] binaryData = DialogueObfuscation.EncryptStringToBytes(jsonString);

            // Create Resources/Dialogues directory if it does not exist
            string directoryPath = Path.Combine(Application.dataPath, "Resources/Dialogues");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save binary file
            string outputPath = Path.Combine(directoryPath, "dialogues.bin");
            File.WriteAllBytes(outputPath, binaryData);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Dialogue Exporter", 
                $"Export successful!\n\nConverted {runtimeNodes.Count} nodes from {guids.Length} graphs.\nSaved to: Assets/Resources/Dialogues/dialogues.bin", "OK");
        }
    }
}
