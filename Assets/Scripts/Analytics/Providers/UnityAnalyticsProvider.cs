using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;

public class UnityAnalyticsProvider : IAnalyticsProvider
{
    private bool isInitialized = false;
    
    public string ProviderName => "Unity Analytics";
    public bool IsInitialized => isInitialized;
    
    public void Initialize()
    {
        if (isInitialized)
        {
            GameLogger.LogWarning("ANALYTICS", "Unity Analytics already initialized");
            return;
        }
        
        isInitialized = true;
        GameLogger.Log("ANALYTICS", "Unity Analytics Provider initialized");
    }
    
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!isInitialized)
        {
            GameLogger.LogWarning("ANALYTICS", "Unity Analytics not initialized. Call Initialize() first.");
            return;
        }
        
        try
        {
            if (parameters == null || parameters.Count == 0)
            {
                AnalyticsService.Instance.RecordEvent(eventName);
            }
            else
            {
                CustomEvent customEvent = new CustomEvent(eventName);
                foreach (var kvp in parameters)
                {
                    customEvent.Add(kvp.Key, kvp.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
            }
            
            GameLogger.LogVerbose("ANALYTICS", $"Event tracked: {eventName}");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error tracking event '{eventName}': {e.Message}");
        }
    }
    
    public void TrackLevelStart(int seed, string levelType)
    {
        if (!isInitialized) return;
        
        try
        {
            LevelStartEvent levelStart = new LevelStartEvent
            {
                Seed = seed,
                LevelType = levelType,
                LevelWidth = LevelStarter.Instance != null ? LevelStarter.Instance.GetLevelWidth() : 0,
                ModuleCount = LevelStarter.Instance != null ? LevelStarter.Instance.GetModuleCount() : 0
            };
            
            AnalyticsService.Instance.RecordEvent(levelStart);
            GameLogger.Log("ANALYTICS", $"Level start tracked: Seed={seed}, Type={levelType}");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error tracking level start: {e.Message}");
        }
    }
    
    public void TrackLevelComplete(int seed, float time, bool victory)
    {
        if (!isInitialized) return;
        
        try
        {
            if (victory)
            {
                bool isPersonalBest = false;
                if (Utils.LeaderboardManager.Instance != null)
                {
                    var existingRecord = Utils.LeaderboardManager.Instance.GetBestTime(seed);
                    isPersonalBest = existingRecord == null || time < existingRecord.time;
                }
                
                LevelCompleteEvent levelComplete = new LevelCompleteEvent
                {
                    Seed = seed,
                    CompletionTime = time,
                    IsPersonalBest = isPersonalBest,
                    LevelType = LevelStarter.Instance != null && LevelStarter.Instance.IsUsingModularGeneration() ? "Modular" : "Procedural"
                };
                
                AnalyticsService.Instance.RecordEvent(levelComplete);
                GameLogger.Log("ANALYTICS", $"Level complete tracked: Time={time:F2}s, PersonalBest={isPersonalBest}");
            }
            else
            {
                LevelDefeatEvent levelDefeat = new LevelDefeatEvent
                {
                    Seed = seed,
                    SurvivalTime = time,
                    DefeatReason = "obstacle_collision"
                };
                
                AnalyticsService.Instance.RecordEvent(levelDefeat);
                GameLogger.Log("ANALYTICS", $"Level defeat tracked: Time={time:F2}s");
            }
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error tracking level complete: {e.Message}");
        }
    }
    
    public void TrackPlayerAction(string actionType, Dictionary<string, object> context = null)
    {
        if (!isInitialized) return;
        
        try
        {
            PlayerActionEvent playerAction = new PlayerActionEvent
            {
                ActionType = actionType
            };
            
            if (context != null)
            {
                if (context.ContainsKey("position_x"))
                    playerAction.PositionX = System.Convert.ToSingle(context["position_x"]);
                
                if (context.ContainsKey("velocity_x"))
                    playerAction.VelocityX = System.Convert.ToSingle(context["velocity_x"]);
                
                if (context.ContainsKey("is_grounded"))
                    playerAction.IsGrounded = System.Convert.ToBoolean(context["is_grounded"]);
            }
            
            AnalyticsService.Instance.RecordEvent(playerAction);
            GameLogger.LogVerbose("ANALYTICS", $"Player action tracked: {actionType}");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error tracking player action: {e.Message}");
        }
    }
    
    public void SetUserProperty(string propertyName, object value)
    {
        if (!isInitialized) return;
        
        GameLogger.LogVerbose("ANALYTICS", $"User property set: {propertyName}={value}");
    }
    
    public void Shutdown()
    {
        if (!isInitialized) return;
        
        try
        {
            AnalyticsService.Instance.Flush();
            isInitialized = false;
            GameLogger.Log("ANALYTICS", "Unity Analytics Provider shutdown");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Error during shutdown: {e.Message}");
        }
    }
}
