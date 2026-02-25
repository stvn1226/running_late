using UnityEngine;
using UnityEngine.Audio;
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
        CD_Numbers,
        CD_Go,
        Music_Menu,
        Music_Level,
        Footsteps
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

    private Dictionary<SoundType, SoundCollection> _soundDictionary = new Dictionary<SoundType, SoundCollection>();
    private AudioSource _musicSource;

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

    public void Play(SoundType type)
    {
        if (!_soundDictionary.TryGetValue(type, out SoundCollection collection))
        {
            GameLogger.LogWarning("Audio", $"Sound type {type} not found!");
            return;
        }

        if (collection.Sounds == null || collection.Sounds.Count == 0)
        {
            GameLogger.LogWarning("Audio", $"Sound collection {type} is empty!");
            return;
        }

        Sound s = collection.Sounds[Random.Range(0, collection.Sounds.Count)];

        var soundObj = new GameObject($"Sound_{type}");
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

        Destroy(soundObj, s.Clip.length + 0.1f);
    }

    public void ChangeMusic(SoundType type)
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

        if (_musicSource.isPlaying)
        {
            _musicSource.Stop();
        }

        _musicSource.clip = track.Clip;
        _musicSource.volume = track.Volume;
        _musicSource.Play();
        
        GameLogger.Log("Audio", $"Music changed to {type}");
    }
    
    public void StopMusic()
    {
        if (_musicSource != null && _musicSource.isPlaying)
        {
            _musicSource.Stop();
            GameLogger.Log("Audio", "Music stopped");
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        if (_musicSource != null)
        {
            _musicSource.volume = Mathf.Clamp01(volume);
        }
    }
}
