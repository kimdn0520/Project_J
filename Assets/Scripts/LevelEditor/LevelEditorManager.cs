using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class LevelEditorManager : MonoBehaviour
{
    public enum ToolMode { Draw, Erase, Spotlight }

    public static LevelEditorManager Instance { get; private set; }

    [Header("Grid References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Transform levelRoot;

    [Header("Item Datas")]
    [SerializeField] private List<LevelItemData> availableItems;

    [Header("Visual Preview")]
    [SerializeField] private SpriteRenderer ghostPreviewRenderer;

    [Header("Spotlight")]
    [SerializeField] private GameObject spotlightMarkerPrefab;

    [Header("Camera Control (WASD)")]
    [SerializeField] private float cameraSpeed = 10f;
    [SerializeField] private float minCameraSize = 2f;
    [SerializeField] private float maxCameraSize = 15f;
    [SerializeField] private float zoomSpeed = 2f;

    // ── Runtime state ──────────────────────────────────────────────────
    private LevelItemData currentSelectedItem;
    private ToolMode currentToolMode = ToolMode.Draw;

    // Spotlight live settings (synced from UI sliders)
    private Color spotlightColor = Color.white;
    private float spotlightIntensity = 1.5f;
    private float spotlightRange = 3f;

    private Dictionary<string, Tilemap> cachedTilemaps = new Dictionary<string, Tilemap>();
    private Dictionary<string, Transform> cachedGameObjectLayers = new Dictionary<string, Transform>();
    private Dictionary<Vector3Int, GameObject> placedGameObjects = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> placedSpotlights = new Dictionary<Vector3Int, GameObject>();

    // EventSystem raycast reuse
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
    private PointerEventData _pointerEventData;

    // ── Public accessors ───────────────────────────────────────────────
    public List<LevelItemData> AvailableItems => availableItems;
    public LevelItemData CurrentSelectedItem => currentSelectedItem;
    public Transform LevelRoot => levelRoot;
    public ToolMode CurrentToolMode => currentToolMode;

    // ── Unity lifecycle ────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeLayers();
        DisableGhostPreview();
    }

    private void Start()
    {
        _pointerEventData = new PointerEventData(EventSystem.current);
        ScanExistingObjects();
        ScanExistingSpotlights();
    }

    private void Update()
    {
        HandleCameraInput();
        HandleToolShortcuts();

        if (IsPointerOverUI())
        {
            DisableGhostPreview();
            return;
        }

        UpdateGhostPreview();
        HandleInput();
    }

    // ── Tool mode ──────────────────────────────────────────────────────
    public void SetToolMode(ToolMode mode)
    {
        currentToolMode = mode;

        // Erase 모드에서는 선택 아이템 유지하되 고스트는 끔
        if (mode == ToolMode.Erase || mode == ToolMode.Spotlight)
        {
            DisableGhostPreview();
        }
        else if (currentSelectedItem != null)
        {
            SelectItem(currentSelectedItem); // 고스트 재활성화
        }
    }

    public void SetSpotlightSettings(Color color, float intensity, float range)
    {
        spotlightColor = color;
        spotlightIntensity = intensity;
        spotlightRange = range;
    }

    // ── Item selection ─────────────────────────────────────────────────
    public void SelectItem(LevelItemData item)
    {
        currentSelectedItem = item;

        if (currentSelectedItem == null || currentToolMode != ToolMode.Draw)
        {
            DisableGhostPreview();
            return;
        }

        ghostPreviewRenderer.gameObject.SetActive(true);

        if (currentSelectedItem.itemType == LevelItemData.ItemType.Tile)
        {
            if (currentSelectedItem.tile is Tile simpleTile)
                ghostPreviewRenderer.sprite = simpleTile.sprite;
            else
                ghostPreviewRenderer.sprite = currentSelectedItem.thumbnail;
        }
        else
        {
            if (currentSelectedItem.prefab != null)
            {
                var sr = currentSelectedItem.prefab.GetComponentInChildren<SpriteRenderer>();
                ghostPreviewRenderer.sprite = sr != null ? sr.sprite : currentSelectedItem.thumbnail;
            }
        }

        Color c = Color.white;
        c.a = 0.5f;
        ghostPreviewRenderer.color = c;
    }

    // ── Input handling ─────────────────────────────────────────────────
    private void HandleToolShortcuts()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame) SetToolMode(ToolMode.Draw);
        if (Keyboard.current.eKey.wasPressedThisFrame) SetToolMode(ToolMode.Erase);
        if (Keyboard.current.rKey.wasPressedThisFrame) SetToolMode(ToolMode.Spotlight);
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3Int cellPos = grid.WorldToCell(mouseWorldPos);

        if (Mouse.current.leftButton.isPressed)
        {
            switch (currentToolMode)
            {
                case ToolMode.Draw:
                    if (currentSelectedItem != null) PaintItem(cellPos);
                    break;
                case ToolMode.Erase:
                    EraseItemAt(cellPos);
                    break;
                case ToolMode.Spotlight:
                    PlaceSpotlight(cellPos);
                    break;
            }
        }
        else if (Mouse.current.rightButton.isPressed)
        {
            // 우클릭은 항상 삭제
            EraseItemAt(cellPos);
            RemoveSpotlight(cellPos);
        }
    }

    // ── Ghost preview ──────────────────────────────────────────────────
    private void UpdateGhostPreview()
    {
        if (currentSelectedItem == null || currentToolMode != ToolMode.Draw)
        {
            DisableGhostPreview();
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3Int cellPos = grid.WorldToCell(mouseWorldPos);
        Vector3 snappedWorldPos = grid.CellToWorld(cellPos);

        Vector3 offset = currentSelectedItem.itemType == LevelItemData.ItemType.GameObject
            ? currentSelectedItem.pivotOffset
            : new Vector3(grid.cellSize.x / 2f, grid.cellSize.y / 2f, 0);

        ghostPreviewRenderer.transform.position = snappedWorldPos + offset;
        ghostPreviewRenderer.gameObject.SetActive(true);
    }

    private void DisableGhostPreview()
    {
        if (ghostPreviewRenderer != null)
            ghostPreviewRenderer.gameObject.SetActive(false);
    }

    // ── Paint / Erase ──────────────────────────────────────────────────
    private void PaintItem(Vector3Int cellPos)
    {
        if (currentSelectedItem.itemType == LevelItemData.ItemType.Tile)
        {
            Tilemap targetTilemap = GetOrCreateTilemap(currentSelectedItem.targetLayerName);
            targetTilemap?.SetTile(cellPos, currentSelectedItem.tile);
        }
        else
        {
            if (placedGameObjects.ContainsKey(cellPos)) return;

            Transform targetParent = GetOrCreateGameObjectLayer(currentSelectedItem.targetLayerName);
            if (currentSelectedItem.prefab != null)
            {
                Vector3 spawnPos = grid.CellToWorld(cellPos) + currentSelectedItem.pivotOffset;
                GameObject spawnedObj = Instantiate(currentSelectedItem.prefab, spawnPos, Quaternion.identity, targetParent);
                placedGameObjects.Add(cellPos, spawnedObj);
            }
        }
    }

    private void EraseItemAt(Vector3Int cellPos)
    {
        // 모든 타일맵에서 해당 셀을 지움
        foreach (var tm in cachedTilemaps.Values)
            tm.SetTile(cellPos, null);

        // GameObject가 있으면 삭제
        if (placedGameObjects.TryGetValue(cellPos, out GameObject obj))
        {
            if (obj != null) Destroy(obj);
            placedGameObjects.Remove(cellPos);
        }
    }

    // ── Spotlight ──────────────────────────────────────────────────────
    private void PlaceSpotlight(Vector3Int cellPos)
    {
        if (placedSpotlights.ContainsKey(cellPos)) return;
        if (spotlightMarkerPrefab == null) return;

        Transform spotlightLayer = GetOrCreateGameObjectLayer("Spotlight_Layer");
        Vector3 spawnPos = grid.CellToWorld(cellPos) + new Vector3(grid.cellSize.x / 2f, grid.cellSize.y / 2f, -1f);
        GameObject marker = Instantiate(spotlightMarkerPrefab, spawnPos, Quaternion.identity, spotlightLayer);

        TileSpotlightMarker tsm = marker.GetComponent<TileSpotlightMarker>();
        tsm?.Apply(spotlightColor, spotlightIntensity, spotlightRange);

        placedSpotlights.Add(cellPos, marker);
    }

    private void RemoveSpotlight(Vector3Int cellPos)
    {
        if (placedSpotlights.TryGetValue(cellPos, out GameObject marker))
        {
            if (marker != null) Destroy(marker);
            placedSpotlights.Remove(cellPos);
        }
    }

    // ── ClearAll ───────────────────────────────────────────────────────
    public void ClearAll()
    {
        foreach (var tm in cachedTilemaps.Values)
            tm.ClearAllTiles();

        foreach (var obj in placedGameObjects.Values)
            if (obj != null) Destroy(obj);
        placedGameObjects.Clear();

        foreach (var s in placedSpotlights.Values)
            if (s != null) Destroy(s);
        placedSpotlights.Clear();

        foreach (var layerPair in cachedGameObjectLayers)
        {
            Transform parent = layerPair.Value;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }

    // ── Layer management ───────────────────────────────────────────────
    private void InitializeLayers()
    {
        if (levelRoot == null) { Debug.LogError("Level Root가 설정되지 않았습니다!"); return; }

        foreach (var tm in levelRoot.GetComponentsInChildren<Tilemap>(true))
            if (!cachedTilemaps.ContainsKey(tm.name))
                cachedTilemaps.Add(tm.name, tm);

        foreach (Transform child in levelRoot)
            if (child.GetComponent<Tilemap>() == null && !cachedGameObjectLayers.ContainsKey(child.name))
                cachedGameObjectLayers.Add(child.name, child);
    }

    private void ScanExistingObjects()
    {
        foreach (var layerPair in cachedGameObjectLayers)
        {
            foreach (Transform child in layerPair.Value)
            {
                Vector3Int cellPos = grid.WorldToCell(child.position);
                if (!placedGameObjects.ContainsKey(cellPos))
                    placedGameObjects.Add(cellPos, child.gameObject);
            }
        }
    }

    private void ScanExistingSpotlights()
    {
        if (!cachedGameObjectLayers.TryGetValue("Spotlight_Layer", out Transform spotlightLayer)) return;
        foreach (Transform child in spotlightLayer)
        {
            Vector3Int cellPos = grid.WorldToCell(child.position);
            if (!placedSpotlights.ContainsKey(cellPos))
                placedSpotlights.Add(cellPos, child.gameObject);
        }
    }

    private Tilemap GetOrCreateTilemap(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) layerName = "Default_Tilemap";
        if (cachedTilemaps.TryGetValue(layerName, out Tilemap tm)) return tm;

        GameObject go = new GameObject(layerName);
        go.transform.SetParent(levelRoot);
        Tilemap newTm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        cachedTilemaps.Add(layerName, newTm);
        return newTm;
    }

    private Transform GetOrCreateGameObjectLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) layerName = "Default_Objects";
        if (cachedGameObjectLayers.TryGetValue(layerName, out Transform t)) return t;

        GameObject go = new GameObject(layerName);
        go.transform.SetParent(levelRoot);
        cachedGameObjectLayers.Add(layerName, go.transform);
        return go.transform;
    }

    // ── Utilities ──────────────────────────────────────────────────────
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Mouse.current == null) return false;
        _pointerEventData.position = Mouse.current.position.ReadValue();
        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _uiRaycastResults);
        return _uiRaycastResults.Count > 0;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (Mouse.current == null || Camera.main == null) return Vector3.zero;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        mousePos.z = 10f;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private void HandleCameraInput()
    {
        if (Keyboard.current == null || Camera.main == null) return;

        Vector3 moveDir = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) moveDir.y += 1f;
        if (Keyboard.current.sKey.isPressed) moveDir.y -= 1f;
        if (Keyboard.current.aKey.isPressed) moveDir.x -= 1f;
        if (Keyboard.current.dKey.isPressed) moveDir.x += 1f;
        Camera.main.transform.Translate(moveDir.normalized * cameraSpeed * Time.deltaTime, Space.World);

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                Camera.main.orthographicSize = Mathf.Clamp(
                    Camera.main.orthographicSize - (scroll * 0.005f * zoomSpeed),
                    minCameraSize, maxCameraSize);
            }
        }
    }
}
