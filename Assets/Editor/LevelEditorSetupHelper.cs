using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class LevelEditorSetupHelper
{
    public static void Execute()
    {
        Debug.Log("레벨 에디터 자동 셋업을 시작합니다...");

        // 1. 새 씬 생성 및 액티브로 설정
        UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "LevelEditor";

        // 기존의 Main Camera와 Directional Light를 얻음
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.transform.position = new Vector3(0, 0, -10);
        }

        // 2. Grid 및 LevelRoot 생성
        GameObject gridObj = new GameObject("Grid");
        Grid gridComponent = gridObj.AddComponent<Grid>();
        gridObj.AddComponent<GridLineRenderer>();

        GameObject levelRootObj = new GameObject("LevelRoot");
        levelRootObj.transform.SetParent(gridObj.transform);

        // 3. Tilemap 레이어 생성
        GameObject floorTmObj = new GameObject("Floor_Tilemap");
        floorTmObj.transform.SetParent(levelRootObj.transform);
        Tilemap floorTm = floorTmObj.AddComponent<Tilemap>();
        TilemapRenderer floorTmr = floorTmObj.AddComponent<TilemapRenderer>();
        floorTmr.sortingOrder = 0;

        GameObject wallTmObj = new GameObject("Wall_Tilemap");
        wallTmObj.transform.SetParent(levelRootObj.transform);
        Tilemap wallTm = wallTmObj.AddComponent<Tilemap>();
        TilemapRenderer wallTmr = wallTmObj.AddComponent<TilemapRenderer>();
        wallTmr.sortingOrder = 1;

        // 4. Object Layer 생성
        GameObject objectLayerObj = new GameObject("Object_Layer");
        objectLayerObj.transform.SetParent(levelRootObj.transform);

        // 5. LevelEditorManager 생성
        GameObject managerObj = new GameObject("LevelEditorManager");
        LevelEditorManager manager = managerObj.AddComponent<LevelEditorManager>();

        // 6. Ghost Preview 생성 및 할당
        GameObject ghostObj = new GameObject("GhostPreview");
        ghostObj.transform.SetParent(managerObj.transform);
        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sortingOrder = 999;

        // LevelEditorManager 필드 할당
        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("grid").objectReferenceValue = gridComponent;
        managerSO.FindProperty("levelRoot").objectReferenceValue = levelRootObj.transform;
        managerSO.FindProperty("ghostPreviewRenderer").objectReferenceValue = ghostSr;

        // 7. 테스트용 LevelItemData 및 Tile 에셋 강제 생성 및 갱신 (오염 방지)
        if (!Directory.Exists("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // 기존 에셋이 존재하면 안전하게 미리 삭제
        AssetDatabase.DeleteAsset("Assets/Resources/SampleFloorTile.asset");
        AssetDatabase.DeleteAsset("Assets/Resources/SampleFloorItem.asset");

        // Square 스프라이트의 PPU 및 피벗을 1x1 크기에 맞게 임포트 설정 변경
        string spritePath = "Assets/Sprites/Square.png";
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (tex != null)
            {
                importer.spritePixelsPerUnit = tex.width; // 가로 크기를 PPU로 설정하여 정확히 1.0 유닛 크기로 채움
            }
            else
            {
                importer.spritePixelsPerUnit = 8;
            }
            
            importer.spritePivot = new Vector2(0.5f, 0.5f); // Center 피벗 강제화
            importer.filterMode = FilterMode.Point; // 쯔꾸르 픽셀 아트 풍을 위해 필터 모드를 Point로 지정
            importer.SaveAndReimport();
        }

        // Square 스프라이트를 찾아 타일로 맵핑
        Sprite sqSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        Tile sampleTile = ScriptableObject.CreateInstance<Tile>();
        sampleTile.sprite = sqSprite;
        AssetDatabase.CreateAsset(sampleTile, "Assets/Resources/SampleFloorTile.asset");

        LevelItemData sampleItem = ScriptableObject.CreateInstance<LevelItemData>();
        sampleItem.itemName = "샘플 바닥 타일";
        sampleItem.itemType = LevelItemData.ItemType.Tile;
        sampleItem.targetLayerName = "Floor_Tilemap";
        sampleItem.tile = sampleTile;
        sampleItem.thumbnail = sqSprite;
        AssetDatabase.CreateAsset(sampleItem, "Assets/Resources/SampleFloorItem.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 갱신된 데이터베이스에서 LevelItemData 검색 및 수집
        string[] guids = AssetDatabase.FindAssets("t:LevelItemData");
        List<LevelItemData> items = new List<LevelItemData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelItemData itemData = AssetDatabase.LoadAssetAtPath<LevelItemData>(path);
            if (itemData != null)
            {
                items.Add(itemData);
            }
        }

        SerializedProperty itemsProp = managerSO.FindProperty("availableItems");
        itemsProp.ClearArray();
        for (int i = 0; i < items.Count; i++)
        {
            itemsProp.InsertArrayElementAtIndex(i);
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
        managerSO.ApplyModifiedProperties();

        // 8. UI 생성 및 구성
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 모드로 복구하여 정확한 픽셀 매핑 보장
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem이 없으면 추가 생성
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // UI Panel_LevelEditorUI 생성
        GameObject panelObj = new GameObject("Panel_LevelEditorUI");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        LevelEditorUI editorUI = panelObj.AddComponent<LevelEditorUI>();

        // MouseFollower UI 이미지 생성
        GameObject followerObj = new GameObject("MouseFollower");
        followerObj.transform.SetParent(panelObj.transform, false);
        RectTransform followerRect = followerObj.AddComponent<RectTransform>();
        followerRect.sizeDelta = new Vector2(40, 40);
        followerRect.anchorMin = new Vector2(0.5f, 0.5f);
        followerRect.anchorMax = new Vector2(0.5f, 0.5f);
        Image followerImg = followerObj.AddComponent<Image>();
        followerImg.raycastTarget = false; // 마우스 클릭 방해 방지
        followerObj.SetActive(false); // 기본 비활성화

        // ScrollView 생성 (하단 브러시 팔레트 영역)
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(panelObj.transform, false);
        RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 0.25f); // 하단 25% 차지
        scrollRect.offsetMin = new Vector2(20, 20);
        scrollRect.offsetMax = new Vector2(-20, 0);
        Image scrollBg = scrollViewObj.AddComponent<Image>();
        scrollBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        ScrollRect scrollComp = scrollViewObj.AddComponent<ScrollRect>();

        // Viewport 생성
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform viewRect = viewportObj.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.sizeDelta = Vector2.zero;
        viewportObj.AddComponent<Image>();
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content 생성
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0.5f);
        contentRect.anchorMax = new Vector2(0, 0.5f);
        contentRect.pivot = new Vector2(0, 0.5f);
        contentRect.sizeDelta = new Vector2(0, 100);

        HorizontalLayoutGroup hGroup = contentObj.AddComponent<HorizontalLayoutGroup>();
        hGroup.spacing = 10;
        hGroup.padding = new RectOffset(10, 10, 10, 10);
        hGroup.childAlignment = TextAnchor.MiddleLeft;
        hGroup.childForceExpandWidth = false;
        hGroup.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollComp.viewport = viewRect;
        scrollComp.content = contentRect;
        scrollComp.horizontal = true;
        scrollComp.vertical = false;

        // PaletteButton Prefab 자동 생성
        GameObject buttonTemplate = CreatePaletteButtonTemplate();
        string prefabPath = "Assets/Prefabs/PaletteButton.prefab";
        if (!Directory.Exists("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        GameObject buttonPrefab = PrefabUtility.SaveAsPrefabAsset(buttonTemplate, prefabPath);
        Object.DestroyImmediate(buttonTemplate);

        // Save, Clear 버튼 생성
        GameObject saveBtnObj = CreateTextButton("Button_Save", "저장", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-100, -50), new Vector2(160, 50), panelObj.transform);
        GameObject clearBtnObj = CreateTextButton("Button_Clear", "전체 지우기", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-280, -50), new Vector2(160, 50), panelObj.transform);

        // LevelEditorUI 컴포넌트 필드 바인딩
        SerializedObject uiSO = new SerializedObject(editorUI);
        uiSO.FindProperty("paletteContentParent").objectReferenceValue = contentRect;
        uiSO.FindProperty("paletteButtonPrefab").objectReferenceValue = buttonPrefab;
        uiSO.FindProperty("saveButton").objectReferenceValue = saveBtnObj.GetComponent<Button>();
        uiSO.FindProperty("clearButton").objectReferenceValue = clearBtnObj.GetComponent<Button>();
        uiSO.FindProperty("mouseFollowerIcon").objectReferenceValue = followerRect;
        uiSO.ApplyModifiedProperties();

        // 9. 씬 파일 저장
        if (!Directory.Exists("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        string sceneFilePath = "Assets/Scenes/LevelEditor.unity";
        bool saveSuccess = EditorSceneManager.SaveScene(newScene, sceneFilePath);

        if (saveSuccess)
        {
            Debug.Log($"<color=green><b>[레벨 에디터 셋업]</b></color> 성공적으로 씬을 생성 및 저장했습니다: {sceneFilePath}");
            AddSceneToBuildSettings(sceneFilePath);
        }
        else
        {
            Debug.LogError("레벨 에디터 씬 저장에 실패했습니다.");
        }
    }

    private static GameObject CreatePaletteButtonTemplate()
    {
        GameObject btnObj = new GameObject("PaletteButton");
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(100, 100);
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.color = Color.white;
        btnObj.AddComponent<Button>();

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.3f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = new Color(1, 1, 1, 0.5f);

        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.25f);
        textRect.sizeDelta = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 12;
        txt.color = Color.black;
        txt.text = "Item";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btnObj;
    }

    private static GameObject CreateTextButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Transform parent)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        btnObj.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 16;
        txt.color = Color.white;
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btnObj;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> newScenes = new List<EditorBuildSettingsScene>();

        bool alreadyExists = false;
        foreach (var scene in scenes)
        {
            newScenes.Add(scene);
            if (scene.path == scenePath)
            {
                alreadyExists = true;
            }
        }

        if (!alreadyExists)
        {
            newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = newScenes.ToArray();
            Debug.Log($"Build Settings에 {scenePath} 씬이 추가되었습니다.");
        }
    }
}
