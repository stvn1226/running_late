using UnityEngine;

[CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "Running Late/Analytics Config")]
public class AnalyticsConfig : ScriptableObject
{
    [Header("General Settings")]
    public bool enableAnalytics = true;
    public AnalyticsProviderType selectedProvider = AnalyticsProviderType.Custom;
    
    [Header("Event Tracking")]
    public bool trackLevelEvents = true;
    public bool trackPlayerActions = true;
    public bool trackUIEvents = true;
    public bool trackPerformance = false;
    
    [Header("Debug")]
    public bool verboseLogging = true;
}