using UnityEngine;
using System.Collections.Generic;
using Utils;

public class AnalyticsManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AnalyticsConfig config;
    
    [Header("Local Tracking")]
    [SerializeField] private bool enableLocalTracking = true;
    [SerializeField] private int maxEventsInMemory = 1000;
    
    private IAnalyticsProvider currentProvider;
    private List<IAnalyticsProvider> activeProviders = new List<IAnalyticsProvider>();
    
    private AnalyticsSession currentSession;
    private List<AnalyticsEventRecord> eventBuffer = new List<AnalyticsEventRecord>();
    
    public static AnalyticsManager Instance { get; private set; }
    
    public AnalyticsSession CurrentSession => currentSession;
    public List<AnalyticsEventRecord> EventBuffer => eventBuffer;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (enableLocalTracking)
        {
            currentSession = new AnalyticsSession();
        }
        
        InitializeProviders();
    }
    
    private void InitializeProviders()
    {
        if (config == null)
        {
            GameLogger.LogWarning("ANALYTICS", "No analytics config assigned!");
            return;
        }
        
        if (config.enableAnalytics)
        {
            currentProvider = CreateProvider(config.selectedProvider);
            
            if (currentProvider != null)
            {
                currentProvider.Initialize();
                activeProviders.Add(currentProvider);
                GameLogger.Log("ANALYTICS", $"Initialized provider: {currentProvider.ProviderName}");
            }
        }
    }
    
    private IAnalyticsProvider CreateProvider(AnalyticsProviderType providerType)
    {
        switch (providerType)
        {
            case AnalyticsProviderType.UnityAnalytics:
                return new UnityAnalyticsProvider();
            case AnalyticsProviderType.GameAnalytics:
                return new GameAnalyticsProvider();
            case AnalyticsProviderType.Custom:
                return new CustomAnalyticsProvider();
            default:
                GameLogger.LogWarning("ANALYTICS", $"Unknown provider type: {providerType}");
                return null;
        }
    }
    
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (enableLocalTracking)
        {
            string providerName = currentProvider != null ? currentProvider.ProviderName : "No Provider";
            var record = new AnalyticsEventRecord(eventName, providerName, parameters);
            
            eventBuffer.Add(record);
            currentSession?.AddEvent(record);
            
            if (eventBuffer.Count > maxEventsInMemory)
            {
                eventBuffer.RemoveAt(0);
            }
        }
        
        if (!config.enableAnalytics) return;
        
        foreach (var provider in activeProviders)
        {
            if (provider.IsInitialized)
            {
                provider.TrackEvent(eventName, parameters);
            }
        }
    }
    
    private string _currentLevelType = "DualPath";

    public void TrackLevelStart(int seed, string levelType)
    {
        _currentLevelType = levelType;
        if (!config.enableAnalytics) return;
        
        var parameters = new Dictionary<string, object>
        {
            { "seed", seed },
            { "level_type", levelType }
        };
        
        TrackEvent(AnalyticsEvents.LEVEL_START, parameters);
        
        foreach (var provider in activeProviders)
        {
            if (provider.IsInitialized)
            {
                provider.TrackLevelStart(seed, levelType);
            }
        }
    }
    
    public void TrackLevelComplete(int seed, float time, bool victory, float progressPercentage = 0f, float playerPositionX = 0f, string defeatReason = "")
    {
        if (!config.enableAnalytics) return;
    
        var parameters = new Dictionary<string, object>
        {
            { "seed", seed },
            { "level_type", _currentLevelType },
            { "completion_time", time },
            { "success", victory }
        };

        if (LeaderboardManager.Instance != null)
        {
            var bestRecord = LeaderboardManager.Instance.GetBestTime(seed);
            if (bestRecord != null)
            {
                parameters["best_time"] = bestRecord.time;
            }
        }
    
        if (!victory)
        {
            parameters["progress_percentage"] = progressPercentage;
            parameters["player_position_x"] = playerPositionX;
            parameters["defeat_reason"] = !string.IsNullOrEmpty(defeatReason) ? defeatReason : "timeout";
        }
    
        TrackEvent(AnalyticsEvents.LEVEL_COMPLETE, parameters);
    
        foreach (var provider in activeProviders)
        {
            if (provider.IsInitialized)
            {
                provider.TrackLevelComplete(seed, _currentLevelType, time, victory);
            }
        }

        if (victory && LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.SaveTime(seed, time);
        }
    }
    
    public void TrackPlayerAction(string actionType, Dictionary<string, object> context = null)
    {
        if (!config.enableAnalytics) return;
        
        var parameters = new Dictionary<string, object>
        {
            { "action_type", actionType }
        };
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }
        
        TrackEvent("player_action", parameters);

        foreach (var provider in activeProviders)
        {
            if (provider.IsInitialized)
            {
                provider.TrackPlayerAction(actionType, context);
            }
        }
    }
    
    public void ClearEventBuffer()
    {
        eventBuffer.Clear();
        GameLogger.Log("ANALYTICS", "Event buffer cleared");
    }
    
    public void StartNewSession()
    {
        currentSession = new AnalyticsSession();
        GameLogger.Log("ANALYTICS", $"New session started: {currentSession.sessionId}");
    }
    
    public void SwitchProvider(AnalyticsProviderType newProviderType)
    {
        ShutdownAllProviders();
        
        config.selectedProvider = newProviderType;
        InitializeProviders();
    }
    
    private void ShutdownAllProviders()
    {
        foreach (var provider in activeProviders)
        {
            provider.Shutdown();
        }
        activeProviders.Clear();
        currentProvider = null;
    }
    
    private void OnDestroy()
    {
        ShutdownAllProviders();
    }
}

public enum AnalyticsProviderType
{
    None,
    UnityAnalytics,
    GameAnalytics,
    Custom
}
