using UnityEngine;

public class DisplaySettingsManager : MonoBehaviour
{
    private static DisplaySettingsManager instance;
    public static DisplaySettingsManager Instance => instance;
    
    private const string VSYNC_KEY = "Display_VSync";
    private const string FRAMERATE_KEY = "Display_TargetFrameRate";
    
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
    
    private void LoadSettings()
    {
        int vSyncCount = PlayerPrefs.GetInt(VSYNC_KEY, 0);
        int targetFrameRate = PlayerPrefs.GetInt(FRAMERATE_KEY, 60);
        
        SetVSync(vSyncCount);
        SetTargetFrameRate(targetFrameRate);
        
        GameLogger.Log("DISPLAY", $"Settings loaded - VSync: {vSyncCount}, Target FPS: {targetFrameRate}");
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
    
    public void ResetToDefaults()
    {
        SetVSync(0);
        SetTargetFrameRate(60);
        
        GameLogger.Log("DISPLAY", "Display settings reset to defaults");
    }
}
