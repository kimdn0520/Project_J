using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 레벨 에디터 씬 자동 생성 헬퍼.
/// Unity 메뉴 > Level Editor > Setup Scene 으로 실행.
/// </summary>
public class LevelEditorSetupHelper
{
    [MenuItem("Level Editor/Setup Scene")]
    public static void SetupFromMenu() => Execute();

    // ── Layout constants ────────────────────────────────────────────────
    private const float SIDEBAR_WIDTH       = 240f;
    private const float TOP_BAR_HEIGHT      = 64f;
    private const float PANEL_HEADER_HEIGHT = 40f;
    private const float CATEGORY_TAB_HEIGHT = 36f;
    private const float COLOR_DARK          = 0.12f;
    private const float COLOR_MID           = 0.18f;
    private const float COLOR_ACCENT        = 0.22f;

    // ── Entry point ─────────────────────────────────────────────────────
    public static void Execute()
    {
        Debug.Log("레벨 에디터 자동 셋업을 시작합니다...");

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "LevelEditor";

        // ── 카메라 설정 ──────────────────────────────────────────────────
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
        }

        // ── Grid / LevelRoot ─────────────────────────────────────────────
        GameObject gridObj = new GameObject("Grid");
        Grid gridComponent = gridObj.AddComponent<Grid>();
        gridObj.AddComponent<GridLineRenderer>();

        GameObject levelRootObj = new GameObject("LevelRoot");
        levelRootObj.transform.SetParent(gridObj.transform);

        // 타일맵 레이어
        CreateTilemap("Floor_Tilemap",    levelRootObj.transform, sortOrder: 0);
        CreateTilemap("Wall_Tilemap",     levelRootObj.transform, sortOrder: 1);
        CreateTilemap("Overlay_Tilemap",  levelRootObj.transform, sortOrder: 2);

        // GameObject 레이어
        CreateEmptyLayer("Object_Layer",     levelRootObj.transform);
        CreateEmptyLayer("Spotlight_Layer",  levelRootObj.transform);

        // ── LevelEditorManager ───────────────────────────────────────────
        GameObject managerObj = new GameObject("LevelEditorManager");
        LevelEditorManager manager = managerObj.AddComponent<LevelEditorManager>();

        // Ghost preview
        GameObject ghostObj = new GameObject("GhostPreview");
        ghostObj.transform.SetParent(managerObj.transform);
        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sortingOrder = 999;

        // SpotlightMarker 프리팹 생성
        GameObject spotlightPrefab = BuildSpotlightMarkerPrefab();

        // ── 샘플 에셋 생성 ────────────────────────────────────────────────
        EnsureDirectory("Assets/Resources");
        AssetDatabase.DeleteAsset("Assets/Resources/SampleFloorTile.asset");
        AssetDatabase.DeleteAsset("Assets/Resources/SampleFloorItem.asset");

        string spritePath = "Assets/Sprites/Square.png";
        ConfigureSpriteImporter(spritePath);
        Sprite sqSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        Tile sampleTile = ScriptableObject.CreateInstance<Tile>();
        sampleTile.sprite = sqSprite;
        AssetDatabase.CreateAsset(sampleTile, "Assets/Resources/SampleFloorTile.asset");

        LevelItemData sampleItem = ScriptableObject.CreateInstance<LevelItemData>();
        sampleItem.itemName = "샘플 바닥 타일";
        sampleItem.itemType = LevelItemData.ItemType.Tile;
        sampleItem.category = "바닥";
        sampleItem.targetLayerName = "Floor_Tilemap";
        sampleItem.tile = sampleTile;
        sampleItem.thumbnail = sqSprite;
        AssetDatabase.CreateAsset(sampleItem, "Assets/Resources/SampleFloorItem.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // LevelItemData 전체 수집
        string[] guids = AssetDatabase.FindAssets("t:LevelItemData");
        var items = new List<LevelItemData>();
        foreach (string guid in guids)
        {
            var data = AssetDatabase.LoadAssetAtPath<LevelItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null) items.Add(data);
        }

