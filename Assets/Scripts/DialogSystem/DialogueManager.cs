using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DialogSystem
{
    /// <summary>
    /// Core manager for the dialogue system.
    /// Manages the flow of active dialogues, choice transitions, conditions, and event callbacks.
    /// </summary>
    public class DialogueManager : SingletonMonoBehaviour<DialogueManager>
    {
        [Header("UI Reference")]
        [SerializeField] private DialogueUI dialogueUI;

        [Header("Configuration")]
        [SerializeField] private bool loadOnAwake = false;

        [Header("Editor & Build Database Selection")]
        [SerializeField] private bool useBinaryInEditor = false;
        [SerializeField] private DialogueContainerSO editorDialogueGraph;
        [SerializeField] private TextAsset binaryDialogueAsset; // dialogues.bin

        public bool IsDialogueActive { get; private set; } = false;

        // Delegates to decouple the dialogue system from other modules
        public Func<string, bool> OnCheckFlag;     // Condition check for game flags (quest state, etc.)
        public Func<string, bool> OnCheckItem;     // Condition check for inventory items
        public Action<string, bool> OnSetFlag;     // Action to modify game flags when choices are selected
        
        // Actions for other systems to listen to
        public event Action OnDialogueStart;
        public event Action OnDialogueEnd;

        private DialogueDatabase database;
        private CancellationTokenSource dialogueCts;
        private DialogueNode currentNode;

        // Simple default flag dictionary in case user hasn't plugged in their own FlagManager
        private readonly Dictionary<string, bool> localFlags = new Dictionary<string, bool>();

        protected override void Awake()
        {
            base.Awake();
            database = new DialogueDatabase();

            // Set up fallback local handlers if not set externally
            OnCheckFlag ??= DefaultCheckFlag;
            OnSetFlag ??= DefaultSetFlag;
            OnCheckItem ??= DefaultCheckItem;

            if (loadOnAwake)
            {
                #if UNITY_EDITOR
                if (!useBinaryInEditor && editorDialogueGraph != null)
                {
                    LoadDialoguesFromSO(editorDialogueGraph);
                    return;
                }
                #endif

                if (binaryDialogueAsset != null)
                {
                    LoadDialoguesFromBinary(binaryDialogueAsset.bytes);
                }
            }
        }

        private void OnDestroy()
        {
            dialogueCts?.Cancel();
            dialogueCts?.Dispose();
        }

        /// <summary>
        /// Loads or reloads dialogue data from raw JSON text.
        /// </summary>
        public void LoadDialogues(string jsonText)
        {
            database.Clear();
            database.LoadFromJSON(jsonText);
        }

        /// <summary>
        /// Starts a dialogue sequence from a specified node ID.
        /// </summary>
        public void StartDialogue(string nodeId)
        {
            if (IsDialogueActive)
            {
                Debug.LogWarning($"[DialogueManager] Dialogue is already active. Cannot play node: {nodeId}");
                return;
            }

            dialogueCts?.Cancel();
            dialogueCts = new CancellationTokenSource();

            RunDialogueLoopAsync(nodeId, dialogueCts.Token).Forget();
        }

        private async UniTaskVoid RunDialogueLoopAsync(string nodeId, CancellationToken token)
        {
            IsDialogueActive = true;
            OnDialogueStart?.Invoke();
            dialogueUI.ShowDialogue();

            string nextNodeId = nodeId;

            while (!string.IsNullOrEmpty(nextNodeId) && !token.IsCancellationRequested)
            {
                currentNode = database.GetNode(nextNodeId);
                if (currentNode == null)
                {
                    break;
                }

                // Trigger node entry event
                if (!string.IsNullOrEmpty(currentNode.triggerEvent))
                {
                    DialogueEventDispatcher.Dispatch(currentNode.triggerEvent);
                }

                // Display dialogue text & wait until typing ends (or skipped)
                await dialogueUI.DisplayDialogueNodeAsync(currentNode, token);

                // Check for conditional choices
                List<DialogueChoice> availableChoices = GetAvailableChoices(currentNode.choices);

                if (availableChoices != null && availableChoices.Count > 0)
                {
                    // Branching path: display buttons and wait for selection
                    int selectedIndex = -1;
                    dialogueUI.DisplayChoices(availableChoices, (index) => selectedIndex = index);

                    await UniTask.WaitUntil(() => selectedIndex != -1, PlayerLoopTiming.Update, token);

                    DialogueChoice chosen = availableChoices[selectedIndex];

                    // Process selection actions
                    if (!string.IsNullOrEmpty(chosen.setFlag))
                    {
                        OnSetFlag?.Invoke(chosen.setFlag, true);
                    }
                    if (!string.IsNullOrEmpty(chosen.triggerEvent))
                    {
                        DialogueEventDispatcher.Dispatch(chosen.triggerEvent);
                    }

                    // Move to next node
                    nextNodeId = chosen.nextNodeId;
                }
                else
                {
                    // Linear path: wait for confirm click
                    await dialogueUI.WaitForPlayerAdvanceAsync(token);
                    
                    // Move to next node
                    nextNodeId = currentNode.nextNodeId;
                }
            }

            EndDialogue();
        }

        /// <summary>
        /// Ends the current dialogue.
        /// </summary>
        public void EndDialogue()
        {
            if (!IsDialogueActive) return;

            dialogueCts?.Cancel();
            dialogueUI.HideDialogue();
            dialogueUI.HideChoices();
            IsDialogueActive = false;
            OnDialogueEnd?.Invoke();
        }

        private List<DialogueChoice> GetAvailableChoices(List<DialogueChoice> allChoices)
        {
            if (allChoices == null) return null;

            List<DialogueChoice> filtered = new List<DialogueChoice>();
            foreach (var choice in allChoices)
            {
                // Check flags condition
                if (!string.IsNullOrEmpty(choice.requiredFlag))
                {
                    if (!OnCheckFlag.Invoke(choice.requiredFlag)) continue;
                }

                // Check items condition
                if (!string.IsNullOrEmpty(choice.requiredItem))
                {
                    if (!OnCheckItem.Invoke(choice.requiredItem)) continue;
                }

                filtered.Add(choice);
            }
            return filtered;
        }

        // Default local handlers if not hooked up externally
        private bool DefaultCheckFlag(string flagName)
        {
            localFlags.TryGetValue(flagName, out bool value);
            return value;
        }

        private void DefaultSetFlag(string flagName, bool value)
        {
            localFlags[flagName] = value;
            Debug.Log($"[DialogueManager] Local flag set: {flagName} = {value}");
        }

        private bool DefaultCheckItem(string itemId)
        {
            Debug.LogWarning($"[DialogueManager] Inventory item check invoked for '{itemId}', but no external handler was registered. Returning false.");
            return false;
        }

        /// <summary>
        /// Loads dialogue data from an obfuscated binary byte array.
        /// </summary>
        public void LoadDialoguesFromBinary(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0)
            {
                Debug.LogError("[DialogueManager] Binary asset is empty or null.");
                return;
            }
            string decryptedJson = DialogueObfuscation.DecryptToString(encryptedBytes);
            LoadDialogues(decryptedJson);
        }

        /// <summary>
        /// Converts the visual Node Editor SO graph into the runtime database structure on the fly (useful in Editor).
        /// </summary>
        public void LoadDialoguesFromSO(DialogueContainerSO graphAsset)
        {
            if (graphAsset == null) return;
            database.Clear();

            // Create GUID-to-ID mapping
            Dictionary<string, string> guidToIdMap = new Dictionary<string, string>();
            foreach (var node in graphAsset.dialogueNodes)
            {
                if (string.IsNullOrEmpty(node.guid)) continue;
                string gameId = string.IsNullOrEmpty(node.id) ? node.guid : node.id;
                guidToIdMap[node.guid] = gameId;
            }

            // Resolve connections and feed database
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

                // Resolve linear connection
                string linearNextNodeId = "";
                var linearLink = graphAsset.nodeLinks.Find(l => l.baseNodeGuid == node.guid && l.portName == "Next");
                if (linearLink != null && guidToIdMap.TryGetValue(linearLink.targetNodeGuid, out var nextId))
                {
                    linearNextNodeId = nextId;
                }
                else if (!string.IsNullOrEmpty(node.nextNodeGuid) && guidToIdMap.TryGetValue(node.nextNodeGuid, out var fallbackId))
                {
                    linearNextNodeId = fallbackId;
                }
                runtimeNode.nextNodeId = linearNextNodeId;

                // Resolve choices
                for (int i = 0; i < node.choices.Count; i++)
                {
                    var choice = node.choices[i];
                    string targetChoiceNodeId = "";

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

                database.AddNode(runtimeNode);
            }

            Debug.Log($"[DialogueManager] Successfully loaded {graphAsset.dialogueNodes.Count} nodes directly from Graph SO: {graphAsset.name}");
        }
    }
}
