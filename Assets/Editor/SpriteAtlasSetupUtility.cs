using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace EditorUtilities
{
    [InitializeOnLoad]
    public static class SpriteAtlasSetupUtility
    {
        static SpriteAtlasSetupUtility()
        {
            EditorApplication.delayCall += () =>
            {
                SetupSpriteAtlas(false);
            };
        }

        [MenuItem("Tools/Setup Sprite Atlas and SpriteManager")]
        public static void SetupManual()
        {
            SetupSpriteAtlas(true);
        }

        public static void SetupSpriteAtlas(bool showLog)
        {
            // 1. Create SpriteAtlas asset if missing
            string atlasPath = "Assets/Textures/UI/UI_SpriteAtlas.spriteatlas";
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                
                SpriteAtlasPackingSettings packSettings = new SpriteAtlasPackingSettings
                {
                    blockOffset = 1,
                    enableRotation = false,
                    enableTightPacking = false,
                    padding = 4
                };
                atlas.SetPackingSettings(packSettings);

                SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings
                {
                    readable = false,
                    generateMipMaps = false,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear
                };
                atlas.SetTextureSettings(textureSettings);

                Object uiFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Textures/UI");
                if (uiFolder != null)
                {
                    atlas.Add(new Object[] { uiFolder });
                }

                AssetDatabase.CreateAsset(atlas, atlasPath);
                if (showLog) Debug.Log($"[SpriteAtlasSetup] Created SpriteAtlas at: {atlasPath}");
            }

            // 2. Create SpriteAtlasSO asset in Assets/Resources/SpriteAtlasSO.asset
            string resourceDir = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourceDir))
            {
                Directory.CreateDirectory(resourceDir);
                AssetDatabase.Refresh();
            }

            string soPath = "Assets/Resources/SpriteAtlasSO.asset";
            SpriteAtlasSO spriteAtlasSO = AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(soPath);

            if (spriteAtlasSO == null)
            {
                spriteAtlasSO = ScriptableObject.CreateInstance<SpriteAtlasSO>();
                AssetDatabase.CreateAsset(spriteAtlasSO, soPath);
            }

            // Assign atlas to SO using SerializedObject
            SerializedObject serializedSO = new SerializedObject(spriteAtlasSO);
            SerializedProperty atlasesProp = serializedSO.FindProperty("atlases");
            if (atlasesProp != null)
            {
                atlasesProp.arraySize = 1;
                atlasesProp.GetArrayElementAtIndex(0).objectReferenceValue = atlas;
                serializedSO.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(spriteAtlasSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showLog) Debug.Log($"[SpriteAtlasSetup] Configured SpriteAtlasSO at: {soPath}");
        }
    }
}
