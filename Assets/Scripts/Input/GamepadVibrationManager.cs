using UnityEngine;
using MoreMountains.Feedbacks;
#if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
using Lofelt.NiceVibrations;
#endif

public class GamepadVibrationManager : MonoBehaviour
{
    private static GamepadVibrationManager instance;
    public static GamepadVibrationManager Instance => instance;
    
    private const string VIBRATION_ENABLED_KEY = "VibrationEnabled";
    private bool vibrationEnabled = true;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            InitializeHaptics();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeHaptics()
    {
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        HapticController.Init();
        GameLogger.Log("HAPTICS", "Nice Vibrations initialized!");
        
        Invoke(nameof(TestHaptic), 2f);
        #else
        GameLogger.LogWarning("HAPTICS", "Nice Vibrations NOT installed! Add MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED to scripting defines.");
        #endif
    }
    
    private void TestHaptic()
    {
        GameLogger.Log("HAPTICS", ">>> TESTING HAPTIC FEEDBACK <<<");
        TriggerHitVibration();
    }
    
    private void LoadSettings()
    {
        vibrationEnabled = PlayerPrefs.GetInt(VIBRATION_ENABLED_KEY, 1) == 1;
        GameLogger.Log("HAPTICS", $"Settings: {(vibrationEnabled ? "ENABLED" : "DISABLED")}");
    }
    
    public void SetVibrationEnabled(bool enabled)
    {
        vibrationEnabled = enabled;
        PlayerPrefs.SetInt(VIBRATION_ENABLED_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        if (!enabled)
        {
            HapticController.Stop();
        }
        #endif
        
        GameLogger.Log("HAPTICS", $"Vibration {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    public bool IsVibrationEnabled()
    {
        return vibrationEnabled;
    }
    
    public void TriggerLightVibration(float duration = 0.05f)
    {
        if (!vibrationEnabled) return;
        
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
        GameLogger.Log("HAPTICS", "▶ Light Impact");
        #endif
    }
    
    public void TriggerMediumVibration(float duration = 0.15f)
    {
        if (!vibrationEnabled) return;
        
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
        GameLogger.Log("HAPTICS", "▶ Medium Impact");
        #endif
    }
    
    public void TriggerHeavyVibration(float duration = 0.25f)
    {
        if (!vibrationEnabled) return;
        
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        GameLogger.Log("HAPTICS", "▶ Heavy Impact");
        #endif
    }
    
    public void TriggerHitVibration()
    {
        if (!vibrationEnabled) return;
        
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
        GameLogger.Log("HAPTICS", "▶ HIT!");
        #endif
    }
    
    public void StopAllVibrations()
    {
        #if MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED
        HapticController.Stop();
        #endif
    }
    
    private void OnDisable()
    {
        StopAllVibrations();
    }
    
    private void OnApplicationQuit()
    {
        StopAllVibrations();
    }
}

