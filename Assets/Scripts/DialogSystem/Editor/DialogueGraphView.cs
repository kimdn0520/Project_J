using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.Editor
{
    /// <summary>
    /// Graphical view representation of the dialogue node graph editor.
    /// Handles node creation, deletion, connection port creation, and data serialization.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        private readonly Vector2 defaultNodeSize = new Vector2(250, 200);
        public List<DialogueNodeView> Nodes = new List<DialogueNodeView>();

        public DialogueGraphView()
        {
            // Add zoom & manipulators
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Add grid background
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Support connections
            this.AddManipulator(new ClickSelector());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        /// <summary>
        /// Creates a new visual dialogue node in the graph.
        /// </summary>
        public DialogueNodeView CreateDialogueNode(Vector2 position, DialogueNodeData data = null)
        {
            var nodeView = new DialogueNodeView(this, position, data);
            AddElement(nodeView);
            Nodes.Add(nodeView);
            return nodeView;
        }

        /// <summary>
        /// Clears all graph elements (nodes and edges).
        /// </summary>
        public void ClearGraph()
        {
            foreach (var node in Nodes)
            {
                RemoveElement(node);
            }
            Nodes.Clear();

            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }
        }
    }

    /// <summary>
    /// Custom visual node class inside the DialogueGraphView.
    /// </summary>
    public class DialogueNodeView : Node
    {
        public string Guid;
        public DialogueNodeData Data;
        private readonly DialogueGraphView graphView;

        // Visual elements inside the node
        public TextField IdField;
        public TextField SpeakerField;
        public TextField TextField;
        public TextField TriggerEventField;

        private VisualElement choicesContainer;
        public List<Port> OutputPorts = new List<Port>();

        public DialogueNodeView(DialogueGraphView graphView, Vector2 position, DialogueNodeData initialData = null)
        {
            this.graphView = graphView;
            SetPosition(new Rect(position, Vector2.zero));

            // Load or create new node data
            if (initialData != null)
            {
                Data = initialData;
                Guid = initialData.guid;
            }
            else
            {
                Guid = System.Guid.NewGuid().ToString();
                Data = new DialogueNodeData
                {
                    guid = Guid,
                    id = "node_" + Guid.Substring(0, 5),
                    speaker = "Speaker",
                    text = "Insert dialogue text here...",
                    position = position
                };
            }

            title = "Dialogue Node";

            // Add standard input port
            var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "Input";
            inputContainer.Add(inputPort);

            // Setup custom UI Inspector inside the Node
            CreateNodeInspector();

            // Refresh representation
            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreateNodeInspector()
        {
            // Node ID (Unique tag for references)
            IdField = new TextField("Dialogue ID");
            IdField.value = Data.id;
            IdField.RegisterValueChangedCallback(evt => Data.id = evt.newValue);
            extensionContainer.Add(IdField);

            // Speaker Name
            SpeakerField = new TextField("Speaker");
            SpeakerField.value = Data.speaker;
            SpeakerField.RegisterValueChangedCallback(evt => Data.speaker = evt.newValue);
            extensionContainer.Add(SpeakerField);

            // Dialogue Multi-line Text
            TextField = new TextField("Text");
            TextField.multiline = true;
            TextField.value = Data.text;
            TextField.style.minHeight = 40;
            TextField.RegisterValueChangedCallback(evt => Data.text = evt.newValue);
            extensionContainer.Add(TextField);

            // Trigger Event Action (Decoupled gameplay event)
            TriggerEventField = new TextField("Event Trigger");
            TriggerEventField.value = Data.triggerEvent;
            TriggerEventField.RegisterValueChangedCallback(evt => Data.triggerEvent = evt.newValue);
            extensionContainer.Add(TriggerEventField);

            // Add Choice Button
            var addChoiceButton = new Button(() => AddChoicePort()) { text = "Add Choice" };
            addChoiceButton.style.marginTop = 5;
            addChoiceButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f);
            extensionContainer.Add(addChoiceButton);

            // Container to house choice UI details
            choicesContainer = new VisualElement();
            extensionContainer.Add(choicesContainer);

            // Rebuild choices from existing data
            if (Data.choices.Count > 0)
            {
                foreach (var choice in Data.choices)
                {
                    BuildChoiceUI(choice);
                }
            }
            else
            {
                // If linear node, create a default "Next" linear port
                CreateLinearNextPort();
            }
        }

        private void CreateLinearNextPort()
        {
            var nextPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            nextPort.portName = "Next";
            outputContainer.Add(nextPort);
            OutputPorts.Add(nextPort);
            RefreshPorts();
        }

        private void ClearLinearNextPort()
        {
            var nextPort = OutputPorts.Find(p => p.portName == "Next");
            if (nextPort != null)
            {
                // Disconnect any edges
                var edgesList = graphView.edges.ToList();
                foreach (var edge in edgesList)
                {
                    if (edge.output == nextPort)
                    {
                        graphView.RemoveElement(edge);
                    }
                }
                outputContainer.Remove(nextPort);
                OutputPorts.Remove(nextPort);
            }
        }

        private void AddChoicePort(DialogueChoiceData choiceData = null)
        {
            // Clear default Next port if we are switching to choice mode
            if (Data.choices.Count == 0)
            {
                ClearLinearNextPort();
            }

            if (choiceData == null)
            {
                choiceData = new DialogueChoiceData
                {
                    text = "Choice " + (Data.choices.Count + 1)
                };
                Data.choices.Add(choiceData);
            }

            BuildChoiceUI(choiceData);
        }

        private void BuildChoiceUI(DialogueChoiceData choiceData)
        {
            int index = Data.choices.IndexOf(choiceData);

            // Port representation on node output side
            var choicePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            choicePort.portName = $"Choice_{index}";
            outputContainer.Add(choicePort);
            OutputPorts.Add(choicePort);

            // Visual details container for this choice inside the node body
            var choiceItemElement = new VisualElement();
            choiceItemElement.style.borderLeftWidth = 1;
            choiceItemElement.style.borderRightWidth = 1;
            choiceItemElement.style.borderTopWidth = 1;
            choiceItemElement.style.borderBottomWidth = 1;
            choiceItemElement.style.borderLeftColor = Color.gray;
            choiceItemElement.style.borderRightColor = Color.gray;
            choiceItemElement.style.borderTopColor = Color.gray;
            choiceItemElement.style.borderBottomColor = Color.gray;
            choiceItemElement.style.marginTop = 3;
            choiceItemElement.style.paddingLeft = 3;

            // Header containing title & Delete Choice Button
            var choiceHeader = new VisualElement();
            choiceHeader.style.flexDirection = FlexDirection.Row;
            choiceHeader.style.justifyContent = Justify.SpaceBetween;

            var choiceTitleLabel = new Label($"Choice {index + 1}");
            choiceTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            choiceHeader.Add(choiceTitleLabel);

            var deleteBtn = new Button(() => RemoveChoice(choiceData, choicePort, choiceItemElement)) { text = "X" };
            deleteBtn.style.backgroundColor = new Color(0.6f, 0.1f, 0.1f);
            choiceHeader.Add(deleteBtn);
            choiceItemElement.Add(choiceHeader);

            // Choice display text
            var choiceText = new TextField("Text");
            choiceText.value = choiceData.text;
            choiceText.RegisterValueChangedCallback(evt => choiceData.text = evt.newValue);
            choiceItemElement.Add(choiceText);

            // Conditions (Flag & Item)
            var reqFlag = new TextField("Req Flag");
            reqFlag.value = choiceData.requiredFlag;
            reqFlag.RegisterValueChangedCallback(evt => choiceData.requiredFlag = evt.newValue);
            choiceItemElement.Add(reqFlag);

            var reqItem = new TextField("Req Item");
            reqItem.value = choiceData.requiredItem;
            reqItem.RegisterValueChangedCallback(evt => choiceData.requiredItem = evt.newValue);
            choiceItemElement.Add(reqItem);

            // Actions (Set Flag & Event trigger)
            var sFlag = new TextField("Set Flag");
            sFlag.value = choiceData.setFlag;
            sFlag.RegisterValueChangedCallback(evt => choiceData.setFlag = evt.newValue);
            choiceItemElement.Add(sFlag);

            var cEvent = new TextField("Choice Event");
            cEvent.value = choiceData.triggerEvent;
            cEvent.RegisterValueChangedCallback(evt => choiceData.triggerEvent = evt.newValue);
            choiceItemElement.Add(cEvent);

            choicesContainer.Add(choiceItemElement);

            RefreshPorts();
            RefreshExpandedState();
        }

        private void RemoveChoice(DialogueChoiceData choiceData, Port choicePort, VisualElement uiContainer)
        {
            // Remove connections to this port
            var edgesList = graphView.edges.ToList();
            foreach (var edge in edgesList)
            {
                if (edge.output == choicePort)
                {
                    graphView.RemoveElement(edge);
                }
            }

            outputContainer.Remove(choicePort);
            OutputPorts.Remove(choicePort);
            choicesContainer.Remove(uiContainer);
            Data.choices.Remove(choiceData);

            // Rebuild remaining choices to fix indexes & portNames
            List<DialogueChoiceData> currentChoices = new List<DialogueChoiceData>(Data.choices);
            
            // Clear current graphical representations
            foreach (var p in OutputPorts.ToList())
            {
                outputContainer.Remove(p);
            }
            OutputPorts.Clear();
            choicesContainer.Clear();
            Data.choices.Clear();

            // Rebuild
            if (currentChoices.Count > 0)
            {
                foreach (var choice in currentChoices)
                {
                    AddChoicePort(choice);
                }
            }
            else
            {
                // Revert to linear flow
                CreateLinearNextPort();
            }

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
