using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewLevelItemData", menuName = "Level Editor/Item Data")]
public class LevelItemData : ScriptableObject
{
    public enum ItemType { Tile, GameObject }

    [Header("Basic Info")]
    public string itemName;
    public Sprite thumbnail;
    public ItemType itemType;

    [Header("Grouping")]
    [Tooltip("팔레트 탭 분류용 카테고리 (예: 바닥, 벽, 특수 / 소품, 적, 조명)")]
    public string category = "기본";

    [Header("Placement Target")]
    [Tooltip("배치될 대상 타일맵 또는 부모 오브젝트의 이름 (예: Floor_Tilemap, Object_Layer)")]
    public string targetLayerName;

    [Header("Tile Settings")]
    public TileBase tile;

    [Header("GameObject Settings")]
    public GameObject prefab;
    [Tooltip("발밑 정렬 등 배치 시 적용할 미세 위치 오프셋")]
    public Vector3 pivotOffset;
}
