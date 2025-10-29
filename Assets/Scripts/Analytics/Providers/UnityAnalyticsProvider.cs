using System.Collections.Generic;

// Still Empty - Template as example
public class UnityAnalyticsProvider : IAnalyticsProvider
{
    public string ProviderName { get; }
    public bool IsInitialized { get; }
    public void Initialize()
    {
        throw new System.NotImplementedException();
    }

    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        throw new System.NotImplementedException();
    }

    public void TrackLevelStart(int seed, string levelType)
    {
        throw new System.NotImplementedException();
    }

    public void TrackLevelComplete(int seed, float time, bool victory)
    {
        throw new System.NotImplementedException();
    }

    public void TrackPlayerAction(string actionType, Dictionary<string, object> context = null)
    {
        throw new System.NotImplementedException();
    }

    public void SetUserProperty(string propertyName, object value)
    {
        throw new System.NotImplementedException();
    }

    public void Shutdown()
    {
        throw new System.NotImplementedException();
    }
}