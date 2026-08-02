using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Framework.Lighting;
using Core;
using System.IO;

public class SetupLobbyLighting
{
    [MenuItem("Tools/Lighting/Setup Lobby 2D Lighting")]
    public static void Execute()
    {
        string lobbyScenePath = "Assets/Scenes/Maps/Map_01_Lobby.unity";
        if (!File.Exists(lobbyScenePath))
        {
            Debug.LogError($"[SetupLobbyLighting] Lobby scene not found at {lobbyScenePath}");
            return;
        }

        Scene lobbyScene = EditorSceneManager.OpenScene(lobbyScenePath, OpenSceneMode.Single);

        // 1. Ensure Lighting Container Object
        GameObject lightingRoot = GameObject.Find("Lobby_Lighting");
        if (lightingRoot == null)
        {
            lightingRoot = new GameObject("Lobby_Lighting");
        }

        // 2. Global Light 2D Setup (Dark Midnight Blue ambience)
        Transform globalLightTrans = lightingRoot.transform.Find("GlobalLight_2D");
        GameObject globalLightGo;
        if (globalLightTrans == null)
        {
            globalLightGo = new GameObject("GlobalLight_2D");
            globalLightGo.transform.SetParent(lightingRoot.transform);
        }
        else
        {
            globalLightGo = globalLightTrans.gameObject;
        }

        Light2D globalLight = globalLightGo.GetComponent<Light2D>();
        if (globalLight == null)
        {
            globalLight = globalLightGo.AddComponent<Light2D>();
        }

        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.09f, 0.11f, 0.18f, 1f); // Deep midnight blue (#171C2E)
        globalLight.intensity = 0.22f; // Dark horror atmosphere

        // 3. Front Desk Lamp Light (Warm Warm Orange Light)
        CreateOrUpdatePointLight(lightingRoot.transform, "FrontDesk_LampLight", new Vector3(0f, 1.5f, 0f), 
            new Color(1.0f, 0.65f, 0.35f, 1.0f), 0.95f, 0.6f, 3.8f);

        // 4. Grand Clock & Clock Light (Left Wall Clock)
        GameObject clockObj = GameObject.Find("GrandClock");
        if (clockObj == null)
        {
            clockObj = new GameObject("GrandClock");
            clockObj.transform.position = new Vector3(-5.5f, 4.0f, 0f);
            
            // Add SpriteRenderer with dark wood palette box if no sprite available
            SpriteRenderer sr = clockObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sr.color = new Color(0.35f, 0.22f, 0.12f, 1.0f); // Dark wood brown
            clockObj.transform.localScale = new Vector3(0.8f, 1.6f, 1.0f);

            var ysort = clockObj.AddComponent<YSortable>();
            ysort.isStatic = true;
        }
        CreateOrUpdatePointLight(clockObj.transform, "Clock_Light", Vector3.zero, 
            new Color(0.9f, 0.75f, 0.3f, 1.0f), 0.65f, 0.3f, 2.5f);

        // 5. Window & MoonLight (Right Wall Window)
        GameObject windowObj = GameObject.Find("LobbyWindow");
        if (windowObj == null)
        {
            windowObj = new GameObject("LobbyWindow");
            windowObj.transform.position = new Vector3(5.5f, 4.5f, 0f);

            SpriteRenderer sr = windowObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sr.color = new Color(0.2f, 0.35f, 0.5f, 0.85f); // Cool glass blue
            windowObj.transform.localScale = new Vector3(1.2f, 1.8f, 1.0f);
        }
        CreateOrUpdatePointLight(windowObj.transform, "MoonLight_2D", Vector3.zero, 
            new Color(0.35f, 0.55f, 0.95f, 1.0f), 0.85f, 0.5f, 4.5f);

        // 6. Northern Corridor Entrance Ambient Light (Eerie Dim Reddish Light)
        CreateOrUpdatePointLight(lightingRoot.transform, "CorridorEntrance_Light", new Vector3(0f, 5.2f, 0f), 
            new Color(0.7f, 0.35f, 0.35f, 1.0f), 0.55f, 0.4f, 3.2f);

        EditorSceneManager.MarkSceneDirty(lobbyScene);
        EditorSceneManager.SaveScene(lobbyScene);
        AssetDatabase.SaveAssets();

        Debug.Log("[SetupLobbyLighting] Map_01_Lobby 2D Horror Lighting setup complete!");
    }

    private static GameObject CreateOrUpdatePointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float innerRadius, float outerRadius)
    {
        Transform child = parent.Find(name);
        GameObject lightGo;
        if (child == null)
        {
            lightGo = new GameObject(name);
            lightGo.transform.SetParent(parent);
            lightGo.transform.localPosition = localPos;
        }
        else
        {
            lightGo = child.gameObject;
            lightGo.transform.localPosition = localPos;
        }

        Light2D lightComp = lightGo.GetComponent<Light2D>();
        if (lightComp == null)
        {
            lightComp = lightGo.AddComponent<Light2D>();
        }

        lightComp.lightType = Light2D.LightType.Point;
        lightComp.color = color;
        lightComp.intensity = intensity;
        lightComp.pointLightInnerRadius = innerRadius;
        lightComp.pointLightOuterRadius = outerRadius;

        return lightGo;
    }
}
