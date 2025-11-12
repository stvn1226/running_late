using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class AnalyticsBootstrap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool grantConsentByDefault = true;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;
    
    private bool isInitialized = false;
    
    public static AnalyticsBootstrap Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (autoInitialize)
        {
            InitializeAnalytics();
        }
    }
    
    public async void InitializeAnalytics()
    {
        if (isInitialized)
        {
            LogMessage("Analytics already initialized");
            return;
        }
        
        try
        {
            LogMessage("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
            LogMessage("Unity Services initialized successfully");
            
            if (grantConsentByDefault)
            {
#pragma warning disable CS0618
                AnalyticsService.Instance.StartDataCollection();
#pragma warning restore CS0618
                LogMessage("Data collection started");
            }
            
            isInitialized = true;
            LogMessage("Analytics Bootstrap complete");
            
            SendGameStartedEvent();
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to initialize Unity Services: {e.Message}");
        }
    }
    
    private void SendGameStartedEvent()
    {
        try
        {
            CustomEvent gameStarted = new CustomEvent("game_session_start")
            {
                { "clientVersion", Application.version },
                { "platform", Application.platform.ToString() },
                { "sdkMethod", "Unity.Services.Analytics" },
                { "userCountry", System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName }
            };
        
            AnalyticsService.Instance.RecordEvent(gameStarted);
            LogMessage("Game started event sent");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error sending game started event: {e.Message}");
        }
    }
    
    private void OnApplicationQuit()
    {
        if (!isInitialized) return;
        
        try
        {
            AnalyticsService.Instance.Flush();
            LogMessage("Analytics buffer flushed on quit");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error flushing analytics on quit: {e.Message}");
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized) return;
        
        if (pauseStatus)
        {
            try
            {
                AnalyticsService.Instance.Flush();
                LogMessage("Analytics buffer flushed on pause");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError("ANALYTICS", $"Error flushing analytics on pause: {e.Message}");
            }
        }
    }
    
    public void StopDataCollection()
    {
        if (!isInitialized) return;
        
        try
        {
#pragma warning disable CS0618
            AnalyticsService.Instance.StopDataCollection();
#pragma warning restore CS0618
            LogMessage("Data collection stopped");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error stopping data collection: {e.Message}");
        }
    }
    
    public void RequestDataDeletion()
    {
        if (!isInitialized) return;
        
        try
        {
            StopDataCollection();
            AnalyticsService.Instance.RequestDataDeletion();
            LogMessage("Data deletion requested");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error requesting data deletion: {e.Message}");
        }
    }
    
    private void LogMessage(string message)
    {
        if (verboseLogging)
        {
            GameLogger.Log("ANALYTICS", $"[Bootstrap] {message}");
        }
    }
    
    public bool IsInitialized => isInitialized;
}
