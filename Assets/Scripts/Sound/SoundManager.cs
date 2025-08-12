using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This helper class bundles a SoundType with a list of clips.
[System.Serializable]
public class SoundGroup
{
    public SoundType soundType;
    public AudioClip[] clips;
}

public enum SoundType
{
    AbilitySFX,
    BackgroundMusic,
    ButtonClick,
    DiceRoll,
    DiceCollide,
    Win,
    Lose
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [Header("Theme Music")]
    [SerializeField] private AudioClip mainThemeMusic;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Sound Effect Groups")]
    [SerializeField] private SoundGroup[] soundGroups;

    private Dictionary<SoundType, AudioClip[]> soundDictionary;
    private static SoundManager instance;
    private AudioSource sfxSource;

    // CHANGED: Renamed for clarity. This is for the main theme.
    private AudioSource bgmSource;
    // NEW: A second music source just for short, situational tracks.
    private AudioSource stingerSource;

    private void Awake()
    {
        // CHANGED: Upgraded Singleton pattern to be persistent
        if (instance != null && instance != this)
        {
            // If another SoundManager exists, destroy this new one.
            Destroy(gameObject);
            return;
        }
        instance = this;

        // NEW: This line prevents the SoundManager from being destroyed on scene load.
        DontDestroyOnLoad(gameObject);

        // Populate the dictionary (unchanged)
        soundDictionary = new Dictionary<SoundType, AudioClip[]>();
        foreach (SoundGroup group in soundGroups)
        {
            soundDictionary[group.soundType] = group.clips;
        }
    }

    void Start()
    {
        sfxSource = GetComponent<AudioSource>();

        // Configure the two music sources
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        stingerSource = gameObject.AddComponent<AudioSource>();
        stingerSource.loop = true;
        stingerSource.playOnAwake = false;

        // Automatically play the main theme on the BGM source
        if (mainThemeMusic != null)
        {
            bgmSource.clip = mainThemeMusic;
            bgmSource.volume = musicVolume;
            bgmSource.Play();
        }
    }

    /// <summary>
    /// Plays a RANDOM sound from the specified sound group.
    /// </summary>
    public static void PlaySound(SoundType sound, float volume = 1.0f)
    {
        if (instance.soundDictionary.TryGetValue(sound, out AudioClip[] clips))
        {
            if (clips.Length > 0)
            {
                // Pick a random clip from the array
                AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
                instance.sfxSource.PlayOneShot(clipToPlay, volume);
            }
        }
    }

    /// <summary>
    /// Plays a SPECIFIC sound from the specified sound group by its index.
    /// </summary>
    public static void PlaySound(SoundType sound, int index, float volume = 1.0f)
    {
        if (instance.soundDictionary.TryGetValue(sound, out AudioClip[] clips))
        {
            if (index >= 0 && index < clips.Length)
            {
                // Pick the specified clip from the array
                AudioClip clipToPlay = clips[index];
                instance.sfxSource.PlayOneShot(clipToPlay, volume);
            }
        }
    }


    public static void PlayMusicStinger(SoundType music, float volume)
    {
        if (instance.soundDictionary.TryGetValue(music, out AudioClip[] clips))
        {
            if (clips.Length > 0)
            {
                instance.stingerSource.clip = clips[0];
                instance.stingerSource.volume = volume;
                instance.stingerSource.Play();
            }
        }
    }

    // RENAMED: This now specifically stops the stinger source.
    public static void StopMusicStinger()
    {
        instance.stingerSource.Stop();
    }

    public static void SetMusicMuted(bool isMuted)
    {
        if (instance == null) return;

        instance.bgmSource.mute = isMuted;
        instance.stingerSource.mute = isMuted;
    }
}