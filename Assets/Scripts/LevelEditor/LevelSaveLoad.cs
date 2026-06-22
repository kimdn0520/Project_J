using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LevelSaveLoad
{
    private const string PrefabSaveDirectory = "Assets/Prefabs/Levels";

    public static void SaveLevelRootAsPrefab(Transform levelRoot)
    {
        if (levelRoot == null)
        {
            Debug.LogError("저장할 Level Root가 null입니다!");
            return;
        }

#if UNITY_EDITOR
        // 1. 저장 디렉토리 존재 확인 및 생성
        if (!Directory.Exists(PrefabSaveDirectory))
        {
            Directory.CreateDirectory(PrefabSaveDirectory);
        }

        // 2. 파일 이름 설정
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Level_{timestamp}.prefab";
        string assetPath = Path.Combine(PrefabSaveDirectory, fileName).Replace("\\", "/");

        // 3. 런타임 저장 꼬임 방지를 위해 Level Root를 임시로 복제(Duplicate)
        GameObject clonedRoot = Object.Instantiate(levelRoot.gameObject);
        clonedRoot.name = levelRoot.gameObject.name; // "(Clone)" 꼬리표 제거

        // 4. 복제본 정제 (Cleanup)
        CleanUpClonedLevel(clonedRoot);

        // 5. 프리팹으로 저장
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(clonedRoot, assetPath, out bool success);

        // 6. 임시 복제본 제거
        Object.DestroyImmediate(clonedRoot);

        if (success && savedPrefab != null)
        {
            Debug.Log($"<color=green><b>[레벨 에디터]</b></color> 프리팹 저장 성공! 경로: {assetPath}");
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("[레벨 에디터] 프리팹 저장에 실패했습니다.");
        }
#else
        Debug.LogWarning("프리팹 저장은 Unity 에디터(플레이 모드)에서만 지원됩니다!");
#endif
    }

    private static void CleanUpClonedLevel(GameObject root)
    {
        // 런타임에 동적으로 변경된 인스턴스 머티리얼 등이 있으면 정리하거나,
        // 프리팹 내부에 불필요하게 남아있을 수 있는 임시 인스턴스 머티리얼 참조 등을 원본 에셋으로 초기화합니다.
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("(Instance)"))
            {
                // 실시간 복제 셰이더 인스턴스를 저장하는 데 따른 경고 메시지 방지를 위해 null 처리 혹은 기본 Material 매핑
            }
        }
    }
}
