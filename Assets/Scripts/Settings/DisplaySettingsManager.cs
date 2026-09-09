using UnityEngine;
using System.Collections.Generic;

public class DisplaySettingsManager : MonoBehaviour
{
    private static DisplaySettingsManager instance;
    public static DisplaySettingsManager Instance => instance;
    
    private const string VSYNC_KEY = "Display_VSync";
    private const string FRAMERATE_KEY = "Display_TargetFrameRate";
    private const string FULLSCREEN_MODE_KEY = "Display_FullscreenMode";
    private const string RESOLUTION_INDEX_KEY = "Display_ResolutionIndex";
    
    private List<Resolution> availableResolutions = new List<Resolution>();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("DisplaySettingsManager");
            instance = go.AddComponent<DisplaySettingsManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeResolutions();
            LoadSettings();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        availableResolutions.Clear();
        
        HashSet<string> uniqueResolutions = new HashSet<string>();
        
        foreach (Resolution res in allResolutions)
        {
            if (res.width >= 1920 && res.height >= 1080)
            {
                string key = $"{res.width}x{res.height}";
                if (!uniqueResolutions.Contains(key))
                {
                    uniqueResolutions.Add(key);
                    availableResolutions.Add(res);
                }
            }
        }
        
        GameLogger.Log("DISPLAY", $"Found {availableResolutions.Count} unique resolutions");
    }
    
    private void LoadSettings()
    {
        int vSyncCount = PlayerPrefs.GetInt(VSYNC_KEY, 0);
        int targetFrameRate = PlayerPrefs.GetInt(FRAMERATE_KEY, 60);
        // Default to FullScreenWindow (Borderless) for maximum stability across all monitor refresh rates
        int fullscreenModeIndex = PlayerPrefs.GetInt(FULLSCREEN_MODE_KEY, (int)FullScreenMode.FullScreenWindow);
        int resolutionIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, -1);
        
        SetVSync(vSyncCount);
        SetTargetFrameRate(targetFrameRate);
        SetFullscreenMode((FullScreenMode)fullscreenModeIndex);
        
        if (resolutionIndex >= 0 && resolutionIndex < availableResolutions.Count)
        {
            SetResolution(resolutionIndex);
        }
        
        GameLogger.Log("DISPLAY", $"Settings loaded - VSync: {vSyncCount}, FPS: {targetFrameRate}, Mode: {(FullScreenMode)fullscreenModeIndex}, ResIndex: {resolutionIndex}");
    }
    
    public void SetVSync(int vSyncCount)
    {
        vSyncCount = Mathf.Clamp(vSyncCount, 0, 4);
        
        QualitySettings.vSyncCount = vSyncCount;
        PlayerPrefs.SetInt(VSYNC_KEY, vSyncCount);
        PlayerPrefs.Save();
        
        GameLogger.Log("DISPLAY", $"VSync set to {vSyncCount}");
    }
    
    public void ToggleVSync()
    {
        int currentVSync = GetVSyncCount();
        int newVSync = currentVSync == 0 ? 1 : 0;
        SetVSync(newVSync);
    }
    
    public int GetVSyncCount()
    {
        return QualitySettings.vSyncCount;
    }
    
    public bool IsVSyncEnabled()
    {
        return QualitySettings.vSyncCount > 0;
    }
    
    public void SetTargetFrameRate(int targetFPS)
    {
        if (targetFPS <= 0)
        {
            targetFPS = -1;
        }
        
        Application.targetFrameRate = targetFPS;
        PlayerPrefs.SetInt(FRAMERATE_KEY, targetFPS);
        PlayerPrefs.Save();
        
        GameLogger.Log("DISPLAY", $"Target frame rate set to {targetFPS}");
    }
    
    public int GetTargetFrameRate()
    {
        return PlayerPrefs.GetInt(FRAMERATE_KEY, 60);
    }
    
    public void SetFullscreenMode(FullScreenMode mode)
    {
        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt(FULLSCREEN_MODE_KEY, (int)mode);
        PlayerPrefs.Save();
        
        GameLogger.Log("DISPLAY", $"Fullscreen mode set to {mode}");
    }
    
    public FullScreenMode GetFullscreenMode()
    {
        int modeIndex = PlayerPrefs.GetInt(FULLSCREEN_MODE_KEY, 0);
        return (FullScreenMode)modeIndex;
    }
    
    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Count)
        {
            GameLogger.LogWarning("DISPLAY", $"Invalid resolution index: {resolutionIndex}");
            return;
        }
        
        Resolution res = availableResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        
        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionIndex);
        PlayerPrefs.Save();
        
        GameLogger.Log("DISPLAY", $"Resolution set to {res.width}x{res.height}");
    }
    
    public int GetCurrentResolutionIndex()
    {
        int savedIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, -1);
        
        if (savedIndex >= 0 && savedIndex < availableResolutions.Count)
        {
            return savedIndex;
        }
        
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Resolution res = availableResolutions[i];
            if (res.width == Screen.width && res.height == Screen.height)
            {
                return i;
            }
        }
        
        return 0;
    }
    
    public List<Resolution> GetAvailableResolutions()
    {
        return new List<Resolution>(availableResolutions);
    }
    
    public string GetResolutionLabel(int index)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return "Unknown";
        
        Resolution res = availableResolutions[index];
        
        if (res.width == 1920 && res.height == 1080)
            return "1920x1080 (Full HD)";
        else if (res.width == 2560 && res.height == 1440)
            return "2560x1440 (2K)";
        else if (res.width == 3840 && res.height == 2160)
            return "3840x2160 (4K)";
        else
            return $"{res.width}x{res.height}";
    }
    
    public void ResetToDefaults()
    {
        SetVSync(0);
        SetTargetFrameRate(60);
        SetFullscreenMode(FullScreenMode.ExclusiveFullScreen);
        
        if (availableResolutions.Count > 0)
        {
            SetResolution(0);
        }
        
        GameLogger.Log("DISPLAY", "Display settings reset to defaults");
    }
}
