using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogSystem.Editor
{
    /// <summary>
    /// EditorWindow that hosts the Dialogue GraphView and toolbar options.
    /// Handles visual saving/loading to/from DialogueContainerSO assets.
    /// </summary>
    public class DialogueGraphEditorWindow : EditorWindow
    {
        private DialogueGraphView graphView;
        private DialogueContainerSO activeGraphSO;
        private Label activeGraphLabel;

        [MenuItem("Tools/Dialogue/Dialogue Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.Show();
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(graphView);
        }

        private void ConstructGraphView()
        {
            graphView = new DialogueGraphView
            {
                name = "Dialogue Graph View"
            };

            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            // Active graph tracker label
            activeGraphLabel = new Label("No Graph Loaded");
            activeGraphLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            activeGraphLabel.style.paddingLeft = 10;
            activeGraphLabel.style.paddingRight = 10;
            activeGraphLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            activeGraphLabel.style.color = new Color(0.8f, 0.8f, 0.2f);
            toolbar.Add(activeGraphLabel);

            // Create Node button
            var createNodeBtn = new Button(() => { graphView.CreateDialogueNode(new Vector2(100, 100)); })
            {
                text = "Create Node"
            };
            toolbar.Add(createNodeBtn);

            // Asset selector field
            var objectField = new ObjectField("Active SO")
            {
                objectType = typeof(DialogueContainerSO),
                value = activeGraphSO
            };
            objectField.RegisterValueChangedCallback(evt =>
            {
                activeGraphSO = (DialogueContainerSO)evt.newValue;
                if (activeGraphSO != null)
                {
                    activeGraphLabel.text = activeGraphSO.name;
                    LoadGraph(activeGraphSO);
                }
                else
                {
                    activeGraphLabel.text = "No Graph Loaded";
                    graphView.ClearGraph();
                }
            });
            toolbar.Add(objectField);

            // Save button
            var saveBtn = new Button(() => SaveGraph())
            {
                text = "Save Graph"
            };
            toolbar.Add(saveBtn);

            // Export shortcut button
            var exportBtn = new Button(() => DialogueBinaryExporter.ExportToBinary())
            {
                text = "Export Binary"
            };
            exportBtn.style.backgroundColor = new Color(0.1f, 0.4f, 0.6f);
            toolbar.Add(exportBtn);

            rootVisualElement.Add(toolbar);
        }

        /// <summary>
        /// Saves the visual graph elements back to the active ScriptableObject database.
        /// </summary>
        private void SaveGraph()
        {
            if (activeGraphSO == null)
            {
                EditorUtility.DisplayDialog("Dialogue Editor", "Please select a DialogueContainerSO file first!", "OK");
                return;
            }

            // Record undo state
            Undo.RecordObject(activeGraphSO, "Save Dialogue Graph");

            activeGraphSO.dialogueNodes.Clear();
            activeGraphSO.nodeLinks.Clear();

            var graphEdges = graphView.edges.ToList();

            // 1. Gather all node data
            foreach (var nodeView in graphView.Nodes)
            {
                // Update position
                nodeView.Data.position = nodeView.GetPosition().position;
                
                // Clear obsolete target guids from choice models
                nodeView.Data.nextNodeGuid = "";
                foreach (var choice in nodeView.Data.choices)
                {
                    choice.nextNodeGuid = "";
                }

                activeGraphSO.dialogueNodes.Add(nodeView.Data);
            }

            // 2. Gather all connections
            foreach (var edge in graphEdges)
            {
                var outputNode = edge.output.node as DialogueNodeView;
                var inputNode = edge.input.node as DialogueNodeView;

                if (outputNode != null && inputNode != null)
                {
                    var link = new NodeLinkData
                    {
                        baseNodeGuid = outputNode.Guid,
                        targetNodeGuid = inputNode.Guid,
                        portName = edge.output.portName
                    };

                    activeGraphSO.nodeLinks.Add(link);

                    // Re-bind inline references in data model
                    if (link.portName == "Next")
                    {
                        outputNode.Data.nextNodeGuid = inputNode.Guid;
                    }
                    else if (link.portName.StartsWith("Choice_"))
                    {
                        if (int.TryParse(link.portName.Split('_')[1], out int index))
                        {
                            if (index >= 0 && index < outputNode.Data.choices.Count)
                            {
                                outputNode.Data.choices[index].nextNodeGuid = inputNode.Guid;
                            }
                        }
                    }
                }
            }

            // Save modifications
            EditorUtility.SetDirty(activeGraphSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Dialogue Editor", "Graph Saved Successfully!", "OK");
        }

        /// <summary>
        /// Loads and reconstructs a DialogueContainerSO visually into the editor window.
        /// </summary>
        private void LoadGraph(DialogueContainerSO targetSO)
        {
            if (targetSO == null) return;

            graphView.ClearGraph();

            // 1. Recreate nodes
            Dictionary<string, DialogueNodeView> nodeMap = new Dictionary<string, DialogueNodeView>();

            foreach (var nodeData in targetSO.dialogueNodes)
            {
                // Create copy to prevent direct reference leaks during editing
                DialogueNodeData nodeDataCopy = new DialogueNodeData
                {
                    guid = nodeData.guid,
                    id = nodeData.id,
                    speaker = nodeData.speaker,
                    text = nodeData.text,
                    triggerEvent = nodeData.triggerEvent,
                    position = nodeData.position,
                    choices = new List<DialogueChoiceData>(nodeData.choices.Select(c => new DialogueChoiceData
                    {
                        text = c.text,
                        nextNodeGuid = c.nextNodeGuid,
                        requiredFlag = c.requiredFlag,
                        requiredItem = c.requiredItem,
                        setFlag = c.setFlag,
                        triggerEvent = c.triggerEvent
                    })),
                    nextNodeGuid = nodeData.nextNodeGuid
                };

                var nodeView = graphView.CreateDialogueNode(nodeDataCopy.position, nodeDataCopy);
                nodeMap.Add(nodeView.Guid, nodeView);
            }

            // 2. Reconnect ports
            foreach (var link in targetSO.nodeLinks)
            {
                if (nodeMap.TryGetValue(link.baseNodeGuid, out var outputNode) &&
                    nodeMap.TryGetValue(link.targetNodeGuid, out var inputNode))
                {
                    // Find correct output port
                    var outputPort = outputNode.OutputPorts.Find(p => p.portName == link.portName);
                    // Input port is always "Input"
                    var inputPort = inputNode.inputContainer.Q<Port>();

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = outputPort.ConnectTo(inputPort);
                        graphView.AddElement(edge);
                    }
                }
            }
        }
    }
}
