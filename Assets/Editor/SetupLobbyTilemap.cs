using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class SetupLobbyTilemap
{
    public static void Execute()
    {
        // 0. Clean up Grid object in current active scene if it's not Map_01_Lobby
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Map_01_Lobby")
        {
            GameObject wrongGrid = GameObject.Find("Grid");
            if (wrongGrid != null)
            {
                Object.DestroyImmediate(wrongGrid);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[SetupLobbyTilemap] Removed Grid from non-lobby scene: {activeScene.name}");
            }
        }

        // 1. Open target Map_01_Lobby scene
        string lobbyScenePath = "Assets/Scenes/Maps/Map_01_Lobby.unity";
        if (!Directory.Exists("Assets/Scenes/Maps"))
        {
            Directory.CreateDirectory("Assets/Scenes/Maps");
        }

        Scene lobbyScene;
        if (File.Exists(lobbyScenePath))
        {
            lobbyScene = EditorSceneManager.OpenScene(lobbyScenePath, OpenSceneMode.Single);
        }
        else
        {
            lobbyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(lobbyScene, lobbyScenePath);
        }

        // 2. Create Tile Textures (Grey Floor & Dark Wall)
        string textureDir = "Assets/Textures/Tiles";
        if (!Directory.Exists(textureDir))
        {
            Directory.CreateDirectory(textureDir);
        }

        Sprite floorSprite = CreateSolidSprite(textureDir + "/tile_floor_grey.png", new Color32(110, 110, 115, 255));
        Sprite wallSprite = CreateSolidSprite(textureDir + "/tile_wall_dark.png", new Color32(30, 30, 35, 255));

        // 3. Create Tile Assets
        string tileDir = "Assets/Tiles";
        if (!Directory.Exists(tileDir))
        {
            Directory.CreateDirectory(tileDir);
        }

        Tile floorTile = CreateOrLoadTile(tileDir + "/FloorTile.asset", floorSprite);
        Tile wallTile = CreateOrLoadTile(tileDir + "/WallTile.asset", wallSprite);

        // 4. Setup Grid & Tilemaps in Map_01_Lobby scene
        GameObject gridGo = GameObject.Find("Grid");
        if (gridGo == null)
        {
            gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
        }

        // Remove Tilemap_AbovePlayer if it exists
        Transform abovePlayerChild = gridGo.transform.Find("Tilemap_AbovePlayer");
        if (abovePlayerChild != null)
        {
            Object.DestroyImmediate(abovePlayerChild.gameObject);
        }

        Tilemap floorMap = GetOrCreateTilemap(gridGo, "Tilemap_Floor", 0);
        Tilemap decorMap = GetOrCreateTilemap(gridGo, "Tilemap_FloorDecor", 1);
        Tilemap wallMap = GetOrCreateTilemap(gridGo, "Tilemap_Walls", 10, true);

        // Clear existing tiles
        floorMap.ClearAllTiles();
        decorMap.ClearAllTiles();
        wallMap.ClearAllTiles();

        // 5. Build 1st Floor Lobby (Room Size: 18 x 14 tiles)
        int minX = -9;
        int maxX = 8;
        int minY = -7;
        int maxY = 6;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                // Outer border walls
                if (x == minX || x == maxX || y == minY || y == maxY)
                {
                    wallMap.SetTile(pos, wallTile);
                }
                else
                {
                    // Floor tile
                    floorMap.SetTile(pos, floorTile);
                }
            }
        }

        // 6. Fix FrontDesk and DeskMemo Sorting Orders in scene
        GameObject[] rootObjects = lobbyScene.GetRootGameObjects();
        foreach (var rootObj in rootObjects)
        {
            FixObjectSortingRecursive(rootObj.transform);
        }

        EditorSceneManager.MarkSceneDirty(lobbyScene);
        EditorSceneManager.SaveScene(lobbyScene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SetupLobbyTilemap] Saved Map_01_Lobby with corrected object sorting orders to {lobbyScenePath}");
    }

    private static void FixObjectSortingRecursive(Transform trans)
    {
        string name = trans.name.ToLower();
        SpriteRenderer sr = trans.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            if (name.Contains("desk") || name.Contains("frontdesk") || name.Contains("counter"))
            {
                sr.sortingOrder = 15;
                Debug.Log($"[SetupLobbyTilemap] Set {trans.name} SpriteRenderer sortingOrder = 15");
            }
            else if (name.Contains("memo") || name.Contains("deskmemo") || name.Contains("note"))
            {
                sr.sortingOrder = 16;
                Debug.Log($"[SetupLobbyTilemap] Set {trans.name} SpriteRenderer sortingOrder = 16");
            }
        }

        foreach (Transform child in trans)
        {
            FixObjectSortingRecursive(child);
        }
    }

    private static Sprite CreateSolidSprite(string path, Color32 color)
    {
        if (!File.Exists(path))
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Tile CreateOrLoadTile(string path, Sprite sprite)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, path);
        }
        else
        {
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
        }
        return tile;
    }

    private static Tilemap GetOrCreateTilemap(GameObject parentGrid, string name, int sortingOrder, bool addCollider = false)
    {
        Transform child = parentGrid.transform.Find(name);
        GameObject mapGo;
        if (child == null)
        {
            mapGo = new GameObject(name);
            mapGo.transform.SetParent(parentGrid.transform);
        }
        else
        {
            mapGo = child.gameObject;
        }

        Tilemap tilemap = mapGo.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = mapGo.AddComponent<Tilemap>();

        TilemapRenderer renderer = mapGo.GetComponent<TilemapRenderer>();
        if (renderer == null) renderer = mapGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (addCollider)
        {
            TilemapCollider2D col = mapGo.GetComponent<TilemapCollider2D>();
            if (col == null) mapGo.AddComponent<TilemapCollider2D>();

            CompositeCollider2D compCol = mapGo.GetComponent<CompositeCollider2D>();
            if (compCol == null)
            {
                compCol = mapGo.AddComponent<CompositeCollider2D>();
                mapGo.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                col = mapGo.GetComponent<TilemapCollider2D>();
                col.usedByComposite = true;
            }
        }

        return tilemap;
    }
}
