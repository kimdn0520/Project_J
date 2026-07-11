#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using Player;
using MapSystem;
using SaveSystem;
using DialogSystem;
using Core;

public class TempSetupHelper
{
    public static void Execute()
    {
        Debug.Log("[TempSetupHelper] Starting game setup...");

        string scenesDir = "Assets/Scenes";
        string mapsDir = "Assets/Scenes/Maps";
        
        if (!Directory.Exists(mapsDir))
        {
            Directory.CreateDirectory(mapsDir);
        }

        string persistentPath = $"{scenesDir}/Persistent.unity";
        string map01Path = $"{mapsDir}/Map_01_Start.unity";
        string map02Path = $"{mapsDir}/Map_02_Corridor.unity";

        CreateSceneIfNeeded(persistentPath);
        CreateSceneIfNeeded(map01Path);
        CreateSceneIfNeeded(map02Path);

        AddScenesToBuildSettings(new string[] { persistentPath, map01Path, map02Path });

        SetupPersistentScene(persistentPath);
        SetupMap01Scene(map01Path);
        SetupMap02Scene(map02Path);

        EditorSceneManager.OpenScene(persistentPath, OpenSceneMode.Single);

        Debug.Log("[TempSetupHelper] Setup complete! You can now load the Persistent scene and play.");
    }

