using UnityEditor;
using UnityEngine;
using Framework.PuzzleSystem;

public class SetupIceTilePrefab
{
    public static void Execute()
    {
        string prefabPath = "Assets/Prefabs/IceTile.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // 기존 PuzzleTile 제거 (IceTile로 교체하기 위함)
        var oldComp = prefabRoot.GetComponent<PuzzleTile>();
        if (oldComp != null && oldComp.GetType() == typeof(PuzzleTile))
        {
            Object.DestroyImmediate(oldComp, true);
        }

        // IceTile 추가 (이미 있으면 무시)
        if (prefabRoot.GetComponent<IceTile>() == null)
        {
            prefabRoot.AddComponent<IceTile>();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        // PuzzleController에 연결
        PuzzleController controller = Object.FindAnyObjectByType<PuzzleController>();
        if (controller != null)
        {
            GameObject icePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            // SerializedObject를 사용하여 프라이빗 필드 할당
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_iceTilePrefab").objectReferenceValue = icePrefab;
            so.ApplyModifiedProperties();
            
            Debug.Log("IceTile Prefab setup and assigned to PuzzleController successfully!");
        }
    }
}
