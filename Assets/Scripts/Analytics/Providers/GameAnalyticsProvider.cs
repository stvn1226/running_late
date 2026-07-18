using System.Collections.Generic;
using GameAnalyticsSDK;
using Utils;

public class GameAnalyticsProvider : IAnalyticsProvider
{
    public string ProviderName => "GameAnalytics";
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        // GameAnalytics auto-initializes itself when the GameAnalytics prefab is in the scene.
        // Calling Initialize() manually is only needed if NOT using the prefab.
        // Since we DO have the prefab in the scene, we just mark ourselves as ready.
        // If the prefab is not present, this call will trigger initialization.
        GameAnalytics.Initialize();
        IsInitialized = true;
        GameLogger.Log("ANALYTICS", "GameAnalytics Provider initialized");
    }

    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!IsInitialized) return;

        // GameAnalytics Design Events require colon-separated hierarchy: "Category:Action:Label"
        // Underscores in the root name are not valid for GA design events.
        // We build the event name by replacing underscores with colons for the hierarchy.
        // e.g. "level_start" -> "level:start", "obstacle_hit" -> "obstacle:hit"
        string gaEventName = eventName.Replace("_", ":");
        GameAnalytics.NewDesignEvent(gaEventName);
    }

    public void TrackLevelStart(int seed, string levelType)
    {
        if (!IsInitialized) return;

        // Progression event: World01 = levelType (DualPath/SingleSidewalk), Level01 = seed
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, levelType, seed.ToString());
    }

    public void TrackLevelComplete(int seed, string levelType, float time, bool victory)
    {
        if (!IsInitialized) return;

        GAProgressionStatus status = victory ? GAProgressionStatus.Complete : GAProgressionStatus.Fail;
        // Score is passed in milliseconds (int) — we convert seconds to ms for more precision
        int scoreMs = (int)(time * 1000f);
        GameAnalytics.NewProgressionEvent(status, levelType, seed.ToString(), scoreMs);
    }

    public void TrackPlayerAction(string actionType, Dictionary<string, object> context = null)
    {
        if (!IsInitialized) return;

        // Design event hierarchy: "PlayerAction:jump", "PlayerAction:slide", etc.
        GameAnalytics.NewDesignEvent($"PlayerAction:{actionType}");
    }

    public void SetUserProperty(string propertyName, object value)
    {
        // SetCustomId must be called BEFORE Initialize() to take effect.
        // Log a warning since we can't reliably set it after initialization.
        if (propertyName.ToLower() == "id")
        {
            GameLogger.LogWarning("ANALYTICS", "GA CustomId must be set before initialization. Call GameAnalytics.SetCustomId() before the GA prefab runs Awake.");
        }
        else
        {
            GameLogger.LogWarning("ANALYTICS", $"SetUserProperty '{propertyName}' not supported in GameAnalyticsProvider.");
        }
    }

    public void Shutdown()
    {
        IsInitialized = false;
    }
}