using UnityEngine;
using SaveSystem;
using DialogSystem;
using MapSystem;
using Cysharp.Threading.Tasks;

namespace Core
{
    public class PersistentSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private string startSceneName = "Map_01_Start";
        [SerializeField] private string startSpawnId = "start_point";

        private void Start()
        {
            // 1. Hook up DialogueManager delegates
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnCheckItem = (itemId) => {
                    return PlayerStatus.Instance != null && PlayerStatus.Instance.HasItem(itemId);
                };
                
                DialogueManager.Instance.OnCheckFlag = (flagName) => {
                    if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
                    {
                        return SaveManager.Instance.CurrentSaveData.HasFlag(flagName);
                    }
                    return false;
                };

                DialogueManager.Instance.OnSetFlag = (flagName, val) => {
                    if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
                    {
                        SaveManager.Instance.CurrentSaveData.SetFlag(flagName, val);
                        Debug.Log($"[Bootstrapper] Game Flag Set: {flagName} = {val}");
                    }
                };

                // Load local test dialogues JSON
                string testDialoguesJson = GetTestDialogueJson();
                DialogueManager.Instance.LoadDialogues(testDialoguesJson);
                Debug.Log("[Bootstrapper] Local test dialogues JSON loaded.");

                // Register dialogue events
                DialogueEventDispatcher.Register("get_key_event", OnGetKey);
                DialogueEventDispatcher.Register("open_door_event", OnOpenDoor);
            }
            else
            {
                Debug.LogError("[Bootstrapper] DialogueManager is missing.");
            }

            // 2. Start game loop
            if (SaveManager.Instance != null)
            {
                Debug.Log($"[Bootstrapper] Bootstrapping game starting at {startSceneName}");
                SaveManager.Instance.StartNewGame(startSceneName, startSpawnId);
            }
            else
            {
                Debug.LogError("[Bootstrapper] SaveManager instance not found. Cannot bootstrap game.");
            }
        }

        private void OnDestroy()
        {
            // Clean up event registrations to prevent memory leaks
            DialogueEventDispatcher.Unregister("get_key_event", OnGetKey);
            DialogueEventDispatcher.Unregister("open_door_event", OnOpenDoor);
        }

        private void OnGetKey()
        {
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.AddItem("key_corridor");
                Debug.Log("[Event Callback] Key 'key_corridor' added to player inventory!");
            }
        }

        private void OnOpenDoor()
        {
            Debug.Log("[Event Callback] Key used! Door is now open. Loading Map_01_Start...");
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.RemoveItem("key_corridor");
            }
            
            // Warp player back to Map_01_Start
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadSceneAsync("Map_01_Start", "start_point").Forget();
            }
        }

        private string GetTestDialogueJson()
        {
            return @"
{
  ""dialogues"": [
    {
      ""id"": ""desk_start"",
      ""speaker"": ""조사"",
      ""text"": ""오래된 책상이다. 서랍이 살짝 열려 있다..."",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": [
        {
          ""text"": ""서랍 안을 들여다본다"",
          ""nextNodeId"": ""desk_find_key"",
          ""requiredFlag"": """",
          ""requiredItem"": """",
          ""setFlag"": """",
          ""triggerEvent"": """"
        },
        {
          ""text"": ""그대로 둔다"",
          ""nextNodeId"": """",
          ""requiredFlag"": """",
          ""requiredItem"": """",
          ""setFlag"": """",
          ""triggerEvent"": """"
        }
      ]
    },
    {
      ""id"": ""desk_find_key"",
      ""speaker"": ""조사"",
      ""text"": ""서랍 속에 낡은 열쇠가 들어있다! 열쇠를 챙기겠습니까?"",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": [
        {
          ""text"": ""예 (열쇠를 챙긴다)"",
          ""nextNodeId"": ""desk_get_key_success"",
          ""requiredFlag"": """",
          ""requiredItem"": """",
          ""setFlag"": ""has_taken_key"",
          ""triggerEvent"": ""get_key_event""
        },
        {
          ""text"": ""아니오"",
          ""nextNodeId"": """",
          ""requiredFlag"": """",
          ""requiredItem"": """",
          ""setFlag"": """",
          ""triggerEvent"": """"
        }
      ]
    },
    {
      ""id"": ""desk_get_key_success"",
      ""speaker"": ""알림"",
      ""text"": ""복도 열쇠를 획득했다! 이제 어딘가 잠긴 문을 열 수 있을 것 같다."",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": []
    },
    {
      ""id"": ""desk_empty"",
      ""speaker"": ""조사"",
      ""text"": ""비어있는 책상이다. 서랍 안에는 아무것도 없다."",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": []
    },
    {
      ""id"": ""door_no_key"",
      ""speaker"": ""조사"",
      ""text"": ""문이 굳게 닫혀 있다. 단단한 복도 열쇠가 있어야 열릴 것 같다."",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": []
    },
    {
      ""id"": ""door_has_key"",
      ""speaker"": ""조사"",
      ""text"": ""잠긴 문이다. 복도 열쇠를 사용하여 문을 열겠습니까?"",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": [
        {
          ""text"": ""예 (열쇠를 사용한다)"",
          ""nextNodeId"": ""door_open_success"",
          ""requiredFlag"": """",
          ""requiredItem"": ""key_corridor"",
          ""setFlag"": ""door_is_open"",
          ""triggerEvent"": ""open_door_event""
        },
        {
          ""text"": ""아니오"",
          ""nextNodeId"": """",
          ""requiredFlag"": """",
          ""requiredItem"": """",
          ""setFlag"": """",
          ""triggerEvent"": """"
        }
      ]
    },
    {
      ""id"": ""door_open_success"",
      ""speaker"": ""알림"",
      ""text"": ""철컥... 열쇠가 맞물려 돌아가며 문이 활짝 열렸다!"",
      ""nextNodeId"": """",
      ""triggerEvent"": """",
      ""choices"": []
    }
  ]
}
";
        }
    }
}
