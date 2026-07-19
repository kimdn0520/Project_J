using UnityEngine;
using UnityEngine.SceneManagement;
using SaveSystem;
using DialogSystem;
using MapSystem;
using Cysharp.Threading.Tasks;

namespace Core
{
    public class PersistentSceneBootstrapper : MonoBehaviour
    {
        [Header("Bootstrapper Config")]
        [SerializeField] private string titleSceneName = "Title";
        [SerializeField] private string startSceneName = "Map_00_HotelExterior";
        [SerializeField] private string startSpawnId = "Spawn_Default";

        public void SetStartScene(string sceneName, string spawnId)
        {
            startSceneName = sceneName;
            startSpawnId = spawnId;
        }

        private void Awake()
        {
            InitializeServices();
        }

        private void Start()
        {
            BootstrapInitialScene().Forget();
        }

        private async UniTaskVoid BootstrapInitialScene()
        {
            await UniTask.Yield(); // Wait one frame for singletons to initialize

            // Check if any map or title scene is already loaded alongside Persistent
            bool hasOtherScene = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != gameObject.scene.name && scene.name != "Persistent")
                {
                    hasOtherScene = true;
                    break;
                }
            }

            // If no other scene is loaded (e.g. launching Persistent scene directly), load Title scene additively
            if (!hasOtherScene)
            {
                Debug.Log($"[Bootstrapper] Persistent initialized. Loading initial Title scene: {titleSceneName}");
                if (Application.CanStreamedLevelBeLoaded(titleSceneName))
                {
                    SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Additive);
                }
                else
                {
                    Debug.LogWarning($"[Bootstrapper] Title scene '{titleSceneName}' not found in build settings.");
                }
            }
        }

        private void InitializeServices()
        {
            // Hook up DialogueManager delegates
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

                // Load Chapter 1 Dialogues JSON from Resources
                TextAsset chapter1Json = Resources.Load<TextAsset>("Dialogues/chapter1_dialogues");
                if (chapter1Json != null)
                {
                    DialogueManager.Instance.LoadDialogues(chapter1Json.text);
                    Debug.Log("[Bootstrapper] Chapter 1 dialogues JSON loaded successfully.");
                }
                else
                {
                    Debug.LogWarning("[Bootstrapper] Failed to load Dialogues/chapter1_dialogues JSON from Resources.");
                }

                // Register dialogue events
                DialogueEventDispatcher.Register("get_key_event", OnGetKey);
                DialogueEventDispatcher.Register("open_door_event", OnOpenDoor);
            }
            else
            {
                Debug.LogError("[Bootstrapper] DialogueManager is missing.");
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
            Debug.Log("[Event Callback] Key used! Door is now open.");
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.RemoveItem("key_corridor");
            }
        }
    }
}
