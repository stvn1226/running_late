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
        CD_Numbers,
        CD_Go,
        Music_Menu,
        Music_Level
    }

    [System.Serializable]
    public class Sound
    {
        public SoundType Type;
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

    private Dictionary<SoundType, Sound> _soundDictionary = new Dictionary<SoundType, Sound>();
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

        foreach (var c in AllCollections)
        {
            foreach (var s in c.Sounds)
            {
                _soundDictionary[s.Type] = s;
            }
        }
    }

    public void Play(SoundType type)
    {
        if (!_soundDictionary.TryGetValue(type, out Sound s))
        {
            GameLogger.LogWarning("Audio", $"Sound type {type} not found!");
            return;
        }

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
        if (!_soundDictionary.TryGetValue(type, out Sound track))
        {
            GameLogger.LogWarning("Audio", $"Music track {type} not found!");
            return;
        }

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
