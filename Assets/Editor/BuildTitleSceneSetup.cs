#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UI;
using System.IO;
using System.Collections.Generic;

namespace EditorTools
{
    public static class BuildTitleSceneSetup
    {
        [MenuItem("Tools/Build Title Scene")]
        public static void Execute()
        {
            Debug.Log("=== Starting Build Title Scene ===");

            string titleScenePath = "Assets/Scenes/Title.unity";
            Scene titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Get Default UI sprites
            Sprite defaultBgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite defaultUISprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            
            // Load NanumGothicBold SDF font
            TMP_FontAsset nanumGothicFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NanumGothicBold SDF.asset");
            if (nanumGothicFont == null)
            {
                nanumGothicFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            // 1. Create Main Camera
            GameObject mainCamObj = new GameObject("Main Camera");
            mainCamObj.tag = "MainCamera";
            mainCamObj.transform.position = new Vector3(0, 0, -10);
            
            Camera mainCam = mainCamObj.AddComponent<Camera>();
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f); // Deep midnight black-blue
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 1000f;

            mainCamObj.AddComponent<AudioListener>();

            // 2. Background Visual
            GameObject bgObj = new GameObject("Title_Background");
            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = defaultBgSprite;
            bgSr.color = new Color(0.05f, 0.06f, 0.08f, 1f);
            bgObj.transform.localScale = new Vector3(25f, 15f, 1f);

            // 3. EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            // 4. Title Canvas (ScreenSpaceCamera)
            GameObject canvasObj = new GameObject("TitleCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            TitleUI titleUI = canvasObj.AddComponent<TitleUI>();

            // 5. Main Panel Container
            GameObject panelObj = new GameObject("TitlePanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            // Main Title Text ("RAPPORT HOTEL")
            GameObject mainTitleObj = new GameObject("MainTitleText");
            mainTitleObj.transform.SetParent(panelObj.transform, false);
            RectTransform titleRt = mainTitleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector3(0, 100, 0);
            titleRt.sizeDelta = new Vector2(1400, 200);

            TextMeshProUGUI mainTitleTmp = mainTitleObj.AddComponent<TextMeshProUGUI>();
            if (nanumGothicFont != null) mainTitleTmp.font = nanumGothicFont;
            mainTitleTmp.text = "RAPPORT HOTEL";
            mainTitleTmp.fontSize = 92;
            mainTitleTmp.alignment = TextAlignmentOptions.Center;
            mainTitleTmp.color = new Color(0.85f, 0.18f, 0.18f, 1f); // Deep Crimson Red
            mainTitleTmp.fontStyle = FontStyles.Bold;
            mainTitleTmp.characterSpacing = 8f;

            // Decorative Line Divider
            GameObject lineObj = new GameObject("TitleDividerLine");
            lineObj.transform.SetParent(panelObj.transform, false);
            RectTransform lineRt = lineObj.AddComponent<RectTransform>();
            lineRt.anchoredPosition = new Vector3(0, -10, 0);
            lineRt.sizeDelta = new Vector2(520, 2);
            Image lineImg = lineObj.AddComponent<Image>();
            lineImg.color = new Color(0.75f, 0.2f, 0.2f, 0.6f);

            // 6. Start Game Button
            GameObject btnObj = new GameObject("StartGameButton");
            btnObj.transform.SetParent(panelObj.transform, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector3(0, -140, 0);
            btnRt.sizeDelta = new Vector2(360, 80);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = defaultUISprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.12f, 0.1f, 0.1f, 0.95f);

            Button btnComp = btnObj.AddComponent<Button>();
            ColorBlock cb = btnComp.colors;
            cb.normalColor = new Color(0.16f, 0.12f, 0.12f, 0.95f);
            cb.highlightedColor = new Color(0.45f, 0.12f, 0.12f, 1f);
            cb.pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
            cb.selectedColor = cb.highlightedColor;
            btnComp.colors = cb;

            // Button Text ("게 임 시 작")
            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRt = btnTextObj.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
            if (nanumGothicFont != null) btnTmp.font = nanumGothicFont;
            btnTmp.text = "게 임 시 작";
            btnTmp.fontSize = 32;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = new Color(0.95f, 0.95f, 0.9f, 1f);

            // 7. Title Local Fade Overlay Canvas (Sorting order 9999 over Title Screen)
            GameObject fadeCanvasObj = new GameObject("TitleFadeCanvas");
            Canvas fadeCanvas = fadeCanvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999;

            fadeCanvasObj.AddComponent<CanvasScaler>();
            fadeCanvasObj.AddComponent<GraphicRaycaster>();
            CanvasGroup titleFadeCg = fadeCanvasObj.AddComponent<CanvasGroup>();
            titleFadeCg.alpha = 0f;
            titleFadeCg.blocksRaycasts = false;

            GameObject fadeImgObj = new GameObject("FadeImage");
            fadeImgObj.transform.SetParent(fadeCanvasObj.transform, false);
            Image fadeImg = fadeImgObj.AddComponent<Image>();
            fadeImg.color = Color.black;
            RectTransform fadeImgRt = fadeImg.rectTransform;
            fadeImgRt.anchorMin = Vector2.zero;
            fadeImgRt.anchorMax = Vector2.one;
            fadeImgRt.sizeDelta = Vector2.zero;

            // Connect references to TitleUI via SerializedObject
            SerializedObject titleUiSO = new SerializedObject(titleUI);
            titleUiSO.FindProperty("startGameButton").objectReferenceValue = btnComp;
            titleUiSO.FindProperty("titleText").objectReferenceValue = mainTitleTmp;
            titleUiSO.FindProperty("titleCanvasGroup").objectReferenceValue = canvasGroup;
            titleUiSO.FindProperty("titleFadeCanvasGroup").objectReferenceValue = titleFadeCg;
            titleUiSO.FindProperty("startSceneName").stringValue = "Map_00_HotelExterior";
            titleUiSO.FindProperty("startSpawnId").stringValue = "Spawn_Default";
            titleUiSO.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(titleScene, titleScenePath);
            Debug.Log($"[Setup] Saved Title Scene with local fade overlay at {titleScenePath}");

            // 8. Update Build Settings
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            buildScenes.Add(new EditorBuildSettingsScene(titleScenePath, true));
            buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Persistent.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_00_HotelExterior.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_01_Lobby.unity", true));
            if (File.Exists("Assets/Scenes/Maps/Map_01_Start.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_01_Start.unity", true));
            }
            if (File.Exists("Assets/Scenes/Maps/Map_02_Corridor.unity"))
            {
                buildScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Maps/Map_02_Corridor.unity", true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.Refresh();
            Debug.Log("=== Build Title Scene Completed Successfully! ===");
        }
    }
}
#endif
