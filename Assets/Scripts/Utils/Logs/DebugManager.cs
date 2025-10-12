using UnityEngine;

[System.Serializable]
public class DebugSettings
{
    [Header("🔧 MASTER DEBUG CONTROL")]
    [SerializeField] public bool enableAllLogs = true;
    
    [Header("📚 GENERAL SYSTEM DEBUG")]
    [SerializeField] public bool enableGameLogger = true;
    [SerializeField] public bool enableVerboseLogs = false;
    [SerializeField] public bool enablePerformanceLogs = false;
    
    [Header("🎮 PLAYER SYSTEM DEBUG")]
    [SerializeField] public bool enablePlayerDebug = true;
    [SerializeField] public bool enableMovementDebug = true;
    [SerializeField] public bool enablePhysicsDebug = true;
    [SerializeField] public bool enableStateDebug = true;
    [SerializeField] public bool enableInputDebug = false;
    
    [Header("🌍 WORLD SYSTEM DEBUG")]
    [SerializeField] public bool enableLevelDebug = false;
    [SerializeField] public bool enableTriggerDebug = true;
    [SerializeField] public bool enableCollisionDebug = false;
    [SerializeField] public bool enableAudioDebug = false;
    
    [Header("🎯 UI SYSTEM DEBUG")]
    [SerializeField] public bool enableUIDebug = false;
    [SerializeField] public bool enableAnimationDebug = false;
}

public class DebugManager : MonoBehaviour
{
    [Header("CENTRALIZED DEBUG CONTROL")]
    [SerializeField] private DebugSettings debugSettings = new DebugSettings();
    
    public static DebugManager Instance { get; private set; }
    public static DebugSettings Settings => Instance?.debugSettings ?? new DebugSettings();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Player System Logs
    public static void LogPlayer(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enablePlayerDebug)
            Debug.Log($"[PLAYER] {message}");
#endif
    }
    
    public static void LogMovement(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableMovementDebug)
            Debug.Log($"[MOVEMENT] {message}");
#endif
    }
    
    public static void LogPhysics(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enablePhysicsDebug)
            Debug.Log($"[PHYSICS] {message}");
#endif
    }
    
    public static void LogState(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableStateDebug)
            Debug.Log($"[STATE] {message}");
#endif
    }
    
    public static void LogInput(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableInputDebug)
            Debug.Log($"[INPUT] {message}");
#endif
    }
    
    // World System Logs
    public static void LogLevel(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableLevelDebug)
            Debug.Log($"[LEVEL] {message}");
#endif
    }
    
    public static void LogTrigger(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableTriggerDebug)
            Debug.Log($"[TRIGGER] {message}");
#endif
    }
    
    public static void LogCollision(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableCollisionDebug)
            Debug.Log($"[COLLISION] {message}");
#endif
    }
    
    public static void LogAudio(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableAudioDebug)
            Debug.Log($"[AUDIO] {message}");
#endif
    }
    
    // UI System Logs
    public static void LogUI(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableUIDebug)
            Debug.Log($"[UI] {message}");
#endif
    }
    
    public static void LogAnimation(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableAnimationDebug)
            Debug.Log($"[ANIMATION] {message}");
#endif
    }
    
    // Generic System Logs
    public static void LogWarning(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs)
            Debug.LogWarning($"[{system.ToUpper()}] {message}");
#endif
    }
    
    public static void LogError(string system, object message)
    {
        // Errors always show, even in builds
        if (Settings.enableAllLogs)
            Debug.LogError($"[{system.ToUpper()}] {message}");
    }
    
    public static void LogVerbose(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableVerboseLogs)
            Debug.Log($"[{system.ToUpper()}] [VERBOSE] {message}");
#endif
    }
    
    public static void LogPerformance(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enablePerformanceLogs)
            Debug.Log($"[{system.ToUpper()}] [PERF] {message}");
#endif
    }
    
    [ContextMenu("🔇 Disable All Logs")]
    public void DisableAllLogs()
    {
        debugSettings.enableAllLogs = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("🔇 ALL LOGS DISABLED via DebugManager");
#endif
    }
    
    [ContextMenu("🔊 Enable All Logs")]
    public void EnableAllLogs()
    {
        debugSettings.enableAllLogs = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("🔊 ALL LOGS ENABLED via DebugManager");
#endif
    }
    
    [ContextMenu("🎮 Enable Player Debug Only")]
    public void EnablePlayerDebugOnly()
    {
        debugSettings.enablePlayerDebug = true;
        debugSettings.enableMovementDebug = true;
        debugSettings.enablePhysicsDebug = true;
        debugSettings.enableStateDebug = true;
        debugSettings.enableInputDebug = true;
        
        debugSettings.enableLevelDebug = false;
        debugSettings.enableTriggerDebug = false;
        debugSettings.enableCollisionDebug = false;
        debugSettings.enableUIDebug = false;
        debugSettings.enableAnimationDebug = false;
        debugSettings.enableAudioDebug = false;
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("🎮 PLAYER DEBUG ONLY enabled via DebugManager");
#endif
    }
    
    [ContextMenu("🔍 Enable Verbose Mode")]
    public void EnableVerboseMode()
    {
        debugSettings.enableVerboseLogs = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("🔍 VERBOSE MODE enabled via DebugManager");
#endif
    }
}
