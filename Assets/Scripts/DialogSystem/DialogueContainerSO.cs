using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogSystem
{
    [Serializable]
    public class DialogueNodeData
    {
        public string guid;
        [Tooltip("The unique ID used in the game to trigger this node (e.g., 'start_game')")]
        public string id;
        public string speaker;
        [TextArea(3, 5)]
        public string text;
        public string triggerEvent;
        public Vector2 position;
        
        // List of choices (Output branches)
        public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
        
        // Fallback next node GUID if there are no choices (linear flow)
        public string nextNodeGuid;
    }

    [Serializable]
    public class DialogueChoiceData
    {
        public string text;
        public string nextNodeGuid;
        public string requiredFlag;
        public string requiredItem;
        public string setFlag;
        public string triggerEvent;
    }

    [Serializable]
    public class NodeLinkData
    {
        public string baseNodeGuid;
        public string targetNodeGuid;
        public string portName; // Used to identify which port/choice this link belongs to
    }

    /// <summary>
    /// ScriptableObject asset that stores the dialogue graph editor data.
    /// This file is edited visually inside the Node Editor and packaged into binary during builds.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialog/Dialogue Graph")]
    public class DialogueContainerSO : ScriptableObject
    {
        public List<NodeLinkData> nodeLinks = new List<NodeLinkData>();
        public List<DialogueNodeData> dialogueNodes = new List<DialogueNodeData>();
    }
}
