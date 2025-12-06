using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadVibrationManager : MonoBehaviour
{
    private static GamepadVibrationManager instance;
    public static GamepadVibrationManager Instance => instance;
    
    private const string VIBRATION_ENABLED_KEY = "VibrationEnabled";
    
    private bool vibrationEnabled = true;
    private Gamepad currentGamepad;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        UpdateCurrentGamepad();
    }
    
    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        StopAllVibrations();
    }
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            UpdateCurrentGamepad();
        }
    }
    
    private void UpdateCurrentGamepad()
    {
        currentGamepad = Gamepad.current;
        
        if (currentGamepad != null)
        {
            GameLogger.Log("VIBRATION", $"Gamepad detected: {currentGamepad.displayName}");
        }
        else
        {
            GameLogger.LogWarning("VIBRATION", "No gamepad connected");
        }
    }
    
    private void LoadSettings()
    {
        vibrationEnabled = PlayerPrefs.GetInt(VIBRATION_ENABLED_KEY, 1) == 1;
        GameLogger.Log("VIBRATION", $"Vibration loaded: {(vibrationEnabled ? "ENABLED" : "DISABLED")}");
    }
    
    public void SetVibrationEnabled(bool enabled)
    {
        vibrationEnabled = enabled;
        PlayerPrefs.SetInt(VIBRATION_ENABLED_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        
        if (!enabled)
        {
            StopAllVibrations();
        }
        
        GameLogger.Log("VIBRATION", $"Vibration {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    public bool IsVibrationEnabled()
    {
        return vibrationEnabled;
    }
    
    public void TriggerVibration(float lowFrequency, float highFrequency, float duration)
    {
        if (!vibrationEnabled)
        {
            GameLogger.LogVerbose("VIBRATION", "Vibration disabled, skipping");
            return;
        }
        
        if (currentGamepad == null)
        {
            GameLogger.LogVerbose("VIBRATION", "No gamepad connected");
            return;
        }
        
        currentGamepad.SetMotorSpeeds(lowFrequency, highFrequency);
        GameLogger.LogVerbose("VIBRATION", $"Vibration triggered: Low={lowFrequency:F2}, High={highFrequency:F2}, Duration={duration:F2}s");
        
        CancelInvoke(nameof(StopAllVibrations));
        Invoke(nameof(StopAllVibrations), duration);
    }
    
    public void TriggerLightVibration(float duration = 0.1f)
    {
        TriggerVibration(0.2f, 0.3f, duration);
    }
    
    public void TriggerMediumVibration(float duration = 0.2f)
    {
        TriggerVibration(0.4f, 0.6f, duration);
    }
    
    public void TriggerHeavyVibration(float duration = 0.3f)
    {
        TriggerVibration(0.7f, 0.9f, duration);
    }
    
    public void TriggerHitVibration()
    {
        TriggerVibration(0.5f, 0.7f, 0.15f);
    }
    
    public void StopAllVibrations()
    {
        if (currentGamepad != null)
        {
            currentGamepad.SetMotorSpeeds(0f, 0f);
        }
        
        CancelInvoke(nameof(StopAllVibrations));
    }
}
