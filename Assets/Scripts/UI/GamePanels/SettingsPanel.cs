using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : BaseMainMenuPanel
{
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Toggle masterMuteToggle;
    
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Toggle musicMuteToggle;
    
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Toggle sfxMuteToggle;
    
    [Header("Display Settings")]
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Toggle fps60Toggle;
    [SerializeField] private Toggle fps120Toggle;
    
    [Header("Controller Settings")]
    [SerializeField] private Toggle vibrationToggle;
    
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button resetButton;
    
    protected override void SetupPanel()
    {
        SetupAudioSliders();
        SetupAudioMuteToggles();
        SetupDisplayToggles();
        SetupVibrationToggle();
        SetupButtons();
    }
    
    private void SetupAudioSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    
    private void SetupAudioMuteToggles()
    {
        if (masterMuteToggle != null)
        {
            masterMuteToggle.onValueChanged.AddListener(OnMasterMuteToggled);
        }
        
        if (musicMuteToggle != null)
        {
            musicMuteToggle.onValueChanged.AddListener(OnMusicMuteToggled);
        }
        
        if (sfxMuteToggle != null)
        {
            sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteToggled);
        }
    }
    
    private void SetupDisplayToggles()
    {
        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged.AddListener(OnVSyncToggled);
        }
        
        if (fps60Toggle != null)
        {
            fps60Toggle.onValueChanged.AddListener((isOn) => {
                if (isOn) OnFrameRateSelected(60);
            });
        }
        
        if (fps120Toggle != null)
        {
            fps120Toggle.onValueChanged.AddListener((isOn) => {
                if (isOn) OnFrameRateSelected(120);
            });
        }
    }
    
    private void SetupVibrationToggle()
    {
        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        }
    }
    
    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
    }
    
    protected override void OnShow()
    {
        LoadCurrentSettings();
    }
    
    private void LoadCurrentSettings()
    {
        if (AudioSettingsManager.Instance != null)
        {
            float masterVolume = AudioSettingsManager.Instance.GetMasterVolume();
            float musicVolume = AudioSettingsManager.Instance.GetMusicVolume();
            float sfxVolume = AudioSettingsManager.Instance.GetSFXVolume();
            
            bool masterMuted = AudioSettingsManager.Instance.IsMasterMuted();
            bool musicMuted = AudioSettingsManager.Instance.IsMusicMuted();
            bool sfxMuted = AudioSettingsManager.Instance.IsSFXMuted();
            
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(masterVolume);
                UpdateVolumeText(masterVolumeText, masterVolume);
            }
            
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(musicVolume);
                UpdateVolumeText(musicVolumeText, musicVolume);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
                UpdateVolumeText(sfxVolumeText, sfxVolume);
            }
            
            if (masterMuteToggle != null)
            {
                masterMuteToggle.SetIsOnWithoutNotify(masterMuted);
            }
            
            if (musicMuteToggle != null)
            {
                musicMuteToggle.SetIsOnWithoutNotify(musicMuted);
            }
            
            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(sfxMuted);
            }
        }
        
        if (DisplaySettingsManager.Instance != null)
        {
            bool vSyncEnabled = DisplaySettingsManager.Instance.IsVSyncEnabled();
            int targetFPS = DisplaySettingsManager.Instance.GetTargetFrameRate();
            
            if (vSyncToggle != null)
            {
                vSyncToggle.SetIsOnWithoutNotify(vSyncEnabled);
            }
            
            if (fps60Toggle != null)
            {
                fps60Toggle.SetIsOnWithoutNotify(targetFPS == 60);
            }
            
            if (fps120Toggle != null)
            {
                fps120Toggle.SetIsOnWithoutNotify(targetFPS == 120);
            }
        }
        
        if (GamepadVibrationManager.Instance != null)
        {
            bool vibrationEnabled = GamepadVibrationManager.Instance.IsVibrationEnabled();
            
            if (vibrationToggle != null)
            {
                vibrationToggle.SetIsOnWithoutNotify(vibrationEnabled);
            }
        }
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMasterVolume(value);
        UpdateVolumeText(masterVolumeText, value);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        AudioSettingsManager.Instance?.SetMusicVolume(value);
        UpdateVolumeText(musicVolumeText, value);
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        AudioSettingsManager.Instance?.SetSFXVolume(value);
        UpdateVolumeText(sfxVolumeText, value);
    }
    
    private void UpdateVolumeText(TextMeshProUGUI textComponent, float volume)
    {
        if (textComponent != null)
        {
            textComponent.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
    
    private void OnMasterMuteToggled(bool muted)
    {
        AudioSettingsManager.Instance?.SetMasterMuted(muted);
    }
    
    private void OnMusicMuteToggled(bool muted)
    {
        AudioSettingsManager.Instance?.SetMusicMuted(muted);
    }
    
    private void OnSFXMuteToggled(bool muted)
    {
        AudioSettingsManager.Instance?.SetSFXMuted(muted);
    }
    
    private void OnVSyncToggled(bool enabled)
    {
        DisplaySettingsManager.Instance?.SetVSync(enabled ? 1 : 0);
    }
    
    private void OnFrameRateSelected(int targetFPS)
    {
        DisplaySettingsManager.Instance?.SetTargetFrameRate(targetFPS);
        
        if (fps60Toggle != null)
        {
            fps60Toggle.SetIsOnWithoutNotify(targetFPS == 60);
        }
        
        if (fps120Toggle != null)
        {
            fps120Toggle.SetIsOnWithoutNotify(targetFPS == 120);
        }
    }
    
    private void OnVibrationToggled(bool enabled)
    {
        GamepadVibrationManager.Instance?.SetVibrationEnabled(enabled);
    }
    
    private void OnBackClicked()
    {
        menuController?.ShowMainPanel();
    }
    
    private void OnResetClicked()
    {
        AudioSettingsManager.Instance?.ResetToDefaults();
        DisplaySettingsManager.Instance?.ResetToDefaults();
        GamepadVibrationManager.Instance?.SetVibrationEnabled(true);
        
        LoadCurrentSettings();
        
        GameLogger.Log("SETTINGS", "All settings reset to defaults");
    }
}