    private static void CreateSceneIfNeeded(string path)
    {
        if (!File.Exists(path))
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, path);
            Debug.Log($"[TempSetupHelper] Created scene: {path}");
        }
    }

    private static void AddScenesToBuildSettings(string[] paths)
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;

        foreach (string path in paths)
        {
            if (!buildScenes.Exists(s => s.path == path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[TempSetupHelper] Updated Build Settings scenes.");
        }
    }

    private static void SetupPersistentScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        GameObject eventSystemObj = GameObject.Find("EventSystem");
        if (eventSystemObj == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[TempSetupHelper] Created EventSystem with InputSystemUIInputModule.");
        }

        GameObject soundMgrObj = GameObject.Find("SoundManager");
        if (soundMgrObj == null)
        {
            soundMgrObj = new GameObject("SoundManager", typeof(SoundManager));
        }

        GameObject sceneTransMgrObj = GameObject.Find("SceneTransitionManager");
        if (sceneTransMgrObj == null)
        {
            sceneTransMgrObj = new GameObject("SceneTransitionManager", typeof(SceneTransitionManager));
        }

        GameObject saveMgrObj = GameObject.Find("SaveManager");
        if (saveMgrObj == null)
        {
            saveMgrObj = new GameObject("SaveManager", typeof(SaveManager));
        }

        GameObject dialogMgrObj = GameObject.Find("DialogueManager");
        if (dialogMgrObj == null)
        {
            dialogMgrObj = new GameObject("DialogueManager", typeof(DialogueManager));
        }
        DialogueManager dialogMgr = dialogMgrObj.GetComponent<DialogueManager>();

        GameObject bootstrapperObj = GameObject.Find("PersistentSceneBootstrapper");
        if (bootstrapperObj == null)
        {
            bootstrapperObj = new GameObject("PersistentSceneBootstrapper", typeof(PersistentSceneBootstrapper));
        }

        GameObject dialogueCanvas = GameObject.Find("DialogueCanvas");
        if (dialogueCanvas == null)
        {
            CreateDialogueUI(dialogMgrObj, dialogMgr);
        }

        // Setup Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }
        if (playerObj == null)
        {
            playerObj = new GameObject("Player");
            playerObj.tag = "Player";
        }

        Sprite existingSprite = null;
        RuntimeAnimatorController existingAnimController = null;
        
        SpriteRenderer rootSR = playerObj.GetComponent<SpriteRenderer>();
        if (rootSR != null)
        {
            existingSprite = rootSR.sprite;
            Object.DestroyImmediate(rootSR);
        }

        Animator rootAnim = playerObj.GetComponent<Animator>();
        if (rootAnim != null)
        {
            existingAnimController = rootAnim.runtimeAnimatorController;
            Object.DestroyImmediate(rootAnim);
        }

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = playerObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D physicalCol = playerObj.GetComponent<BoxCollider2D>();
        if (physicalCol == null) physicalCol = playerObj.AddComponent<BoxCollider2D>();
        physicalCol.isTrigger = false;
        physicalCol.size = new Vector2(0.6f, 0.2f);
        physicalCol.offset = new Vector2(0f, -0.4f);

        // Visual child
        Transform visualTrans = playerObj.transform.Find("Visual");
        GameObject visualObj;
        if (visualTrans == null)
        {
            visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(playerObj.transform);
            visualObj.transform.localPosition = Vector3.zero;
        }
        else
        {
            visualObj = visualTrans.gameObject;
        }

        SpriteRenderer sr = visualObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = visualObj.AddComponent<SpriteRenderer>();
        
        if (sr.sprite == null)
        {
            if (existingSprite != null)
            {
                sr.sprite = existingSprite;
            }
            else
            {
                Sprite femaleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3DNPC_Characters/Female_NPC/Female1.png");
                if (femaleSprite != null)
                {
                    object[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/3DNPC_Characters/Female_NPC/Female1.png");
                    foreach (var s in sprites)
                    {
                        if (s is Sprite) { sr.sprite = (Sprite)s; break; }
                    }
                }
                else
                {
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
                }
            }
        }

        Animator anim = visualObj.GetComponent<Animator>();
        if (anim == null) anim = visualObj.AddComponent<Animator>();
        
        if (anim.runtimeAnimatorController == null)
        {
            if (existingAnimController != null)
            {
                anim.runtimeAnimatorController = existingAnimController;
            }
            else
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/3DNPC_Characters/Female_NPC/PlayerAnimator.controller");
                if (controller == null)
                {
                    controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/3DNPC_Characters/Female_NPC/Female1_0.controller");
                }
                if (controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                }
            }
        }

        // Trigger child
        Transform triggerTrans = playerObj.transform.Find("Trigger");
        GameObject triggerObj;
        if (triggerTrans == null)
        {
            triggerObj = new GameObject("Trigger");
            triggerObj.transform.SetParent(playerObj.transform);
            triggerObj.transform.localPosition = Vector3.zero;
        }
        else
        {
            triggerObj = triggerTrans.gameObject;
        }
        triggerObj.tag = "Untagged";

        BoxCollider2D triggerCol = triggerObj.GetComponent<BoxCollider2D>();
        if (triggerCol == null) triggerCol = triggerObj.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector2(0.7f, 0.9f);
        triggerCol.offset = new Vector2(0f, 0f);

        // PlayerInput
        UnityEngine.InputSystem.PlayerInput pi = playerObj.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi == null) pi = playerObj.AddComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi.actions == null)
        {
            var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actionsAsset != null)
            {
                pi.actions = actionsAsset;
                pi.defaultActionMap = "Player";
            }
        }

        // PlayerController & Interaction
        PlayerController pc = playerObj.GetComponent<PlayerController>();
        if (pc == null) pc = playerObj.AddComponent<PlayerController>();
        PlayerInteraction piInteract = playerObj.GetComponent<PlayerInteraction>();
        if (piInteract == null) piInteract = playerObj.AddComponent<PlayerInteraction>();
        
        SetSerializedValue(piInteract, "interactableLayer", -1);

        playerObj.layer = LayerMask.NameToLayer("Default");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TempSetupHelper] Persistent scene setup complete.");
    }

    private static void CreateDialogueUI(GameObject parent, DialogueManager dialogueMgr)
    {
        // Delete old UI canvas if exists to recreate clean
        GameObject oldCanvas = GameObject.Find("DialogueCanvas");
        if (oldCanvas != null)
        {
            Object.DestroyImmediate(oldCanvas);
        }

        GameObject canvasObj = new GameObject("DialogueCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dialogue Panel (Background)
        GameObject panelObj = new GameObject("DialoguePanel");
        panelObj.transform.SetParent(canvasObj.transform);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.05f);
        panelRect.anchorMax = new Vector2(0.85f, 0.35f);
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Speaker Text
        GameObject speakerObj = new GameObject("SpeakerText");
        speakerObj.transform.SetParent(panelObj.transform);
        TextMeshProUGUI speakerText = speakerObj.AddComponent<TextMeshProUGUI>();
        speakerText.fontSize = 22;
        speakerText.color = Color.cyan;
        speakerText.fontStyle = FontStyles.Bold;
        
        RectTransform speakerRect = speakerObj.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.05f, 0.8f);
        speakerRect.anchorMax = new Vector2(0.65f, 0.95f); // Limit width to prevent overlap with choices
        speakerRect.sizeDelta = Vector2.zero;
        speakerRect.anchoredPosition = Vector2.zero;

        // Dialogue Text
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(panelObj.transform);
        TextMeshProUGUI dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 18;
        dialogueText.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.65f, 0.75f); // Limit width to 65% so text doesn't overlap with buttons!
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Choice Button Container (Narrow, Right-aligned)
        GameObject choiceContainerObj = new GameObject("ChoiceContainer");
        choiceContainerObj.transform.SetParent(panelObj.transform);
        VerticalLayoutGroup layout = choiceContainerObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        RectTransform choiceRect = choiceContainerObj.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(0.68f, 0.1f); // Place at the right 30% area
        choiceRect.anchorMax = new Vector2(0.95f, 0.9f);
        choiceRect.sizeDelta = Vector2.zero;
        choiceRect.anchoredPosition = Vector2.zero;
        choiceContainerObj.SetActive(false);

        // Next Indicator
        GameObject indicatorObj = new GameObject("NextIndicator");
        indicatorObj.transform.SetParent(panelObj.transform);
        TextMeshProUGUI indicatorText = indicatorObj.AddComponent<TextMeshProUGUI>();
        indicatorText.text = "▼ Space/Z";
        indicatorText.fontSize = 14;
        indicatorText.color = Color.yellow;
        indicatorText.alignment = TextAlignmentOptions.Right;

        RectTransform indRect = indicatorObj.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0.05f); // Move next indicator left of the choices
        indRect.anchorMax = new Vector2(0.65f, 0.18f);
        indRect.sizeDelta = Vector2.zero;
        indRect.anchoredPosition = Vector2.zero;

        // Assemble DialogueUI component
        DialogueUI ui = canvasObj.AddComponent<DialogueUI>();
        SetSerializedValue(ui, "dialoguePanel", panelObj);
        SetSerializedValue(ui, "speakerText", speakerText);
        SetSerializedValue(ui, "dialogueText", dialogueText);
        SetSerializedValue(ui, "nextIndicator", indicatorObj);
        SetSerializedValue(ui, "choiceButtonContainer", choiceRect);

        // Load Choice Button Prefab
        Button buttonAsset = AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/Button.prefab");
        if (buttonAsset != null)
        {
            SetSerializedValue(ui, "choiceButtonPrefab", buttonAsset);
        }

        // Bind to DialogueManager
        SetSerializedValue(dialogueMgr, "dialogueUI", ui);
        
        Debug.Log("[TempSetupHelper] Created Dialogue Canvas and bound UI to DialogueManager.");
    }

    private static void SetupMap01Scene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        GameObject spawnObj = GameObject.Find("SpawnPoint_Start");
        if (spawnObj == null)
        {
            spawnObj = new GameObject("SpawnPoint_Start");
            spawnObj.transform.position = Vector3.zero;
        }
        SpawnPoint sp = spawnObj.GetComponent<SpawnPoint>();
        if (sp == null) sp = spawnObj.AddComponent<SpawnPoint>();
        SetSerializedValue(sp, "spawnId", "start_point");

        GameObject doorObj = GameObject.Find("DoorToCorridor");
        if (doorObj == null)
        {
            doorObj = new GameObject("DoorToCorridor");
            doorObj.transform.position = new Vector3(3f, 0f, 0f);
        }
        
        SceneDoor door = doorObj.GetComponent<SceneDoor>();
        if (door == null) door = doorObj.AddComponent<SceneDoor>();
        SetSerializedValue(door, "targetScene", "Map_02_Corridor");
        SetSerializedValue(door, "targetSpawnId", "from_map01");

        BoxCollider2D col = doorObj.GetComponent<BoxCollider2D>();
        if (col == null) col = doorObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        
        if (doorObj.GetComponent<SpriteRenderer>() == null)
        {
            col.size = new Vector2(1f, 1.5f);
            SpriteRenderer sr = doorObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
            sr.color = Color.blue;
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TempSetupHelper] Map_01_Start scene setup complete.");
    }

    private static void SetupMap02Scene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        GameObject spawnObj = GameObject.Find("SpawnPoint_FromMap01");
        if (spawnObj == null)
        {
            spawnObj = new GameObject("SpawnPoint_FromMap01");
            spawnObj.transform.position = new Vector3(-2f, 0f, 0f);
        }
        SpawnPoint sp = spawnObj.GetComponent<SpawnPoint>();
        if (sp == null) sp = spawnObj.AddComponent<SpawnPoint>();
        SetSerializedValue(sp, "spawnId", "from_map01");

        GameObject doorObj = GameObject.Find("DoorToStart");
        if (doorObj == null)
        {
            doorObj = new GameObject("DoorToStart");
            doorObj.transform.position = new Vector3(-5f, 0f, 0f);
        }
        
        SceneDoor door = doorObj.GetComponent<SceneDoor>();
        if (door == null) door = doorObj.AddComponent<SceneDoor>();
        SetSerializedValue(door, "targetScene", "Map_01_Start");
        SetSerializedValue(door, "targetSpawnId", "start_point");

        BoxCollider2D col = doorObj.GetComponent<BoxCollider2D>();
        if (col == null) col = doorObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        
        if (doorObj.GetComponent<SpriteRenderer>() == null)
        {
            col.size = new Vector2(1f, 1.5f);
            SpriteRenderer sr = doorObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
            sr.color = Color.red;
        }

        // --- INTERACTIVE SCENARIO OBJECTS ---
        // 1. Desk (Interactive Object)
        GameObject deskObj = GameObject.Find("Desk");
        if (deskObj == null)
        {
            deskObj = new GameObject("Desk");
            deskObj.transform.position = new Vector3(0f, 2f, 0f);
        }
        
        SpriteRenderer deskSr = deskObj.GetComponent<SpriteRenderer>();
        if (deskSr == null)
        {
            deskSr = deskObj.AddComponent<SpriteRenderer>();
            deskSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
            deskSr.color = new Color(0.6f, 0.4f, 0.2f);
        }
        
        BoxCollider2D deskCol = deskObj.GetComponent<BoxCollider2D>();
        if (deskCol == null) deskCol = deskObj.AddComponent<BoxCollider2D>();
        deskCol.size = new Vector2(1.2f, 1.2f);
        deskCol.isTrigger = false;

        InteractionTrigger deskTrigger = deskObj.GetComponent<InteractionTrigger>();
        if (deskTrigger == null) deskTrigger = deskObj.AddComponent<InteractionTrigger>();
        SetSerializedValue(deskTrigger, "defaultDialogueNodeId", "desk_start");
        
        SerializedObject soDesk = new SerializedObject(deskTrigger);
        SerializedProperty overridesProp = soDesk.FindProperty("overrides");
        overridesProp.ClearArray();
        overridesProp.InsertArrayElementAtIndex(0);
        SerializedProperty overrideElem = overridesProp.GetArrayElementAtIndex(0);
        overrideElem.FindPropertyRelative("nodeId").stringValue = "desk_empty";
        overrideElem.FindPropertyRelative("requiredFlag").stringValue = "has_taken_key";
        overrideElem.FindPropertyRelative("requiredItem").stringValue = "";
        soDesk.ApplyModifiedProperties();

        // 2. Locked Door (Interactive Object)
        GameObject lockedDoorObj = GameObject.Find("LockedDoor");
        if (lockedDoorObj == null)
        {
            lockedDoorObj = new GameObject("LockedDoor");
            lockedDoorObj.transform.position = new Vector3(3f, 2f, 0f);
        }

        SpriteRenderer lockedSr = lockedDoorObj.GetComponent<SpriteRenderer>();
        if (lockedSr == null)
        {
            lockedSr = lockedDoorObj.AddComponent<SpriteRenderer>();
            lockedSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");
            lockedSr.color = Color.magenta;
        }

        BoxCollider2D lockedCol = lockedDoorObj.GetComponent<BoxCollider2D>();
        if (lockedCol == null) lockedCol = lockedDoorObj.AddComponent<BoxCollider2D>();
        lockedCol.size = new Vector2(1.2f, 1.2f);
        lockedCol.isTrigger = false;

        InteractionTrigger lockedTrigger = lockedDoorObj.GetComponent<InteractionTrigger>();
        if (lockedTrigger == null) lockedTrigger = lockedDoorObj.AddComponent<InteractionTrigger>();
        SetSerializedValue(lockedTrigger, "defaultDialogueNodeId", "door_no_key");

        SerializedObject soDoor = new SerializedObject(lockedTrigger);
        SerializedProperty doorOverrides = soDoor.FindProperty("overrides");
        doorOverrides.ClearArray();
        
        doorOverrides.InsertArrayElementAtIndex(0);
        SerializedProperty overrideOpen = doorOverrides.GetArrayElementAtIndex(0);
        overrideOpen.FindPropertyRelative("nodeId").stringValue = "door_open_success";
        overrideOpen.FindPropertyRelative("requiredFlag").stringValue = "door_is_open";
        overrideOpen.FindPropertyRelative("requiredItem").stringValue = "";

        doorOverrides.InsertArrayElementAtIndex(1);
        SerializedProperty overrideKey = doorOverrides.GetArrayElementAtIndex(1);
        overrideKey.FindPropertyRelative("nodeId").stringValue = "door_has_key";
        overrideKey.FindPropertyRelative("requiredFlag").stringValue = "";
        overrideKey.FindPropertyRelative("requiredItem").stringValue = "key_corridor";
        
        soDoor.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TempSetupHelper] Map_02_Corridor scene setup complete.");
    }

    private static void SetSerializedValue(UnityEngine.Object obj, string propName, string value)
    {
        if (obj == null) return;
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
        {
            prop.stringValue = value;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedValue(UnityEngine.Object obj, string propName, UnityEngine.Object value)
    {
        if (obj == null) return;
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedValue(UnityEngine.Object obj, string propName, int value)
    {
        if (obj == null) return;
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
        {
            prop.intValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
