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
    
    [Header("Persistence Settings")]
    public bool enablePersistence = true;
    public bool autoSaveEnabled = true;
    [Range(10f, 300f)]
    public float autoSaveInterval = 60f;
    [Range(10, 200)]
    public int maxSessionsToKeep = 50;
    
    [Header("Debug")]
    public bool verboseLogging = true;
}