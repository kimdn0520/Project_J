using UnityEngine;
using UnityEngine.U2D;

[CreateAssetMenu(fileName = "SpriteAtlasSO", menuName = "Scriptable Objects/SpriteAtlasSO")]
public class SpriteAtlasSO : ScriptableObject
{
    [SerializeField]
    private SpriteAtlas[] atlases;

    public SpriteAtlas[] Atlases => atlases;
}