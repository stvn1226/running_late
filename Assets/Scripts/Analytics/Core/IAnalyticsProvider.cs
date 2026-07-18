using System.Collections.Generic;

public interface IAnalyticsProvider
{
    string ProviderName { get; }
    bool IsInitialized { get; }
    
    void Initialize();
    void TrackEvent(string eventName, Dictionary<string, object> parameters = null);
    void TrackLevelStart(int seed, string levelType);
    void TrackLevelComplete(int seed, string levelType, float time, bool victory);
    void TrackPlayerAction(string actionType, Dictionary<string, object> context = null);
    void SetUserProperty(string propertyName, object value);
    void Shutdown();
}
