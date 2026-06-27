using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Professional, extensible SoundManager for BGM and SFX.
/// Caches BGM and SFX separately from a SoundLibrarySO asset, support crossfading,
/// manual/AudioMixer scaling, and PlayerPrefs saving/loading.
/// </summary>
public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    private const string MasterVolumeKey = "Settings_MasterVolume";
    private const string BgmVolumeKey = "Settings_BgmVolume";
    private const string SfxVolumeKey = "Settings_SfxVolume";

    [Header("BGM Channels")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    [Header("Audio Mixer (Optional)")]
    [Tooltip("If assigned, volumes will be controlled through the AudioMixer parameters instead of individual AudioSources.")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParamName = "MasterVolume";
    [SerializeField] private string bgmParamName = "BGMVolume";
    [SerializeField] private string sfxParamName = "SFXVolume";

    [Header("Sound Registry Asset")]
    [SerializeField] private SoundLibrarySO soundLibrary;

    [Header("SFX Pool Settings")]
    [SerializeField] private int sfxPoolSize = 10;

    [Header("Volume Defaults")]
    [Range(0f, 1f)] [SerializeField] private float defaultMaster = 1.0f;
    [Range(0f, 1f)] [SerializeField] private float defaultBgm = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float defaultSfx = 0.8f;

    public float MasterVolume { get; private set; }
    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private AudioSource activeBgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private float[] sfxVolumeScales;
    private int currentSfxIndex = 0;

    // Distinct caches to prevent key collision between a BGM and an SFX with the same name
    private Dictionary<string, AudioClip> bgmCache = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

    protected override void Awake()
    {
        base.Awake();

        // 1. Load sound database from ScriptableObject
        InitializeRegistryCaches();

        // 2. Initialize BGM AudioSources
        if (bgmSourceA == null) bgmSourceA = CreateAudioSource("BGMSource_A", true);
        if (bgmSourceB == null) bgmSourceB = CreateAudioSource("BGMSource_B", true);
        activeBgmSource = bgmSourceA;

        // 3. Initialize SFX Pool
        sfxVolumeScales = new float[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = CreateAudioSource($"SFXSource_{i}", false);
            sfxSources.Add(src);
            sfxVolumeScales[i] = 1.0f;
        }

        // 4. Load Saved Volume Settings
        LoadVolumeSettings();
    }

    private void InitializeRegistryCaches()
    {
        bgmCache.Clear();
        sfxCache.Clear();

        if (soundLibrary == null)
        {
            Debug.LogWarning("[SoundManager] SoundLibrarySO asset is not assigned in the inspector.");
            return;
        }

        // Cache BGMs
        foreach (var entry in soundLibrary.BgmList)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
            {
                bgmCache[entry.key.ToLower()] = entry.clip;
            }
        }

        // Cache SFXs
        foreach (var entry in soundLibrary.SfxList)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
            {
                sfxCache[entry.key.ToLower()] = entry.clip;
            }
        }
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform);
        AudioSource src = obj.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D by default
        return src;
    }

    #region Volume Control Public API

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.Save();
        ApplyVolumeSettings();
    }

    public void SetBGMVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        PlayerPrefs.Save();
        ApplyVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
        ApplyVolumeSettings();
    }

    private void LoadVolumeSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMaster);
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, defaultBgm);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfx);
        ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        if (audioMixer != null)
        {
            SetMixerVolume(masterParamName, MasterVolume);
            SetMixerVolume(bgmParamName, BgmVolume);
            SetMixerVolume(sfxParamName, SfxVolume);

            if (bgmSourceA != null) bgmSourceA.volume = 1.0f;
            if (bgmSourceB != null) bgmSourceB.volume = 1.0f;
            for (int i = 0; i < sfxSources.Count; i++)
            {
                if (sfxSources[i] != null) sfxSources[i].volume = sfxVolumeScales[i];
            }
        }
        else
        {
            float targetBgmVol = MasterVolume * BgmVolume;
            if (bgmSourceA != null) bgmSourceA.volume = targetBgmVol;
            if (bgmSourceB != null) bgmSourceB.volume = targetBgmVol;

            for (int i = 0; i < sfxSources.Count; i++)
            {
                if (sfxSources[i] != null)
                {
                    sfxSources[i].volume = MasterVolume * SfxVolume * sfxVolumeScales[i];
                }
            }
        }
    }

    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        float dB = linearVolume > 0.0001f ? Mathf.Log10(linearVolume) * 20f : -80f;
        audioMixer.SetFloat(parameterName, dB);
    }

    #endregion

    #region BGM Playback API

    public void PlayBGM(AudioClip clip, float fadeDuration = 1.0f, bool loop = true)
    {
        if (clip == null)
        {
            StopBGM(fadeDuration);
            return;
        }

        if (activeBgmSource.clip == clip && activeBgmSource.isPlaying) return;

        AudioSource targetSource = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        AudioSource fadingSource = activeBgmSource;

        activeBgmSource = targetSource;

        fadingSource.DOKill();
        targetSource.DOKill();

        targetSource.clip = clip;
        targetSource.loop = loop;
        targetSource.volume = 0f;
        targetSource.Play();

        float targetVolume = (audioMixer != null) ? 1.0f : (MasterVolume * BgmVolume);

        if (fadeDuration > 0f)
        {
            fadingSource.DOFade(0f, fadeDuration).OnComplete(() => fadingSource.Stop());
            targetSource.DOFade(targetVolume, fadeDuration);
        }
        else
        {
            fadingSource.Stop();
            targetSource.volume = targetVolume;
        }
    }

    public void PlayBGM(string soundKey, float fadeDuration = 1.0f, bool loop = true)
    {
        AudioClip clip = GetAudioClip(soundKey, true);
        if (clip != null)
        {
            PlayBGM(clip, fadeDuration, loop);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] BGM clip not found for key: {soundKey}");
        }
    }

    public void StopBGM(float fadeDuration = 1.0f)
    {
        activeBgmSource.DOKill();
        if (fadeDuration > 0f && activeBgmSource.isPlaying)
        {
            activeBgmSource.DOFade(0f, fadeDuration).OnComplete(() => activeBgmSource.Stop());
        }
        else
        {
            activeBgmSource.Stop();
        }
    }

    #endregion

    #region SFX Playback API

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
    {
        if (clip == null) return;

        AudioSource src = GetAvailableSFXSource();
        src.transform.position = Vector3.zero;
        src.spatialBlend = 0f; // 2D

        int index = sfxSources.IndexOf(src);
        sfxVolumeScales[index] = volumeScale;

        src.clip = clip;
        src.volume = (audioMixer != null) ? volumeScale : (MasterVolume * SfxVolume * volumeScale);
        src.pitch = pitch;
        src.Play();
    }

    public void PlaySFX(string soundKey, float volumeScale = 1.0f, float pitch = 1.0f)
    {
        AudioClip clip = GetAudioClip(soundKey, false);
        if (clip != null)
        {
            PlaySFX(clip, volumeScale, pitch);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX clip not found for key: {soundKey}");
        }
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1.0f, float pitch = 1.0f)
    {
        if (clip == null) return;

        AudioSource src = GetAvailableSFXSource();
        src.transform.position = position;
        src.spatialBlend = 1f; // 3D

        int index = sfxSources.IndexOf(src);
        sfxVolumeScales[index] = volumeScale;

        src.clip = clip;
        src.volume = (audioMixer != null) ? volumeScale : (MasterVolume * SfxVolume * volumeScale);
        src.pitch = pitch;
        src.Play();
    }

    public void PlaySFXAtPosition(string soundKey, Vector3 position, float volumeScale = 1.0f, float pitch = 1.0f)
    {
        AudioClip clip = GetAudioClip(soundKey, false);
        if (clip != null)
        {
            PlaySFXAtPosition(clip, position, volumeScale, pitch);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] SFX clip not found for key: {soundKey}");
        }
    }

    #endregion

    private AudioSource GetAvailableSFXSource()
    {
        AudioSource src = sfxSources[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxSources.Count;

        if (src.isPlaying)
        {
            src.Stop();
        }

        return src;
    }

    private AudioClip GetAudioClip(string soundKey, bool isBgm)
    {
        if (string.IsNullOrEmpty(soundKey)) return null;

        string searchKey = soundKey.ToLower();
        var targetCache = isBgm ? bgmCache : sfxCache;

        // 1. Check cache
        if (targetCache.TryGetValue(searchKey, out AudioClip clip))
        {
            return clip;
        }

        // 2. Try loading from Resources dynamically (Resources/Sound/BGM or Resources/Sound/SFX)
        string subFolder = isBgm ? "BGM" : "SFX";
        clip = Resources.Load<AudioClip>($"Sound/{subFolder}/{soundKey}");
        if (clip == null)
        {
            clip = Resources.Load<AudioClip>($"Sound/{soundKey}");
        }

        // Cache loaded clip
        if (clip != null)
        {
            targetCache[searchKey] = clip;
        }

        return clip;
    }
}
