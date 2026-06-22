using System;
using System.Collections.Generic;

namespace DialogSystem
{
    [Serializable]
    public class DialogueChoice
    {
        public string text;               // The text shown to the player for this choice
        public string nextNodeId;         // The node ID to transition to when selected
        
        // Conditions (Optional)
        public string requiredFlag;       // Game flag that must be TRUE to show this choice
        public string requiredItem;       // Item ID that the player must have in inventory to show this choice
        
        // Actions (Optional)
        public string setFlag;            // Game flag to set to TRUE when this choice is selected
        public string triggerEvent;       // Custom event ID to trigger when this choice is selected
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;                 // Unique identifier for the dialogue node
        public string speaker;            // Name of the speaker. Leave blank or null for narration.
        public string text;               // The dialogue text content
        public string nextNodeId;         // The next node ID to go to if there are no choices (default path)
        
        // Action (Optional)
        public string triggerEvent;       // Custom event ID to trigger when this dialogue node is displayed
        
        // Branches
        public List<DialogueChoice> choices; // Choices list. If empty, the dialogue is linear and proceeds on click.
    }

    [Serializable]
    public class DialogueData
    {
        public List<DialogueNode> dialogues; // Root list for JSON utility loading
    }
}
