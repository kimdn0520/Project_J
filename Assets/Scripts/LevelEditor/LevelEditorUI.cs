using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LevelEditorUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Tile Panel")]
    [SerializeField] private RectTransform tileContentParent;
    [SerializeField] private Transform tileCategoryTabParent;

    [Header("Object Panel")]
    [SerializeField] private RectTransform objectContentParent;
    [SerializeField] private Transform objectCategoryTabParent;

    [Header("Tool Buttons")]
    [SerializeField] private Button toolDrawButton;
    [SerializeField] private Button toolEraseButton;
    [SerializeField] private Button toolSpotlightButton;

    [Header("Action Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearButton;

    [Header("Spotlight Settings Panel")]
    [SerializeField] private GameObject spotlightSettingsPanel;
    [SerializeField] private Image spotlightColorPreview;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private Slider rangeSlider;
    [SerializeField] private Button[] spotlightColorButtons; // preset color buttons

    [Header("Mouse Follower")]
    [SerializeField] private RectTransform mouseFollowerIcon;

    // ── Runtime state ───────────────────────────────────────────────────
    [SerializeField] private GameObject paletteButtonPrefab;

    private List<LevelItemData> tileItems = new List<LevelItemData>();
    private List<LevelItemData> objectItems = new List<LevelItemData>();

    private List<Image> tileBtnBgs = new List<Image>();
    private List<Image> objectBtnBgs = new List<Image>();

    private int selectedTileIndex = -1;
    private int selectedObjectIndex = -1;

    private string activeTileCategory = "전체";
    private string activeObjectCategory = "전체";

    private Color currentSpotlightColor = Color.white;
    private static readonly Color[] PresetColors = {
        Color.white, Color.yellow, new Color(1f,0.6f,0.1f), Color.cyan, Color.magenta, Color.red
    };

    // ── Unity lifecycle ─────────────────────────────────────────────────
    private void Start()
    {
        CollectItems();
        BuildCategoryTabs(tileCategoryTabParent, tileItems, isTile: true);
        BuildCategoryTabs(objectCategoryTabParent, objectItems, isTile: false);
        RefreshTilePalette();
        RefreshObjectPalette();
        BindActionButtons();
        BindToolButtons();
        BindSpotlightPanel();
        SetSpotlightPanelVisible(false);
    }

    private void Update()
    {
        UpdateMouseFollower();
    }

    // ── Item collection ─────────────────────────────────────────────────
    private void CollectItems()
    {
        if (LevelEditorManager.Instance == null) return;
        var all = LevelEditorManager.Instance.AvailableItems;
        if (all == null) return;

        tileItems.Clear();
        objectItems.Clear();
        foreach (var item in all)
        {
            if (item.itemType == LevelItemData.ItemType.Tile) tileItems.Add(item);
            else objectItems.Add(item);
        }
    }

    // ── Category tabs ───────────────────────────────────────────────────
    private void BuildCategoryTabs(Transform tabParent, List<LevelItemData> source, bool isTile)
    {
        if (tabParent == null) return;
        foreach (Transform child in tabParent) Destroy(child.gameObject);

        var categories = new List<string> { "전체" };
        foreach (var item in source)
            if (!string.IsNullOrEmpty(item.category) && !categories.Contains(item.category))
                categories.Add(item.category);

        foreach (string cat in categories)
        {
            string capturedCat = cat;
            GameObject tabObj = new GameObject(cat + "_Tab");
            tabObj.transform.SetParent(tabParent, false);

            RectTransform rt = tabObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 32);

            Image bg = tabObj.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            Button btn = tabObj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            Text txt = textObj.AddComponent<Text>();
            txt.text = capturedCat;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (isTile)
            {
                btn.onClick.AddListener(() =>
                {
                    activeTileCategory = capturedCat;
                    selectedTileIndex = -1;
                    LevelEditorManager.Instance?.SelectItem(null);
                    RefreshTilePalette();
                    HighlightCategoryTab(tabParent, capturedCat);
                });
            }
            else
            {
                btn.onClick.AddListener(() =>
                {
                    activeObjectCategory = capturedCat;
                    selectedObjectIndex = -1;
                    LevelEditorManager.Instance?.SelectItem(null);
                    RefreshObjectPalette();
                    HighlightCategoryTab(tabParent, capturedCat);
                });
            }
        }

        HighlightCategoryTab(tabParent, "전체");
    }

    private void HighlightCategoryTab(Transform tabParent, string activeCategory)
    {
        foreach (Transform tab in tabParent)
        {
            Image bg = tab.GetComponent<Image>();
            if (bg == null) continue;
            bool isActive = tab.name == activeCategory + "_Tab";
            bg.color = isActive ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
        }
    }

    // ── Palette builders ────────────────────────────────────────────────
    private void RefreshTilePalette()
    {
        BuildPalette(tileContentParent, tileItems, activeTileCategory, tileBtnBgs,
            ref selectedTileIndex, isForTiles: true);
    }

    private void RefreshObjectPalette()
    {
        BuildPalette(objectContentParent, objectItems, activeObjectCategory, objectBtnBgs,
            ref selectedObjectIndex, isForTiles: false);
    }

    private void BuildPalette(RectTransform contentParent, List<LevelItemData> source,
        string categoryFilter, List<Image> btnBgList, ref int selectedIndex, bool isForTiles)
    {
        if (contentParent == null || paletteButtonPrefab == null) return;

        foreach (Transform child in contentParent) Destroy(child.gameObject);
        btnBgList.Clear();
        selectedIndex = -1;

        var filtered = categoryFilter == "전체"
            ? source
            : source.FindAll(i => i.category == categoryFilter);

        for (int i = 0; i < filtered.Count; i++)
        {
            LevelItemData item = filtered[i];
            GameObject btnObj = Instantiate(paletteButtonPrefab, contentParent);

            Image iconImage = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null) iconImage.sprite = item.thumbnail;

            Text nameText = btnObj.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null) nameText.text = item.itemName;

            Image bgImage = btnObj.GetComponent<Image>();
            if (bgImage != null) btnBgList.Add(bgImage);

            Button btn = btnObj.GetComponent<Button>();
            int capturedIndex = i;
            bool capturedIsForTiles = isForTiles;
            btn?.onClick.AddListener(() => OnPaletteItemSelected(capturedIndex, filtered, btnBgList, capturedIsForTiles));
        }
    }

    private void OnPaletteItemSelected(int index, List<LevelItemData> filtered,
        List<Image> btnBgList, bool isForTiles)
    {
        if (index < 0 || index >= filtered.Count) return;

        if (isForTiles)
        {
            if (selectedTileIndex == index)
            {
                selectedTileIndex = -1;
                LevelEditorManager.Instance?.SelectItem(null);
            }
            else
            {
                selectedTileIndex = index;
                selectedObjectIndex = -1;
                LevelEditorManager.Instance?.SelectItem(filtered[index]);
                ClearObjectSelection();
            }
            ApplySelectionHighlight(btnBgList, selectedTileIndex);
        }
        else
        {
            if (selectedObjectIndex == index)
            {
                selectedObjectIndex = -1;
                LevelEditorManager.Instance?.SelectItem(null);
            }
            else
            {
                selectedObjectIndex = index;
                selectedTileIndex = -1;
                LevelEditorManager.Instance?.SelectItem(filtered[index]);
                ClearTileSelection();
            }
            ApplySelectionHighlight(btnBgList, selectedObjectIndex);
        }

        // 아이템을 선택하면 Draw 모드로 전환
        if (LevelEditorManager.Instance != null &&
            LevelEditorManager.Instance.CurrentToolMode != LevelEditorManager.ToolMode.Draw)
        {
            LevelEditorManager.Instance.SetToolMode(LevelEditorManager.ToolMode.Draw);
            UpdateToolButtonHighlight(LevelEditorManager.ToolMode.Draw);
        }
    }

    private void ApplySelectionHighlight(List<Image> bgList, int selectedIdx)
    {
        for (int i = 0; i < bgList.Count; i++)
        {
            if (bgList[i] == null) continue;
            bgList[i].color = (i == selectedIdx)
                ? new Color(0.3f, 0.8f, 0.3f, 1f)
                : new Color(0.18f, 0.18f, 0.18f, 1f);
        }
    }

    private void ClearTileSelection()
    {
        selectedTileIndex = -1;
        ApplySelectionHighlight(tileBtnBgs, -1);
    }

    private void ClearObjectSelection()
    {
        selectedObjectIndex = -1;
        ApplySelectionHighlight(objectBtnBgs, -1);
    }

    // ── Tool buttons ────────────────────────────────────────────────────
    private void BindToolButtons()
    {
        toolDrawButton?.onClick.AddListener(() =>
        {
            LevelEditorManager.Instance?.SetToolMode(LevelEditorManager.ToolMode.Draw);
            UpdateToolButtonHighlight(LevelEditorManager.ToolMode.Draw);
            SetSpotlightPanelVisible(false);
        });

        toolEraseButton?.onClick.AddListener(() =>
        {
            LevelEditorManager.Instance?.SetToolMode(LevelEditorManager.ToolMode.Erase);
            UpdateToolButtonHighlight(LevelEditorManager.ToolMode.Erase);
            SetSpotlightPanelVisible(false);
        });

        toolSpotlightButton?.onClick.AddListener(() =>
        {
            LevelEditorManager.Instance?.SetToolMode(LevelEditorManager.ToolMode.Spotlight);
            UpdateToolButtonHighlight(LevelEditorManager.ToolMode.Spotlight);
            SetSpotlightPanelVisible(true);
        });

        UpdateToolButtonHighlight(LevelEditorManager.ToolMode.Draw);
    }

    private void UpdateToolButtonHighlight(LevelEditorManager.ToolMode mode)
    {
        SetButtonHighlight(toolDrawButton, mode == LevelEditorManager.ToolMode.Draw);
        SetButtonHighlight(toolEraseButton, mode == LevelEditorManager.ToolMode.Erase);
        SetButtonHighlight(toolSpotlightButton, mode == LevelEditorManager.ToolMode.Spotlight);
    }

    private void SetButtonHighlight(Button btn, bool active)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
    }

    // ── Spotlight panel ─────────────────────────────────────────────────
    private void BindSpotlightPanel()
    {
        if (intensitySlider != null)
        {
            intensitySlider.minValue = 0.1f;
            intensitySlider.maxValue = 5f;
            intensitySlider.value = 1.5f;
            intensitySlider.onValueChanged.AddListener(_ => PushSpotlightSettings());
        }

        if (rangeSlider != null)
        {
            rangeSlider.minValue = 0.5f;
            rangeSlider.maxValue = 10f;
            rangeSlider.value = 3f;
            rangeSlider.onValueChanged.AddListener(_ => PushSpotlightSettings());
        }

        // 프리셋 색상 버튼 바인딩
        if (spotlightColorButtons != null)
        {
            for (int i = 0; i < spotlightColorButtons.Length && i < PresetColors.Length; i++)
            {
                int idx = i;
                Color col = PresetColors[i];
                Image btnImg = spotlightColorButtons[i]?.GetComponent<Image>();
                if (btnImg != null) btnImg.color = col;
                spotlightColorButtons[i]?.onClick.AddListener(() =>
                {
                    currentSpotlightColor = PresetColors[idx];
                    if (spotlightColorPreview != null) spotlightColorPreview.color = currentSpotlightColor;
                    PushSpotlightSettings();
                });
            }
        }

        currentSpotlightColor = Color.white;
        if (spotlightColorPreview != null) spotlightColorPreview.color = currentSpotlightColor;
    }

    private void PushSpotlightSettings()
    {
        float intensity = intensitySlider != null ? intensitySlider.value : 1.5f;
        float range = rangeSlider != null ? rangeSlider.value : 3f;
        LevelEditorManager.Instance?.SetSpotlightSettings(currentSpotlightColor, intensity, range);
    }

    private void SetSpotlightPanelVisible(bool visible)
    {
        if (spotlightSettingsPanel != null)
            spotlightSettingsPanel.SetActive(visible);
    }

    // ── Action buttons ──────────────────────────────────────────────────
    private void BindActionButtons()
    {
        saveButton?.onClick.AddListener(() =>
        {
            if (LevelEditorManager.Instance != null)
                LevelSaveLoad.SaveLevelRootAsPrefab(LevelEditorManager.Instance.LevelRoot);
        });

        clearButton?.onClick.AddListener(() =>
        {
            LevelEditorManager.Instance?.ClearAll();
        });
    }

    // ── Mouse follower ──────────────────────────────────────────────────
    private void UpdateMouseFollower()
    {
        if (mouseFollowerIcon == null) return;

        bool hasItem = LevelEditorManager.Instance != null &&
                       LevelEditorManager.Instance.CurrentSelectedItem != null &&
                       LevelEditorManager.Instance.CurrentToolMode == LevelEditorManager.ToolMode.Draw;

        if (!hasItem)
        {
            mouseFollowerIcon.gameObject.SetActive(false);
            return;
        }

        LevelItemData selected = LevelEditorManager.Instance.CurrentSelectedItem;
        Image img = mouseFollowerIcon.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = selected.thumbnail;
            img.enabled = selected.thumbnail != null;
        }

        mouseFollowerIcon.gameObject.SetActive(true);

        if (Mouse.current != null && rootCanvas != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.GetComponent<RectTransform>(),
                mousePos,
                rootCanvas.worldCamera,
                out Vector2 localPos);
            mouseFollowerIcon.localPosition = localPos + new Vector2(24f, -24f);
        }
    }
}
