using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class FixLobbyObjectSorting
{
    public static void Execute()
    {
        string lobbyScenePath = "Assets/Scenes/Maps/Map_01_Lobby.unity";
        if (!File.Exists(lobbyScenePath))
        {
            Debug.LogError($"[FixLobbyObjectSorting] Scene file not found at {lobbyScenePath}");
            return;
        }

        Scene lobbyScene = EditorSceneManager.OpenScene(lobbyScenePath, OpenSceneMode.Single);

        // Find FrontDesk and DeskMemo in active scene
        GameObject[] rootObjects = lobbyScene.GetRootGameObjects();

        foreach (var rootObj in rootObjects)
        {
            FixSortingRecursive(rootObj.transform);
        }

        EditorSceneManager.MarkSceneDirty(lobbyScene);
        EditorSceneManager.SaveScene(lobbyScene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FixLobbyObjectSorting] Successfully updated sorting orders in {lobbyScenePath}");
    }

    private static void FixSortingRecursive(Transform trans)
    {
        string name = trans.name.ToLower();
        SpriteRenderer sr = trans.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            if (name.Contains("desk") || name.Contains("frontdesk") || name.Contains("counter"))
            {
                sr.sortingOrder = 15;
                Debug.Log($"[FixLobbyObjectSorting] Set {trans.name} SpriteRenderer sortingOrder = 15");
            }
            else if (name.Contains("memo") || name.Contains("deskmemo") || name.Contains("note"))
            {
                sr.sortingOrder = 16;
                Debug.Log($"[FixLobbyObjectSorting] Set {trans.name} SpriteRenderer sortingOrder = 16");
            }
        }

        foreach (Transform child in trans)
        {
            FixSortingRecursive(child);
        }
    }
}
