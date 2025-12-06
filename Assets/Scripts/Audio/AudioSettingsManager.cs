using UnityEngine;
using MoreMountains.Tools;

public class AudioSettingsManager : MonoBehaviour
{
    private static AudioSettingsManager instance;
    public static AudioSettingsManager Instance => instance;
    
    [Header("MM Sound Manager Settings")]
    [SerializeField] private MMSoundManagerSettingsSO soundManagerSettings;
    
    private const string MASTER_VOLUME_KEY = "Audio_MasterVolume";
    private const string MUSIC_VOLUME_KEY = "Audio_MusicVolume";
    private const string SFX_VOLUME_KEY = "Audio_SfxVolume";
    private const string MASTER_MUTED_KEY = "Audio_MasterMuted";
    private const string MUSIC_MUTED_KEY = "Audio_MusicMuted";
    private const string SFX_MUTED_KEY = "Audio_SfxMuted";
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (soundManagerSettings == null)
            {
                soundManagerSettings = Resources.Load<MMSoundManagerSettingsSO>("MMSoundManager/Settings/MMSoundManagerSettings");
            }
            
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        ApplyLoadedSettings();
    }
    
    private void LoadSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        
        bool masterMuted = PlayerPrefs.GetInt(MASTER_MUTED_KEY, 0) == 1;
        bool musicMuted = PlayerPrefs.GetInt(MUSIC_MUTED_KEY, 0) == 1;
        bool sfxMuted = PlayerPrefs.GetInt(SFX_MUTED_KEY, 0) == 1;
        
        GameLogger.Log("AUDIO", $"Settings loaded - Master: {masterVolume:F2} (Muted: {masterMuted}), Music: {musicVolume:F2} (Muted: {musicMuted}), SFX: {sfxVolume:F2} (Muted: {sfxMuted})");
    }
    
    private void ApplyLoadedSettings()
    {
        if (soundManagerSettings == null)
        {
            GameLogger.LogError("AUDIO", "MMSoundManagerSettings not found!");
            return;
        }
        
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        
        bool masterMuted = PlayerPrefs.GetInt(MASTER_MUTED_KEY, 0) == 1;
        bool musicMuted = PlayerPrefs.GetInt(MUSIC_MUTED_KEY, 0) == 1;
        bool sfxMuted = PlayerPrefs.GetInt(SFX_MUTED_KEY, 0) == 1;
        
        soundManagerSettings.Settings.MasterOn = !masterMuted;
        soundManagerSettings.Settings.MusicOn = !musicMuted;
        soundManagerSettings.Settings.SfxOn = !sfxMuted;
        
        soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Master, masterVolume);
        soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Music, musicVolume);
        soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Sfx, sfxVolume);
        
        GameLogger.Log("AUDIO", "Settings applied to MMSoundManager");
    }
    
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        if (soundManagerSettings != null)
        {
            soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Master, volume);
        }
        
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        
        GameLogger.Log("AUDIO", $"Master volume set to {volume:F2}");
    }
    
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        if (soundManagerSettings != null)
        {
            soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Music, volume);
        }
        
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        
        GameLogger.Log("AUDIO", $"Music volume set to {volume:F2}");
    }
    
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        if (soundManagerSettings != null)
        {
            soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Sfx, volume);
        }
        
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        
        GameLogger.Log("AUDIO", $"SFX volume set to {volume:F2}");
    }
    
    public void ToggleMasterMute()
    {
        bool isMuted = IsMasterMuted();
        SetMasterMuted(!isMuted);
    }
    
    public void SetMasterMuted(bool muted)
    {
        if (soundManagerSettings != null)
        {
            soundManagerSettings.Settings.MasterOn = !muted;
            
            if (muted)
            {
                soundManagerSettings.TargetAudioMixer.SetFloat(soundManagerSettings.Settings.MasterVolumeParameter, -80f);
            }
            else
            {
                float volume = GetMasterVolume();
                soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Master, volume);
            }
        }
        
        PlayerPrefs.SetInt(MASTER_MUTED_KEY, muted ? 1 : 0);
        PlayerPrefs.Save();
        
        GameLogger.Log("AUDIO", $"Master {(muted ? "MUTED" : "UNMUTED")}");
    }
    
    public void ToggleMusicMute()
    {
        bool isMuted = IsMusicMuted();
        SetMusicMuted(!isMuted);
    }
    
    public void SetMusicMuted(bool muted)
    {
        if (soundManagerSettings != null)
        {
            soundManagerSettings.Settings.MusicOn = !muted;
            
            if (muted)
            {
                soundManagerSettings.TargetAudioMixer.SetFloat(soundManagerSettings.Settings.MusicVolumeParameter, -80f);
            }
            else
            {
                float volume = GetMusicVolume();
                soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Music, volume);
            }
        }
        
        PlayerPrefs.SetInt(MUSIC_MUTED_KEY, muted ? 1 : 0);
        PlayerPrefs.Save();
        
        GameLogger.Log("AUDIO", $"Music {(muted ? "MUTED" : "UNMUTED")}");
    }
    
    public void ToggleSFXMute()
    {
        bool isMuted = IsSFXMuted();
        SetSFXMuted(!isMuted);
    }
    
    public void SetSFXMuted(bool muted)
    {
        if (soundManagerSettings != null)
        {
            soundManagerSettings.Settings.SfxOn = !muted;
            
            if (muted)
            {
                soundManagerSettings.TargetAudioMixer.SetFloat(soundManagerSettings.Settings.SfxVolumeParameter, -80f);
            }
            else
            {
                float volume = GetSFXVolume();
                soundManagerSettings.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Sfx, volume);
            }
        }
        
        PlayerPrefs.SetInt(SFX_MUTED_KEY, muted ? 1 : 0);
        PlayerPrefs.Save();
        
        GameLogger.Log("AUDIO", $"SFX {(muted ? "MUTED" : "UNMUTED")}");
    }
    
    public float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
    }
    
    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
    }
    
    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }
    
    public bool IsMasterMuted()
    {
        return PlayerPrefs.GetInt(MASTER_MUTED_KEY, 0) == 1;
    }
    
    public bool IsMusicMuted()
    {
        return PlayerPrefs.GetInt(MUSIC_MUTED_KEY, 0) == 1;
    }
    
    public bool IsSFXMuted()
    {
        return PlayerPrefs.GetInt(SFX_MUTED_KEY, 0) == 1;
    }
    
    public void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetMusicVolume(0.5f);
        SetSFXVolume(1f);
        
        SetMasterMuted(false);
        SetMusicMuted(false);
        SetSFXMuted(false);
        
        GameLogger.Log("AUDIO", "Settings reset to defaults");
    }
}
