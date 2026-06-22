using UnityEngine;
using UnityEditor;
using Framework.PuzzleSystem;
using UnityEditor.SceneManagement;

public class PuzzleSetupEditor
{
    [MenuItem("Puzzle/Setup Game")]
    public static void Setup()
    {
        // 1. Create a simple white sprite if it doesn't exist
        string spritePath = "Assets/Sprites/Square.png";
        if (!System.IO.Directory.Exists("Assets/Sprites"))
            System.IO.Directory.CreateDirectory("Assets/Sprites");
            
        if (!System.IO.File.Exists(spritePath))
        {
            Texture2D tex = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(spritePath, bytes);
            AssetDatabase.ImportAsset(spritePath);
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        string prefabPath = "Assets/Prefabs/PuzzleTile.prefab";
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs");

        GameObject tileObj = new GameObject("PuzzleTile");
        var sr = tileObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        tileObj.AddComponent<PuzzleTile>();
        
        PrefabUtility.SaveAsPrefabAsset(tileObj, prefabPath);
        Object.DestroyImmediate(tileObj);

        // 2. Create Scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // 3. Setup Controller
        GameObject controllerObj = new GameObject("PuzzleController");
        var controller = controllerObj.AddComponent<PuzzleController>();
        
        var prefab = AssetDatabase.LoadAssetAtPath<PuzzleTile>(prefabPath);
        
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_tilePrefab").objectReferenceValue = prefab;
        so.FindProperty("_gridParent").objectReferenceValue = new GameObject("GridParent").transform;
        so.ApplyModifiedProperties();

        // 4. Adjust Camera
        Camera.main.transform.position = new Vector3(2.5f, 3.5f, -10);
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 5;
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.15f); // Dark blue background
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // 5. Save Scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PuzzleGame.unity");
    }
}