        // Manager 필드 바인딩
        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("grid").objectReferenceValue = gridComponent;
        managerSO.FindProperty("levelRoot").objectReferenceValue = levelRootObj.transform;
        managerSO.FindProperty("ghostPreviewRenderer").objectReferenceValue = ghostSr;
        managerSO.FindProperty("spotlightMarkerPrefab").objectReferenceValue = spotlightPrefab;

        SerializedProperty itemsProp = managerSO.FindProperty("availableItems");
        itemsProp.ClearArray();
        for (int i = 0; i < items.Count; i++)
        {
            itemsProp.InsertArrayElementAtIndex(i);
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
        managerSO.ApplyModifiedProperties();

        // ── EventSystem ──────────────────────────────────────────────────
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ── UI Canvas (Screen Space Camera) ──────────────────────────────
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 1f;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ── PaletteButton Prefab ─────────────────────────────────────────
        EnsureDirectory("Assets/Prefabs");
        GameObject buttonTemplate = BuildPaletteButtonTemplate();
        string prefabPath = "Assets/Prefabs/PaletteButton.prefab";
        GameObject buttonPrefab = PrefabUtility.SaveAsPrefabAsset(buttonTemplate, prefabPath);
        Object.DestroyImmediate(buttonTemplate);

        // ── UI 패널들 생성 ────────────────────────────────────────────────
        // Root panel (전체 영역)
        GameObject rootPanel = CreatePanel("Panel_Root", canvasObj.transform, Color.clear,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        // Top bar
        GameObject topBarPanel = BuildTopBar(rootPanel.transform, out Button saveBtn, out Button clearBtn,
            out Button drawBtn, out Button eraseBtn, out Button spotlightBtn);

        // Left sidebar (타일 팔레트)
        GameObject leftPanel = BuildSidebarPanel(rootPanel.transform, "Panel_Left", isPivotLeft: true,
            headerText: "TILES",
            out RectTransform tileContent, out Transform tileCategoryParent);

        // Right sidebar (오브젝트 팔레트)
        GameObject rightPanel = BuildSidebarPanel(rootPanel.transform, "Panel_Right", isPivotLeft: false,
            headerText: "OBJECTS",
            out RectTransform objectContent, out Transform objectCategoryParent);

        // Spotlight settings panel (플로팅)
        GameObject spotlightPanel = BuildSpotlightPanel(rootPanel.transform,
            out Image colorPreview, out Slider intensitySlider, out Slider rangeSlider,
            out Button[] colorBtns);

        // Mouse follower
        GameObject followerObj = new GameObject("MouseFollower");
        followerObj.transform.SetParent(rootPanel.transform, false);
        RectTransform followerRect = followerObj.AddComponent<RectTransform>();
        followerRect.sizeDelta = new Vector2(40, 40);
        followerRect.anchorMin = new Vector2(0, 1);
        followerRect.anchorMax = new Vector2(0, 1);
        followerRect.pivot = new Vector2(0, 1);
        Image followerImg = followerObj.AddComponent<Image>();
        followerImg.raycastTarget = false;
        followerObj.SetActive(false);

        // ── LevelEditorUI 컴포넌트 바인딩 ────────────────────────────────
        LevelEditorUI editorUI = rootPanel.AddComponent<LevelEditorUI>();
        SerializedObject uiSO = new SerializedObject(editorUI);

        uiSO.FindProperty("rootCanvas").objectReferenceValue = canvas;
        uiSO.FindProperty("tileContentParent").objectReferenceValue = tileContent;
        uiSO.FindProperty("tileCategoryTabParent").objectReferenceValue = tileCategoryParent;
        uiSO.FindProperty("objectContentParent").objectReferenceValue = objectContent;
        uiSO.FindProperty("objectCategoryTabParent").objectReferenceValue = objectCategoryParent;
        uiSO.FindProperty("toolDrawButton").objectReferenceValue = drawBtn;
        uiSO.FindProperty("toolEraseButton").objectReferenceValue = eraseBtn;
        uiSO.FindProperty("toolSpotlightButton").objectReferenceValue = spotlightBtn;
        uiSO.FindProperty("saveButton").objectReferenceValue = saveBtn;
        uiSO.FindProperty("clearButton").objectReferenceValue = clearBtn;
        uiSO.FindProperty("spotlightSettingsPanel").objectReferenceValue = spotlightPanel;
        uiSO.FindProperty("spotlightColorPreview").objectReferenceValue = colorPreview;
        uiSO.FindProperty("intensitySlider").objectReferenceValue = intensitySlider;
        uiSO.FindProperty("rangeSlider").objectReferenceValue = rangeSlider;
        uiSO.FindProperty("mouseFollowerIcon").objectReferenceValue = followerRect;
        uiSO.FindProperty("paletteButtonPrefab").objectReferenceValue = buttonPrefab;

        // spotlightColorButtons 배열
        SerializedProperty colorBtnProp = uiSO.FindProperty("spotlightColorButtons");
        colorBtnProp.ClearArray();
        for (int i = 0; i < colorBtns.Length; i++)
        {
            colorBtnProp.InsertArrayElementAtIndex(i);
            colorBtnProp.GetArrayElementAtIndex(i).objectReferenceValue = colorBtns[i];
        }
        uiSO.ApplyModifiedProperties();

        // ── 씬 저장 ──────────────────────────────────────────────────────
        EnsureDirectory("Assets/Scenes");
        string sceneFilePath = "Assets/Scenes/LevelEditor.unity";
        if (EditorSceneManager.SaveScene(newScene, sceneFilePath))
        {
            Debug.Log($"<color=green><b>[LevelEditor Setup]</b></color> 씬 저장 완료: {sceneFilePath}");
            AddSceneToBuildSettings(sceneFilePath);
        }
        else
        {
            Debug.LogError("레벨 에디터 씬 저장 실패.");
        }
    }

    // ── UI Builders ─────────────────────────────────────────────────────

    private static GameObject BuildTopBar(Transform parent,
        out Button saveBtn, out Button clearBtn,
        out Button drawBtn, out Button eraseBtn, out Button spotlightBtn)
    {
        GameObject bar = CreatePanel("Panel_TopBar", parent,
            new Color(COLOR_DARK, COLOR_DARK, COLOR_DARK, 0.97f),
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -TOP_BAR_HEIGHT), Vector2.zero);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, TOP_BAR_HEIGHT);
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0.5f, 1);
        barRect.offsetMin = new Vector2(0, -TOP_BAR_HEIGHT);
        barRect.offsetMax = new Vector2(0, 0);

        HorizontalLayoutGroup hGroup = bar.AddComponent<HorizontalLayoutGroup>();
        hGroup.padding = new RectOffset(12, 12, 10, 10);
        hGroup.spacing = 8;
        hGroup.childAlignment = TextAnchor.MiddleLeft;
        hGroup.childForceExpandWidth = false;
        hGroup.childForceExpandHeight = true;

        // 도구 버튼 그룹
        drawBtn      = CreateIconButton("Btn_Draw",      "✏ Draw",      bar.transform, new Vector2(88, 44), new Color(0.3f, 0.3f, 0.3f));
        eraseBtn     = CreateIconButton("Btn_Erase",     "⌫ Erase",     bar.transform, new Vector2(88, 44), new Color(0.3f, 0.3f, 0.3f));
        spotlightBtn = CreateIconButton("Btn_Spotlight", "☀ Light",     bar.transform, new Vector2(88, 44), new Color(0.3f, 0.3f, 0.3f));

        // 구분선
        CreateSeparator(bar.transform);

        // 레이어 라벨
        CreateLabel("단축키: Q=그리기  E=지우기  R=스팟라이트  |  WASD=카메라  스크롤=줌", bar.transform, 11, Color.gray);

        // 오른쪽 정렬을 위한 Flexible Space
        CreateFlexibleSpace(bar.transform);

        // 액션 버튼
        clearBtn = CreateIconButton("Btn_Clear", "전체 지우기", bar.transform, new Vector2(100, 44), new Color(0.6f, 0.2f, 0.2f));
        saveBtn  = CreateIconButton("Btn_Save",  "💾 저장",     bar.transform, new Vector2(88, 44),  new Color(0.2f, 0.5f, 0.8f));

        return bar;
    }

    private static GameObject BuildSidebarPanel(Transform parent, string name, bool isPivotLeft,
        string headerText,
        out RectTransform contentParent, out Transform categoryTabParent)
    {
        // 사이드바 루트
        GameObject sidebar = new GameObject(name);
        sidebar.transform.SetParent(parent, false);
        RectTransform sidebarRect = sidebar.AddComponent<RectTransform>();
        Image sidebarBg = sidebar.AddComponent<Image>();
        sidebarBg.color = new Color(COLOR_MID, COLOR_MID, COLOR_MID, 0.97f);

        if (isPivotLeft)
        {
            sidebarRect.anchorMin = new Vector2(0, 0);
            sidebarRect.anchorMax = new Vector2(0, 1);
            sidebarRect.pivot = new Vector2(0, 0.5f);
            sidebarRect.offsetMin = new Vector2(0, 0);
            sidebarRect.offsetMax = new Vector2(SIDEBAR_WIDTH, -TOP_BAR_HEIGHT);
        }
        else
        {
            sidebarRect.anchorMin = new Vector2(1, 0);
            sidebarRect.anchorMax = new Vector2(1, 1);
            sidebarRect.pivot = new Vector2(1, 0.5f);
            sidebarRect.offsetMin = new Vector2(-SIDEBAR_WIDTH, 0);
            sidebarRect.offsetMax = new Vector2(0, -TOP_BAR_HEIGHT);
        }

        VerticalLayoutGroup vGroup = sidebar.AddComponent<VerticalLayoutGroup>();
        vGroup.padding = new RectOffset(0, 0, 0, 0);
        vGroup.spacing = 0;
        vGroup.childForceExpandWidth = true;
        vGroup.childForceExpandHeight = false;

        // 헤더
        GameObject header = new GameObject("Header");
        header.transform.SetParent(sidebar.transform, false);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = PANEL_HEADER_HEIGHT;
        headerLayout.flexibleHeight = 0;
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(COLOR_DARK, COLOR_DARK, COLOR_DARK, 1f);
        CreateCenteredLabel(headerText, header.transform, 15, Color.white);

        // 카테고리 탭 영역
        GameObject tabRow = new GameObject("CategoryTabs");
        tabRow.transform.SetParent(sidebar.transform, false);
        LayoutElement tabLayout = tabRow.AddComponent<LayoutElement>();
        tabLayout.preferredHeight = CATEGORY_TAB_HEIGHT;
        tabLayout.flexibleHeight = 0;
        Image tabRowBg = tabRow.AddComponent<Image>();
        tabRowBg.color = new Color(COLOR_ACCENT, COLOR_ACCENT, COLOR_ACCENT, 1f);

        HorizontalLayoutGroup tabHGroup = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabHGroup.padding = new RectOffset(4, 4, 4, 4);
        tabHGroup.spacing = 4;
        tabHGroup.childForceExpandWidth = true;
        tabHGroup.childForceExpandHeight = true;
        categoryTabParent = tabRow.transform;

        // 스크롤 뷰 (나머지 공간 전부)
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(sidebar.transform, false);
        LayoutElement scrollLayout = scrollViewObj.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        Image scrollBg = scrollViewObj.AddComponent<Image>();
        scrollBg.color = new Color(0.14f, 0.14f, 0.14f, 1f);
        ScrollRect scrollComp = scrollViewObj.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform viewRect = viewportObj.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.sizeDelta = Vector2.zero;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;
        viewportObj.AddComponent<Image>().color = Color.clear;
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content (GridLayout)
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup gridLayout = contentObj.AddComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(8, 8, 8, 8);
        gridLayout.cellSize = new Vector2(96, 96);
        gridLayout.spacing = new Vector2(6, 6);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollComp.viewport = viewRect;
        scrollComp.content = contentRect;
        scrollComp.horizontal = false;
        scrollComp.vertical = true;
        scrollComp.scrollSensitivity = 20f;

        contentParent = contentRect;
        return sidebar;
    }

    private static GameObject BuildSpotlightPanel(Transform parent,
        out Image colorPreview, out Slider intensitySlider, out Slider rangeSlider,
        out Button[] colorButtons)
    {
        // 플로팅 패널 (우측 하단)
        GameObject panel = new GameObject("Panel_SpotlightSettings");
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-SIDEBAR_WIDTH - 12, 12);
        rect.sizeDelta = new Vector2(220, 280);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.10f, 0.12f, 0.97f);

        VerticalLayoutGroup vGroup = panel.AddComponent<VerticalLayoutGroup>();
        vGroup.padding = new RectOffset(12, 12, 12, 12);
        vGroup.spacing = 10;
        vGroup.childForceExpandWidth = true;
        vGroup.childForceExpandHeight = false;

        // 헤더
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panel.transform, false);
        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 28;
        CreateCenteredLabel("☀ SPOTLIGHT SETTINGS", headerObj.transform, 13, new Color(1f, 0.9f, 0.3f));

        // 색상 미리보기
        GameObject colorRow = CreateRowGroup("ColorRow", panel.transform, 40);
        CreateLabel("색상", colorRow.transform, 12, Color.gray);
        GameObject previewObj = new GameObject("ColorPreview");
        previewObj.transform.SetParent(colorRow.transform, false);
        LayoutElement previewLayout = previewObj.AddComponent<LayoutElement>();
        previewLayout.preferredWidth = 40;
        previewLayout.preferredHeight = 28;
        Image previewImg = previewObj.AddComponent<Image>();
        previewImg.color = Color.white;
        colorPreview = previewImg;

        // 색상 프리셋 버튼 행 (6개)
        GameObject presetRow = CreateRowGroup("PresetRow", panel.transform, 36);
        HorizontalLayoutGroup presetHGroup = presetRow.GetComponent<HorizontalLayoutGroup>();
        presetHGroup.spacing = 4;
        Color[] presets = { Color.white, Color.yellow, new Color(1f, 0.6f, 0.1f), Color.cyan, Color.magenta, Color.red };
        var btnList = new List<Button>();
        foreach (Color col in presets)
        {
            GameObject btnObj = new GameObject("ColorBtn_" + ColorUtility.ToHtmlStringRGB(col));
            btnObj.transform.SetParent(presetRow.transform, false);
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 26;
            le.preferredHeight = 26;
            Image img = btnObj.AddComponent<Image>();
            img.color = col;
            Button btn = btnObj.AddComponent<Button>();
            btnList.Add(btn);
        }
        colorButtons = btnList.ToArray();

        // Intensity 슬라이더
        intensitySlider = BuildLabeledSlider("강도 (Intensity)", panel.transform, 0.1f, 5f, 1.5f);

        // Range 슬라이더
        rangeSlider = BuildLabeledSlider("범위 (Range)", panel.transform, 0.5f, 10f, 3f);

        panel.SetActive(false);
        return panel;
    }

    // ── Spotlight Marker Prefab ─────────────────────────────────────────
    private static GameObject BuildSpotlightMarkerPrefab()
    {
        EnsureDirectory("Assets/Prefabs");
        string prefabPath = "Assets/Prefabs/SpotlightMarker.prefab";

        GameObject markerObj = new GameObject("SpotlightMarker");

        // Light 컴포넌트
        Light light = markerObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.white;
        light.intensity = 1.5f;
        light.range = 3f;
        light.renderMode = LightRenderMode.ForcePixel;

        markerObj.AddComponent<TileSpotlightMarker>();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(markerObj, prefabPath);
        Object.DestroyImmediate(markerObj);
        return saved;
    }

    // ── UI Helper methods ───────────────────────────────────────────────

    private static void CreateTilemap(string name, Transform parent, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = sortOrder;
    }

    private static void CreateEmptyLayer(string name, Transform parent)
    {
        new GameObject(name).transform.SetParent(parent);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        if (color != Color.clear)
        {
            Image img = go.AddComponent<Image>();
            img.color = color;
        }
        return go;
    }

    private static Button CreateIconButton(string name, string label, Transform parent,
        Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;
        le.flexibleWidth = 0;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f);
        cb.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f);
        btn.colors = cb;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 13;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btn;
    }

    private static void CreateSeparator(Transform parent)
    {
        GameObject sep = new GameObject("Separator");
        sep.transform.SetParent(parent, false);
        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.preferredWidth = 1;
        le.flexibleHeight = 1;
        Image img = sep.AddComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    }

    private static void CreateFlexibleSpace(Transform parent)
    {
        GameObject space = new GameObject("FlexibleSpace");
        space.transform.SetParent(parent, false);
        LayoutElement le = space.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
    }

    private static void CreateLabel(string text, Transform parent, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Label_" + text.Substring(0, Mathf.Min(8, text.Length)));
        obj.transform.SetParent(parent, false);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleLeft;
    }

    private static void CreateCenteredLabel(string text, Transform parent, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
    }

    private static GameObject CreateRowGroup(string name, Transform parent, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        HorizontalLayoutGroup hGroup = go.AddComponent<HorizontalLayoutGroup>();
        hGroup.spacing = 8;
        hGroup.childAlignment = TextAnchor.MiddleLeft;
        hGroup.childForceExpandWidth = false;
        hGroup.childForceExpandHeight = true;
        return go;
    }

    private static Slider BuildLabeledSlider(string labelText, Transform parent,
        float min, float max, float defaultVal)
    {
        GameObject container = new GameObject("SliderRow_" + labelText);
        container.transform.SetParent(parent, false);
        LayoutElement le = container.AddComponent<LayoutElement>();
        le.preferredHeight = 48;
        VerticalLayoutGroup vGroup = container.AddComponent<VerticalLayoutGroup>();
        vGroup.spacing = 2;
        vGroup.childForceExpandWidth = true;
        vGroup.childForceExpandHeight = false;

        // 레이블
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);
        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 18;
        Text lbl = labelObj.AddComponent<Text>();
        lbl.text = labelText;
        lbl.fontSize = 11;
        lbl.color = Color.gray;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(container.transform, false);
        LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.preferredHeight = 24;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultVal;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-15, 0);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.sizeDelta = new Vector2(10, 0);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.7f, 1f);

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, 0);
        handleAreaRect.anchoredPosition = Vector2.zero;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        if (sliderRect == null) sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        return slider;
    }

    private static GameObject BuildPaletteButtonTemplate()
    {
        GameObject btnObj = new GameObject("PaletteButton");
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(96, 96);

        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.18f, 0.18f);
        Button btn = btnObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.28f, 0.28f, 0.28f);
        cb.pressedColor = new Color(0.12f, 0.12f, 0.12f);
        btn.colors = cb;

        // 아이콘
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.25f);
        iconRect.anchorMax = new Vector2(0.9f, 0.95f);
        iconRect.sizeDelta = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;

        // 이름 텍스트
        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.25f);
        textRect.sizeDelta = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 10;
        txt.color = new Color(0.85f, 0.85f, 0.85f);
        txt.text = "Item";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btnObj;
    }

    private static void ConfigureSpriteImporter(string spritePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        importer.spritePixelsPerUnit = tex != null ? tex.width : 8;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == scenePath) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
