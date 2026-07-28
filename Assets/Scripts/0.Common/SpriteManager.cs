using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpriteManager : SingletonMonoBehaviour<SpriteManager>
{
    [SerializeField]
    private SpriteAtlasSO spriteAtlasData;

    private Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    public void Initialize()
    {
        _spriteDic.Clear();

        if (spriteAtlasData == null)
        {
            spriteAtlasData = Resources.Load<SpriteAtlasSO>("SpriteAtlasSO");
        }

        if (spriteAtlasData != null && spriteAtlasData.Atlases != null)
        {
            foreach (SpriteAtlas atlas in spriteAtlasData.Atlases)
            {
                if (atlas == null) continue;

                Sprite[] sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);

                foreach (Sprite sprite in sprites)
                {
                    if (sprite == null) continue;
                    string cleanedName = sprite.name.Replace("(Clone)", "");

                    if (!_spriteDic.ContainsKey(cleanedName))
                    {
                        _spriteDic.Add(cleanedName, sprite);
                    }
                }
            }
        }
    }

    public Sprite Get(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        // 1. Try exact dictionary match
        if (_spriteDic.TryGetValue(spriteName, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        // 2. Partial / Alias matching (e.g. "enju_portrait_400x400" vs "이은주_400x400_배경제거_0")
        foreach (var kvp in _spriteDic)
        {
            if (kvp.Key.Contains(spriteName) || spriteName.Contains(kvp.Key) ||
                (spriteName.ToLower().Contains("enju") && (kvp.Key.Contains("이은주") || kvp.Key.ToLower().Contains("enju"))))
            {
                return kvp.Value;
            }
        }

#if UNITY_EDITOR
        // 3. Fallback in Editor: Find sprite directly in Project Assets if Atlas is not packed yet
        string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
        if (guids.Length == 0 && (spriteName.Contains("enju") || spriteName.Contains("이은주")))
        {
            guids = AssetDatabase.FindAssets("이은주 t:Sprite");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("enju t:Sprite");
            }
        }

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loadedSprite != null)
            {
                _spriteDic[spriteName] = loadedSprite;
                return loadedSprite;
            }
        }
#endif

        // 4. Fallback in Resources
        Sprite resSprite = Resources.Load<Sprite>($"Textures/UI/{spriteName}");
        if (resSprite == null) resSprite = Resources.Load<Sprite>(spriteName);
        if (resSprite != null)
        {
            _spriteDic[spriteName] = resSprite;
            return resSprite;
        }

        Debug.LogWarning($"[SpriteManager] Sprite '{spriteName}' not found in Atlas or Resources.");
        return null;
    }
}