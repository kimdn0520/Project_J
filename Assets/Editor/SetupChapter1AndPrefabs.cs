#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MapSystem;
using DialogSystem;
using Core;
using System.IO;
using System.Collections.Generic;

namespace EditorTools
{
    public static class SetupChapter1AndPrefabs
    {
        [MenuItem("Tools/Setup Chapter 1 & Prefabs")]
        public static void Execute()
        {
            Debug.Log("=== Starting Setup Chapter 1 & Prefabs ===");

            // Ensure Prefabs and Scenes/Maps directories exist
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
            }
            if (!Directory.Exists("Assets/Scenes/Maps"))
            {
                Directory.CreateDirectory("Assets/Scenes/Maps");
            }

            Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            string exteriorScenePath = "Assets/Scenes/Maps/Map_00_HotelExterior.unity";

            // 1. Open Persistent.unity and ensure Player prefab
            string persistentScenePath = "Assets/Scenes/Persistent.unity";
            Scene persistentScene = EditorSceneManager.OpenScene(persistentScenePath, OpenSceneMode.Single);
            
            PersistentSceneBootstrapper bootstrapper = Object.FindFirstObjectByType<PersistentSceneBootstrapper>();
            if (bootstrapper != null)
            {
                bootstrapper.SetStartScene("Map_00_HotelExterior", "Spawn_Default");
                EditorUtility.SetDirty(bootstrapper);
            }

            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                string prefabPath = "Assets/Prefabs/Player.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(playerObj, prefabPath, InteractionMode.AutomatedAction);
            }

            EditorSceneManager.MarkSceneDirty(persistentScene);
            EditorSceneManager.SaveScene(persistentScene);

            // -------------------------------------------------------------
            // Note: Map_00_HotelExterior.unity is modified by user and preserved as-is!
            // -------------------------------------------------------------

            // -------------------------------------------------------------
            // 2. Build Map_01_Lobby.unity (Clean Lobby with Single Desk Object)
            // -------------------------------------------------------------
            string lobbyScenePath = "Assets/Scenes/Maps/Map_01_Lobby.unity";
            Scene lobbyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject lobbyControllerObj = new GameObject("MapController");
            lobbyControllerObj.AddComponent<Map01LobbyController>();

            // SpawnPoints
            GameObject lobbySpawnExt = new GameObject("SpawnPoint_FromExterior");
            lobbySpawnExt.transform.position = new Vector3(0, -3f, 0);
            SpawnPoint spExtComp = lobbySpawnExt.AddComponent<SpawnPoint>();
            spExtComp.SetSpawnId("Spawn_FromExterior");

            GameObject lobbySpawnDef = new GameObject("SpawnPoint_Default");
            lobbySpawnDef.transform.position = new Vector3(0, -3f, 0);
            SpawnPoint spDefComp = lobbySpawnDef.AddComponent<SpawnPoint>();
            spDefComp.SetSpawnId("Spawn_Default");

            // Clean Background Floor & Wall
            CreateSpriteObject("Background_Lobby", defaultSprite, new Vector3(0, 0, 0), new Vector3(20f, 12f, 1f), new Color(0.12f, 0.1f, 0.12f, 1f), -15);
            CreateSpriteObject("Lobby_Floor_Carpet", defaultSprite, new Vector3(0, -2f, 0), new Vector3(5f, 8f, 1f), new Color(0.35f, 0.08f, 0.08f, 1f), -14);

            // Simple Front Desk Object (Single Square Sprite)
            GameObject deskObj = CreateSpriteObject("Front_Desk", defaultSprite, new Vector3(0, 0.5f, 0), new Vector3(4.5f, 1.6f, 1f), new Color(0.24f, 0.14f, 0.09f, 1f), -5);
            BoxCollider2D deskCol = deskObj.AddComponent<BoxCollider2D>();
            deskCol.size = new Vector2(1f, 1f);

            // Memo Paper on Desk (Interactable Trigger)
            GameObject memoObj = new GameObject("Desk_Memo_Trigger");
            memoObj.transform.position = new Vector3(0, 0.8f, 0);
            BoxCollider2D memoCol = memoObj.AddComponent<BoxCollider2D>();
            memoCol.isTrigger = true;
            memoCol.size = new Vector2(2.0f, 2.0f);

            InteractionTrigger memoTrigger = memoObj.AddComponent<InteractionTrigger>();
            memoTrigger.SetDefaultDialogueNodeId("lobby_memo_1");

            // Paper Visual on Desk
            GameObject memoVisual = CreateSpriteObject("Memo_Paper", defaultSprite, new Vector3(0, 0.8f, 0), new Vector3(0.5f, 0.6f, 1f), new Color(0.95f, 0.92f, 0.75f, 1f), -4);
            memoVisual.transform.SetParent(memoObj.transform);

            // Stairs Scene Door to 2F
            GameObject stairsDoorObj = new GameObject("StairsTo2F");
            stairsDoorObj.transform.position = new Vector3(0, 4.5f, 0);
            BoxCollider2D stairsCol = stairsDoorObj.AddComponent<BoxCollider2D>();
            stairsCol.isTrigger = true;
            stairsCol.size = new Vector2(2.5f, 1.2f);

            SceneDoor stairsDoor = stairsDoorObj.AddComponent<SceneDoor>();
            stairsDoor.SetTarget("Map_02_Corridor", "Spawn_FromLobby");

            EditorSceneManager.SaveScene(lobbyScene, lobbyScenePath);
            Debug.Log($"[Setup] Saved clean Map_01_Lobby scene at {lobbyScenePath}");

            // -------------------------------------------------------------
            // 3. Sync Build Settings
            // -------------------------------------------------------------
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            if (File.Exists("Assets/Scenes/Persistent.unity"))
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Persistent.unity", true));
            if (File.Exists("Assets/Scenes/Title.unity"))
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true));
            if (File.Exists(exteriorScenePath))
                buildScenes.Add(new EditorBuildSettingsScene(exteriorScenePath, true));
            if (File.Exists(lobbyScenePath))
                buildScenes.Add(new EditorBuildSettingsScene(lobbyScenePath, true));
            if (File.Exists("Assets/Scenes/Maps/Map_01_Start.unity"))
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_01_Start.unity", true));
            if (File.Exists("Assets/Scenes/Maps/Map_02_Corridor.unity"))
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_02_Corridor.unity", true));

            EditorBuildSettings.scenes = buildScenes.ToArray();

            EditorSceneManager.OpenScene(persistentScenePath, OpenSceneMode.Single);
            AssetDatabase.Refresh();

            Debug.Log("=== Setup Chapter 1 & Prefabs Completed Successfully! ===");
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int sortingOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return obj;
        }
    }
}
#endif
