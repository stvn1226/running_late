using System.Collections.Generic;
using UnityEngine;

public class CustomAnalyticsProvider : IAnalyticsProvider
{
    public string ProviderName => "Custom Analytics";
    public bool IsInitialized { get; private set; }
    
    public void Initialize()
    {
        IsInitialized = true;
        GameLogger.Log("ANALYTICS", $"{ProviderName} initialized");
    }
    
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        string paramString = parameters != null ? FormatParameters(parameters) : "no params";
        GameLogger.Log("ANALYTICS", $"Event: {eventName} | {paramString}");
    }
    
    public void TrackLevelStart(int seed, string levelType)
    {
        var parameters = new Dictionary<string, object>
        {
            { "seed", seed },
            { "level_type", levelType },
            { "timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        
        TrackEvent("level_start", parameters);
    }
    
    public void TrackLevelComplete(int seed, string levelType, float time, bool victory)
    {
        var parameters = new Dictionary<string, object>
        {
            { "seed", seed },
            { "level_type", levelType },
            { "completion_time", time },
            { "completion_time_formatted", Utils.TimeRecord.FormatTime(time) },
            { "victory", victory },
            { "timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        
        TrackEvent(victory ? "level_victory" : "level_defeat", parameters);
    }
    
    public void TrackPlayerAction(string actionType, Dictionary<string, object> context = null)
    {
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
    }
    
    public void SetUserProperty(string propertyName, object value)
    {
        GameLogger.Log("ANALYTICS", $"User Property: {propertyName} = {value}");
    }
    
    public void Shutdown()
    {
        IsInitialized = false;
        GameLogger.Log("ANALYTICS", $"{ProviderName} shutdown");
    }
    
    private string FormatParameters(Dictionary<string, object> parameters)
    {
        var parts = new List<string>();
        foreach (var kvp in parameters)
        {
            parts.Add($"{kvp.Key}={kvp.Value}");
        }
        return string.Join(", ", parts);
    }
}
