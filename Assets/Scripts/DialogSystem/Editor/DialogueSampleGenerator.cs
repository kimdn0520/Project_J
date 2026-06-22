using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DialogSystem.Editor
{
    /// <summary>
    /// Editor script to automatically generate a pre-configured sample dialogue graph asset.
    /// Helps developers test the Dialogue Graph Node Editor immediately.
    /// </summary>
    public static class DialogueSampleGenerator
    {
        [MenuItem("Tools/Dialogue/Generate Sample Graph Asset")]
        public static void GenerateSampleAsset()
        {
            // 1. Create Target Directory if it doesn't exist
            string directoryPath = "Assets/Resources/Dialogues";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string assetPath = Path.Combine(directoryPath, "SampleGraph.asset");

            // 2. Create Dialogue Container ScriptableObject
            DialogueContainerSO container = ScriptableObject.CreateInstance<DialogueContainerSO>();

            // 3. Instantiate dialogue nodes with GUIDs and parameters
            string startGuid = System.Guid.NewGuid().ToString();
            string introGuid = System.Guid.NewGuid().ToString();
            string choiceGuid = System.Guid.NewGuid().ToString();
            string doorGuid = System.Guid.NewGuid().ToString();
            string paperGuid = System.Guid.NewGuid().ToString();
            string pushGuid = System.Guid.NewGuid().ToString();
            string unlockGuid = System.Guid.NewGuid().ToString();
            string escapeGuid = System.Guid.NewGuid().ToString();

            // Node 1: Start
            var startNode = new DialogueNodeData
            {
                guid = startGuid,
                id = "start_game",
                speaker = "",
                text = "정신을 차려보니 낯선 저택의 복도에 서 있다. 한기가 살을 찌르는 것 같다.",
                triggerEvent = "light_flicker",
                position = new Vector2(100, 250),
                nextNodeGuid = introGuid
            };

            // Node 2: Intro 독백
            var introNode = new DialogueNodeData
            {
                guid = introGuid,
                id = "mansion_intro_2",
                speaker = "주인공",
                text = "여긴 어디지...? 방금 전까지 집에 있었는데...",
                triggerEvent = "",
                position = new Vector2(400, 250),
                nextNodeGuid = choiceGuid
            };

            // Node 3: 메인 분기선택
            var choiceNode = new DialogueNodeData
            {
                guid = choiceGuid,
                id = "mansion_choice",
                speaker = "",
                text = "복도 끝에 굳게 닫힌 문이 있고, 바닥에는 낡은 종이 조각이 떨어져 있다.",
                triggerEvent = "",
                position = new Vector2(700, 250),
                choices = new List<DialogueChoiceData>
                {
                    new DialogueChoiceData { text = "문으로 다가간다", nextNodeGuid = doorGuid },
                    new DialogueChoiceData { text = "종이 조각을 줍는다", nextNodeGuid = paperGuid }
                }
            };

            // Node 4: 문 상호작용 분기
            var doorNode = new DialogueNodeData
            {
                guid = doorGuid,
                id = "door_interact",
                speaker = "",
                text = "문은 굳게 잠겨 있다. 열쇠 구멍은 녹슬어 있다.",
                triggerEvent = "",
                position = new Vector2(1050, 100),
                choices = new List<DialogueChoiceData>
                {
                    new DialogueChoiceData { text = "열쇠를 사용해본다", nextNodeGuid = unlockGuid, requiredItem = "mansion_key" },
                    new DialogueChoiceData { text = "몸으로 밀어본다", nextNodeGuid = pushGuid },
                    new DialogueChoiceData { text = "돌아선다", nextNodeGuid = choiceGuid }
                }
            };

            // Node 5: 종이 조각 획득
            var paperNode = new DialogueNodeData
            {
                guid = paperGuid,
                id = "paper_interact",
                speaker = "",
                text = "낡은 종이에는 피로 쓴 듯한 글씨가 적혀 있다.\n<color=red>'열쇠는 책상 서랍 속에...'</color>",
                triggerEvent = "gain_key_hint",
                position = new Vector2(1050, 450),
                nextNodeGuid = choiceGuid
            };

            // Node 6: 몸으로 밀기 (실패)
            var pushNode = new DialogueNodeData
            {
                guid = pushGuid,
                id = "door_push",
                speaker = "주인공",
                text = "윽... 끄떡도 하지 않는다. 열쇠를 찾아야 할 것 같다.",
                triggerEvent = "camera_shake",
                position = new Vector2(1400, 10),
                nextNodeGuid = choiceGuid
            };

            // Node 7: 열쇠 열기 (성공)
            var unlockNode = new DialogueNodeData
            {
                guid = unlockGuid,
                id = "door_unlock",
                speaker = "",
                text = "찰칵. 열쇠가 부드럽게 돌아가며 문이 열린다.",
                triggerEvent = "play_door_open_sound",
                position = new Vector2(1400, 220),
                nextNodeGuid = escapeGuid
            };

            // Node 8: 탈출 성공 엔딩
            var escapeNode = new DialogueNodeData
            {
                guid = escapeGuid,
                id = "escape_success",
                speaker = "",
                text = "문 너머로 어두운 숲이 보인다. 마침내 이곳을 빠져나왔다.",
                triggerEvent = "game_over_win",
                position = new Vector2(1700, 220),
                nextNodeGuid = ""
            };

            // Add all nodes to graph list
            container.dialogueNodes.Add(startNode);
            container.dialogueNodes.Add(introNode);
            container.dialogueNodes.Add(choiceNode);
            container.dialogueNodes.Add(doorNode);
            container.dialogueNodes.Add(paperNode);
            container.dialogueNodes.Add(pushNode);
            container.dialogueNodes.Add(unlockNode);
            container.dialogueNodes.Add(escapeNode);

            // 4. Create visual node link wires
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = startGuid, targetNodeGuid = introGuid, portName = "Next" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = introGuid, targetNodeGuid = choiceGuid, portName = "Next" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = choiceGuid, targetNodeGuid = doorGuid, portName = "Choice_0" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = choiceGuid, targetNodeGuid = paperGuid, portName = "Choice_1" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = doorGuid, targetNodeGuid = unlockGuid, portName = "Choice_0" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = doorGuid, targetNodeGuid = pushGuid, portName = "Choice_1" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = doorGuid, targetNodeGuid = choiceGuid, portName = "Choice_2" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = paperGuid, targetNodeGuid = choiceGuid, portName = "Next" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = pushGuid, targetNodeGuid = choiceGuid, portName = "Next" });
            container.nodeLinks.Add(new NodeLinkData { baseNodeGuid = unlockGuid, targetNodeGuid = escapeGuid, portName = "Next" });

            // 5. Save SO asset to database
            AssetDatabase.CreateAsset(container, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Dialogue Sample Generator", 
                "Sample Dialogue Graph Asset created successfully!\n\nLocation: Assets/Resources/Dialogues/SampleGraph.asset", "OK");
        }
    }
}
