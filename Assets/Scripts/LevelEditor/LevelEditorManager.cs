using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class LevelEditorManager : MonoBehaviour
{
    public static LevelEditorManager Instance { get; private set; }

    [Header("Grid References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Transform levelRoot; // 프리팹으로 구워질 최상위 오브젝트

    [Header("Item Datas")]
    [SerializeField] private List<LevelItemData> availableItems;

    [Header("Visual Preview")]
    [SerializeField] private SpriteRenderer ghostPreviewRenderer;

    [Header("Camera Control (WASD)")]
    [SerializeField] private float cameraSpeed = 10f;
    [SerializeField] private float minCameraSize = 2f;
    [SerializeField] private float maxCameraSize = 15f;
    [SerializeField] private float zoomSpeed = 2f;

    private LevelItemData currentSelectedItem;
    private Dictionary<string, Tilemap> cachedTilemaps = new Dictionary<string, Tilemap>();
    private Dictionary<string, Transform> cachedGameObjectLayers = new Dictionary<string, Transform>();
    
    // 특정 좌표(Cell)에 배치된 GameObject들을 추적 (중복 스폰 방지 및 우클릭 삭제용)
    private Dictionary<Vector3Int, GameObject> placedGameObjects = new Dictionary<Vector3Int, GameObject>();

    public List<LevelItemData> AvailableItems => availableItems;
    public LevelItemData CurrentSelectedItem => currentSelectedItem;
    public Transform LevelRoot => levelRoot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeLayers();
        DisableGhostPreview();
    }

    private void Start()
    {
        // 씬 로드 시 이미 배치되어 있는 GameObject들을 스캔해서 placedGameObjects에 등록 (수정/보완용)
        ScanExistingObjects();
    }

    private void Update()
    {
        // 키보드 WASD 및 마우스 휠 카메라 조작
        HandleCameraInput();

        // 씬 내의 UI 캔버스 가림막으로 인한 입력 씹힘 버그 방지
        // 마우스가 UI 영역(하단 팔레트 26% 또는 우측 상단 버튼 영역)에 있으면 씬 입력을 완전히 차단
        if (Mouse.current != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            
            // 1. 하단 팔레트 영역 (높이 26% 이하)
            bool isOverBottomPalette = mouseScreenPos.y < Screen.height * 0.26f;
            
            // 2. 우측 상단 버튼 영역 (오른쪽 450px 이내 & 위쪽 110px 이내)
            bool isOverTopRightButtons = mouseScreenPos.x > Screen.width - 450f && mouseScreenPos.y > Screen.height - 110f;

            if (isOverBottomPalette || isOverTopRightButtons)
            {
                DisableGhostPreview();
                return;
            }
        }

        UpdateGhostPreview();
        HandleInput();
    }

    private void HandleCameraInput()
    {
        if (Keyboard.current == null) return;
        if (Camera.main == null) return;

        Vector3 moveDir = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) moveDir.y += 1f;
        if (Keyboard.current.sKey.isPressed) moveDir.y -= 1f;
        if (Keyboard.current.aKey.isPressed) moveDir.x -= 1f;
        if (Keyboard.current.dKey.isPressed) moveDir.x += 1f;

        // 카메라 월드 공간 수평 이동
        Camera.main.transform.Translate(moveDir.normalized * cameraSpeed * Time.deltaTime, Space.World);

        // 마우스 휠 줌 제어
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                Camera.main.orthographicSize = Mathf.Clamp(
                    Camera.main.orthographicSize - (scroll * 0.005f * zoomSpeed),
                    minCameraSize,
                    maxCameraSize
                );
            }
        }
    }

    private void InitializeLayers()
    {
        if (levelRoot == null)
        {
            Debug.LogError("Level Root가 설정되지 않았습니다!");
            return;
        }

        // LevelRoot 하위의 모든 Tilemap 캐싱
        Tilemap[] tilemaps = levelRoot.GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in tilemaps)
        {
            if (!cachedTilemaps.ContainsKey(tm.name))
            {
                cachedTilemaps.Add(tm.name, tm);
            }
        }

        // LevelRoot 하위의 다른 레이어들(Transform) 캐싱 (타일맵이 아닌 일반 GameObject 부모 역할)
        foreach (Transform child in levelRoot)
        {
            if (child.GetComponent<Tilemap>() == null)
            {
                if (!cachedGameObjectLayers.ContainsKey(child.name))
                {
                    cachedGameObjectLayers.Add(child.name, child);
                }
            }
        }
    }

    private void ScanExistingObjects()
    {
        // GameObject 레이어 아래에 있는 것들을 스캔하여 그리드 좌표에 등록
        foreach (var layerPair in cachedGameObjectLayers)
        {
            Transform layerParent = layerPair.Value;
            foreach (Transform child in layerParent)
            {
                Vector3Int cellPos = grid.WorldToCell(child.position);
                // 이미 존재한다면 중복된 것일 수 있으나 일단 첫번째 것을 등록
                if (!placedGameObjects.ContainsKey(cellPos))
                {
                    placedGameObjects.Add(cellPos, child.gameObject);
                }
            }
        }
    }

    public void SelectItem(LevelItemData item)
    {
        currentSelectedItem = item;
        if (currentSelectedItem != null)
        {
            ghostPreviewRenderer.gameObject.SetActive(true);
            
            // 고스트 썸네일/스프라이트 설정
            if (currentSelectedItem.itemType == LevelItemData.ItemType.Tile)
            {
                // 타일의 스프라이트 가져오기
                if (currentSelectedItem.tile is Tile simpleTile)
                {
                    ghostPreviewRenderer.sprite = simpleTile.sprite;
                }
                else
                {
                    ghostPreviewRenderer.sprite = currentSelectedItem.thumbnail;
                }
            }
            else
            {
                // GameObject 프리팹의 SpriteRenderer가 있다면 가져옴
                if (currentSelectedItem.prefab != null)
                {
                    var sr = currentSelectedItem.prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        ghostPreviewRenderer.sprite = sr.sprite;
                    }
                    else
                    {
                        ghostPreviewRenderer.sprite = currentSelectedItem.thumbnail;
                    }
                }
            }
            
            // 반투명하게 설정
            Color color = Color.white;
            color.a = 0.5f;
            ghostPreviewRenderer.color = color;
        }
        else
        {
            DisableGhostPreview();
        }
    }

    private void UpdateGhostPreview()
    {
        if (currentSelectedItem == null)
        {
            DisableGhostPreview();
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3Int cellPos = grid.WorldToCell(mouseWorldPos);
        Vector3 snappedWorldPos = grid.CellToWorld(cellPos);

        // 스냅 시 오프셋 적용
        Vector3 offset = Vector3.zero;
        if (currentSelectedItem.itemType == LevelItemData.ItemType.GameObject)
        {
            // 발밑(Bottom Pivot) 스냅을 위해 피벗 오프셋 더하기
            offset = currentSelectedItem.pivotOffset;
        }
        else
        {
            // 타일맵 스냅: 2D Grid CellToWorld는 기본적으로 셀의 왼쪽 아래를 반환하므로 중심 맞추기용 오프셋 적용 가능
            offset = new Vector3(grid.cellSize.x / 2f, grid.cellSize.y / 2f, 0);
        }

        ghostPreviewRenderer.transform.position = snappedWorldPos + offset;
    }

    private void DisableGhostPreview()
    {
        if (ghostPreviewRenderer != null)
        {
            ghostPreviewRenderer.gameObject.SetActive(false);
        }
    }

    private void HandleInput()
    {
        if (currentSelectedItem == null) return;
        if (Mouse.current == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3Int cellPos = grid.WorldToCell(mouseWorldPos);

        // 마우스 좌클릭 드래그: 페인팅
        if (Mouse.current.leftButton.isPressed)
        {
            PaintItem(cellPos);
        }
        // 마우스 우클릭 드래그: 삭제
        else if (Mouse.current.rightButton.isPressed)
        {
            EraseItem(cellPos);
        }
    }

    private void PaintItem(Vector3Int cellPos)
    {
        if (currentSelectedItem.itemType == LevelItemData.ItemType.Tile)
        {
            // 타일맵 라우팅
            Tilemap targetTilemap = GetOrCreateTilemap(currentSelectedItem.targetLayerName);
            if (targetTilemap != null)
            {
                targetTilemap.SetTile(cellPos, currentSelectedItem.tile);
            }
        }
        else
        {
            // GameObject 배치
            // 중복 배치 방지 체크
            if (placedGameObjects.ContainsKey(cellPos)) return;

            Transform targetParent = GetOrCreateGameObjectLayer(currentSelectedItem.targetLayerName);
            if (currentSelectedItem.prefab != null)
            {
                Vector3 spawnPos = grid.CellToWorld(cellPos) + currentSelectedItem.pivotOffset;
                GameObject spawnedObj = Instantiate(currentSelectedItem.prefab, spawnPos, Quaternion.identity, targetParent);
                
                // 좌표 등록
                placedGameObjects.Add(cellPos, spawnedObj);
            }
        }
    }

    private void EraseItem(Vector3Int cellPos)
    {
        if (currentSelectedItem.itemType == LevelItemData.ItemType.Tile)
        {
            // 타일맵 삭제: 선택된 브러시의 대상 레이어에서 지우기
            Tilemap targetTilemap = GetOrCreateTilemap(currentSelectedItem.targetLayerName);
            if (targetTilemap != null)
            {
                targetTilemap.SetTile(cellPos, null);
            }
        }
        else
        {
            // GameObject 삭제
            if (placedGameObjects.TryGetValue(cellPos, out GameObject objToDestroy))
            {
                if (objToDestroy != null)
                {
                    Destroy(objToDestroy);
                }
                placedGameObjects.Remove(cellPos);
            }
        }
    }

    public void ClearAll()
    {
        // 1. 모든 타일맵 클리어
        foreach (var tm in cachedTilemaps.Values)
        {
            tm.ClearAllTiles();
        }

        // 2. 모든 GameObject 클리어
        foreach (var obj in placedGameObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        placedGameObjects.Clear();

        // 씬에서 혹시 캐싱되지 않고 남아있는 자식들도 모두 날림
        foreach (var layerPair in cachedGameObjectLayers)
        {
            Transform parent = layerPair.Value;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }

    private Tilemap GetOrCreateTilemap(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) layerName = "Default_Tilemap";

        if (cachedTilemaps.TryGetValue(layerName, out Tilemap tm))
        {
            return tm;
        }

        // 없을 경우 동적 생성 및 캐싱
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

        if (cachedGameObjectLayers.TryGetValue(layerName, out Transform t))
        {
            return t;
        }

        // 없을 경우 동적 생성 및 캐싱
        GameObject go = new GameObject(layerName);
        go.transform.SetParent(levelRoot);
        
        cachedGameObjectLayers.Add(layerName, go.transform);
        return go.transform;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (Mouse.current == null) return Vector3.zero;
        if (Camera.main == null) return Vector3.zero;

        Vector3 mousePos = Mouse.current.position.ReadValue();
        
        // Z = 0 평면(바닥 평면) 정의 (2D/2.5D 게임용 Z축 법선)
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        
        // 레이캐스트 실패 시 기본 화면 백업 연산
        mousePos.z = 10f;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
