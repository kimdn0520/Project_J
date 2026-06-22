using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteManager : SingletonMonoBehaviour<SpriteManager>
{
    [SerializeField]
    private SpriteAtlasSO spriteAtlasData;

    private Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();

    protected override void Awake()
    {
        base.Awake();
    }

    public void Initialize()
    {
        if (spriteAtlasData == null)
        {
            Debug.LogError("SpriteManager에 SpriteAtlasSO가 할당되지 않았습니다!");
            return;
        }

        // 등록된 모든 아틀라스를 순회
        foreach (SpriteAtlas atlas in spriteAtlasData.Atlases)
        {
            // 각 아틀라스에 포함된 모든 스프라이트를 가져옴.
            Sprite[] sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);

            foreach (Sprite sprite in sprites)
            {
                // 스프라이트 이름에서 "(Clone)" 접미사를 제거.
                string cleanedName = sprite.name.Replace("(Clone)", "");

                // Dictionary에 이미 같은 이름의 키가 있는지 확인.
                if (_spriteDic.ContainsKey(cleanedName))
                {
                    continue;
                }

                _spriteDic.Add(cleanedName, sprite);
            }
        }
    }

    public Sprite Get(string spriteName)
    {
        if (_spriteDic.TryGetValue(spriteName, out Sprite sprite))
        {
            return sprite;
        }
        else
        {
            Debug.LogError($"'${spriteName}' 이름의 스프라이트를 찾을 수 없습니다. 아틀라스에 등록되어 있는지 확인해주세요.");
            return null;
        }
    }
}