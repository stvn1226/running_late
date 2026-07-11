using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public enum SoundType
    {
        Jump,
        Stagger,
        Bounce,
        Powerup,
        PowerupSFX,
        CDNumbers,
        CDGo,
        MusicMenu,
        MusicLevel,
        Footsteps,
        OptionSelect,
        OptionChange,
        OptionReturn,
        PauseOpen,
        MusicVictory,
        MusicDefeat,
        MusicInvincibility
    }

    [System.Serializable]
    public class Sound
    {
        public AudioClip Clip;

        [Range(0f, 1f)]
        public float Volume = 1f;
        
        public bool IsMusic = false;
    }

    [System.Serializable]
    public class SoundCollection
    {
        public SoundType Type;
        public List<Sound> Sounds;
    }

    public static AudioManager Instance;

    [Header("Audio Settings")]
    //public Sound[] AllSounds;
    public SoundCollection[] AllCollections;
    
    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    public const float InvincibilityLayerBaseVolume = 0.01f;
    public const float InvincibilityLayerActiveVolume = 0.8f;

    private Dictionary<SoundType, SoundCollection> _soundDictionary = new Dictionary<SoundType, SoundCollection>();
    private AudioSource _musicSource;
    private AudioSource _invincibilityMusicSource;
    private bool _levelMusicPrewarmed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var collection in AllCollections)
        {
            if (collection.Sounds != null && collection.Sounds.Count > 0)
            {
                _soundDictionary[collection.Type] = collection;
            }
        }
    }

    private void Start()
    {
        // MusicLevel starts a second, silent MusicInvincibility source.  Starting both clips for
        // the first time at the end of the countdown forces Unity/FMOD to create their native
        // sounds in a gameplay frame. Do that one-time work while the initial scene is loading.
        StartCoroutine(PrewarmLevelMusic());
    }

    private IEnumerator PrewarmLevelMusic()
    {
        if (_levelMusicPrewarmed)
        {
            yield break;
        }

        _levelMusicPrewarmed = true;

        var clipsToPrewarm = new HashSet<AudioClip>();
        CollectClips(SoundType.MusicLevel, clipsToPrewarm);
        CollectClips(SoundType.MusicInvincibility, clipsToPrewarm);

        foreach (AudioClip clip in clipsToPrewarm)
        {
            // This is normally already requested by the importer's Preload Audio Data option,
            // but calling it is harmless for an already-loaded clip and covers clips configured
            // not to preload.
            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            while (clip.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                GameLogger.LogWarning("Audio", $"Could not prewarm music clip {clip.name}.");
                continue;
            }

            // Assigning a clip is not enough to populate FMOD's first-use cache. Play it silently
            // for one frame so the expensive native Sound creation cannot occur during countdown.
            var warmupObject = new GameObject($"MusicWarmup_{clip.name}");
            warmupObject.transform.SetParent(transform);
            var warmupSource = warmupObject.AddComponent<AudioSource>();
            warmupSource.clip = clip;
            warmupSource.volume = 0f;
            warmupSource.loop = false;
            warmupSource.playOnAwake = false;

            if (musicMixerGroup != null)
            {
                warmupSource.outputAudioMixerGroup = musicMixerGroup;
            }

            warmupSource.Play();
            yield return null;
            warmupSource.Stop();
            Destroy(warmupObject);

            // Keep the two first-use operations out of the same frame.
            yield return null;
        }

        GameLogger.Log("Audio", "Level music prewarm complete");
    }

    private void CollectClips(SoundType soundType, HashSet<AudioClip> clips)
    {
        if (!_soundDictionary.TryGetValue(soundType, out SoundCollection collection)
            || collection.Sounds == null)
        {
            return;
        }

        foreach (Sound sound in collection.Sounds)
        {
            if (sound.Clip != null)
            {
                clips.Add(sound.Clip);
            }
        }
    }

    public Sound Play(SoundType soundType)
    {
        if (!_soundDictionary.TryGetValue(soundType, out SoundCollection collection))
        {
            GameLogger.LogWarning("Audio", $"Sound type {soundType} not found!");
            return null;
        }

        if (collection.Sounds == null || collection.Sounds.Count == 0)
        {
            GameLogger.LogWarning("Audio", $"Sound collection {soundType} is empty!");
            return null;
        }

        Sound s = collection.Sounds[Random.Range(0, collection.Sounds.Count)];

        var soundObj = new GameObject($"Sound_{soundType}");
        soundObj.transform.SetParent(transform);
        var audioSrc = soundObj.AddComponent<AudioSource>();

        audioSrc.clip = s.Clip;
        audioSrc.volume = s.Volume;
        audioSrc.playOnAwake = false;
        
        if (s.IsMusic && musicMixerGroup != null)
        {
            audioSrc.outputAudioMixerGroup = musicMixerGroup;
        }
        else if (!s.IsMusic && sfxMixerGroup != null)
        {
            audioSrc.outputAudioMixerGroup = sfxMixerGroup;
        }

        audioSrc.Play();

        StartCoroutine(DestroyAfterPlay(soundObj, s.Clip.length + 0.1f));
        return s;
    }

    private IEnumerator DestroyAfterPlay(GameObject soundObj, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Destroy(soundObj);
    }

    public void ChangeMusic(SoundType soundType)
    {
        if (!_soundDictionary.TryGetValue(soundType, out SoundCollection collection))
        {
            GameLogger.LogWarning("Audio", $"Music track {soundType} not found!");
            return;
        }

        if (collection.Sounds == null || collection.Sounds.Count == 0)
        {
            GameLogger.LogWarning("Audio", $"Music collection {soundType} is empty!");
            return;
        }

        Sound track = collection.Sounds[Random.Range(0, collection.Sounds.Count)];

        if (_musicSource == null)
        {
            var container = new GameObject("MusicSourceObj");
            container.transform.SetParent(transform);
            _musicSource = container.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            
            if (musicMixerGroup != null)
            {
                _musicSource.outputAudioMixerGroup = musicMixerGroup;
            }
        }

        if (_musicSource.isPlaying)
        {
            _musicSource.Stop();
        }

        _musicSource.clip = track.Clip;
        _musicSource.volume = track.Volume;
        _musicSource.Play();

        if (soundType == SoundType.MusicLevel)
            StartInvincibilityLayer();
        else
            StopInvincibilityLayer();
        
        GameLogger.Log("Audio", $"Music changed to {soundType}");
    }

    /// <summary>
    /// Starts the MusicInvincibility track as a silent layer synchronized with the level music.
    /// </summary>
    private void StartInvincibilityLayer()
    {
        if (!_soundDictionary.TryGetValue(SoundType.MusicInvincibility, out SoundCollection collection)
            || collection.Sounds == null || collection.Sounds.Count == 0)
        {
            GameLogger.LogWarning("Audio", "MusicInvincibility collection not found or empty!");
            return;
        }

        Sound track = collection.Sounds[Random.Range(0, collection.Sounds.Count)];

        if (_invincibilityMusicSource == null)
        {
            var container = new GameObject("InvincibilityMusicSourceObj");
            container.transform.SetParent(transform);
            _invincibilityMusicSource = container.AddComponent<AudioSource>();
            _invincibilityMusicSource.loop = true;
            _invincibilityMusicSource.playOnAwake = false;

            if (musicMixerGroup != null)
                _invincibilityMusicSource.outputAudioMixerGroup = musicMixerGroup;
        }

        if (_invincibilityMusicSource.isPlaying)
            _invincibilityMusicSource.Stop();

        _invincibilityMusicSource.clip = track.Clip;
        _invincibilityMusicSource.volume = InvincibilityLayerBaseVolume;
        _invincibilityMusicSource.Play();

        GameLogger.Log("Audio", "Invincibility music layer started");
    }

    /// <summary>
    /// Stops the MusicInvincibility layer, e.g. when leaving the gameplay scene.
    /// </summary>
    private void StopInvincibilityLayer()
    {
        if (_invincibilityMusicSource != null && _invincibilityMusicSource.isPlaying)
        {
            _invincibilityMusicSource.Stop();
            GameLogger.Log("Audio", "Invincibility music layer stopped");
        }
    }

    /// <summary>
    /// Sets the volume of the MusicInvincibility layer. Use InvincibilityLayerActiveVolume (0.6) when the power-up
    /// is active, and InvincibilityLayerBaseVolume (0.01) when it expires.
    /// </summary>
    public void SetInvincibilityLayerVolume(float volume)
    {
        if (_invincibilityMusicSource != null)
            _invincibilityMusicSource.volume = Mathf.Clamp01(volume);
    }

    public void AddMusic(SoundType type)
    {
        if (!_soundDictionary.TryGetValue(type, out SoundCollection collection))
        {
            GameLogger.LogWarning("Audio", $"Music track {type} not found!");
            return;
        }

        if (collection.Sounds == null || collection.Sounds.Count == 0)
        {
            GameLogger.LogWarning("Audio", $"Music collection {type} is empty!");
            return;
        }

        Sound track = collection.Sounds[Random.Range(0, collection.Sounds.Count)];

        if (_musicSource == null)
        {
            var container = new GameObject("MusicSourceObj");
            container.transform.SetParent(transform);
            _musicSource = container.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            
            if (musicMixerGroup != null)
            {
                _musicSource.outputAudioMixerGroup = musicMixerGroup;
            }
        }

        _musicSource.clip = track.Clip;
        _musicSource.volume = track.Volume;
        _musicSource.Play();
        
        GameLogger.Log("Audio", $"Music added: {type}");
    }
    
    public void StopMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
        {
            _musicSource.Stop();
            GameLogger.Log("Audio", "Music stopped");
        }
        StopInvincibilityLayer();
    }
    
    /// <summary>
    /// Stops all audio immediately, including music and any playing one-shot SFX sources.
    /// </summary>
    public void StopAll()
    {
        StopMusic();
        
        foreach (AudioSource source in GetComponentsInChildren<AudioSource>())
        {
            if (source != _musicSource && source.isPlaying)
            {
                source.Stop();
            }
        }
        
        GameLogger.Log("Audio", "All audio stopped");
    }
    
    public void SetMusicVolume(float volume)
    {
        if (_musicSource != null)
        {
            _musicSource.volume = Mathf.Clamp01(volume);
        }
    }
}
