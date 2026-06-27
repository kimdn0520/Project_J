using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct SoundEntry
{
    public string key;
    public AudioClip clip;
}

/// <summary>
/// ScriptableObject to hold BGM and SFX registries.
/// Allows designers to manage all audio assets in one central data file.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrarySO : ScriptableObject
{
    [Header("Background Music (BGM)")]
    [SerializeField] private List<SoundEntry> bgmList = new List<SoundEntry>();

    [Header("Sound Effects (SFX)")]
    [SerializeField] private List<SoundEntry> sfxList = new List<SoundEntry>();

    public List<SoundEntry> BgmList => bgmList;
    public List<SoundEntry> SfxList => sfxList;
}
