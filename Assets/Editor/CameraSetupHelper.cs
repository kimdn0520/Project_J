#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Core;

public class CameraSetupHelper
{
    public static void Execute()
    {
        string persistentPath = "Assets/Scenes/Persistent.unity";
        var scene = EditorSceneManager.OpenScene(persistentPath, OpenSceneMode.Single);

        // Find any camera in the scene
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            GameObject camObj = cam.gameObject;
            if (camObj.GetComponent<CameraFollow>() == null)
            {
                camObj.AddComponent<CameraFollow>();
                Debug.Log("[CameraSetupHelper] Successfully added CameraFollow to: " + camObj.name);
            }
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            Debug.LogError("[CameraSetupHelper] No Camera found in the Persistent scene to attach CameraFollow.");
        }
    }
}
#endif
