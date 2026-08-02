using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using Core;

public class ApplyYSortableToLobby
{
    public static void Execute()
    {
        // 1. Update Player Prefab Visual with YSortable
        string[] playerGuids = AssetDatabase.FindAssets("Player t:Prefab");
        foreach (string guid in playerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot != null)
            {
                Transform visual = prefabRoot.transform.Find("Visual");
                GameObject targetGo = visual != null ? visual.gameObject : prefabRoot;
                
                SpriteRenderer sr = targetGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    YSortable ySort = targetGo.GetComponent<YSortable>();
                    if (ySort == null)
                    {
                        ySort = targetGo.AddComponent<YSortable>();
                    }
                    ySort.SetStatic(false);
                    Debug.Log($"[ApplyYSortableToLobby] Attached YSortable to Player prefab ({path})");
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // 2. Update Map_01_Lobby scene objects
        string lobbyScenePath = "Assets/Scenes/Maps/Map_01_Lobby.unity";
        if (File.Exists(lobbyScenePath))
        {
            Scene lobbyScene = EditorSceneManager.OpenScene(lobbyScenePath, OpenSceneMode.Single);

            GameObject[] rootObjects = lobbyScene.GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                ApplyYSortRecursive(rootObj.transform);
            }

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ApplyYSortableToLobby] Saved Y-Sorting setup to scene {lobbyScenePath}");
        }
    }

    private static void ApplyYSortRecursive(Transform trans)
    {
        string name = trans.name.ToLower();
        SpriteRenderer sr = trans.GetComponent<SpriteRenderer>();

        if (sr != null && !trans.name.StartsWith("Tilemap_"))
        {
            YSortable ySort = trans.GetComponent<YSortable>();
            if (ySort == null)
            {
                ySort = trans.gameObject.AddComponent<YSortable>();
            }

            bool isPlayer = name.Contains("player") || trans.CompareTag("Player") || (trans.parent != null && trans.parent.CompareTag("Player"));
            bool isMemo = name.Contains("memo") || name.Contains("note") || name.Contains("paper");

            ySort.SetStatic(!isPlayer);

            if (isMemo && trans.parent != null)
            {
                ySort.SetFollowParent(true, 2); // Memo paper sits on top of Desk (Desk Order + 2)
                Debug.Log($"[ApplyYSortableToLobby] Set {trans.name} to Follow Parent YSort +2");
            }
            else
            {
                ySort.SetFollowParent(false);
            }

            ySort.UpdateSortingOrder();

            Debug.Log($"[ApplyYSortableToLobby] Applied YSortable to {trans.name} (isStatic: {!isPlayer})");
        }

        foreach (Transform child in trans)
        {
            ApplyYSortRecursive(child);
        }
    }
}
