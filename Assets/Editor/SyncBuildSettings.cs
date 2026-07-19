#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace EditorTools
{
    public static class SyncBuildSettings
    {
        [MenuItem("Tools/Sync Build Settings Scenes")]
        public static void Execute()
        {
            Debug.Log("=== Syncing Build Settings Scenes ===");

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            // Index 0: Persistent.unity
            if (File.Exists("Assets/Scenes/Persistent.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Persistent.unity", true));
            }

            // Index 1: Title.unity
            if (File.Exists("Assets/Scenes/Title.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true));
            }

            // Index 2: Map_00_HotelExterior.unity (User modified scene preserved)
            if (File.Exists("Assets/Scenes/Maps/Map_00_HotelExterior.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_00_HotelExterior.unity", true));
            }

            // Index 3: Map_01_Lobby.unity
            if (File.Exists("Assets/Scenes/Maps/Map_01_Lobby.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_01_Lobby.unity", true));
            }

            if (File.Exists("Assets/Scenes/Maps/Map_01_Start.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_01_Start.unity", true));
            }

            if (File.Exists("Assets/Scenes/Maps/Map_02_Corridor.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_02_Corridor.unity", true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[SyncBuildSettings] Successfully updated {buildScenes.Count} scenes in Build Settings.");
            AssetDatabase.Refresh();
        }
    }
}
#endif
