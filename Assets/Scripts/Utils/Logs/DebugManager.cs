using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class DebugSettings
{
    [Header("🔧 MASTER DEBUG CONTROL")]
    [SerializeField] public bool enableAllLogs = true;
    
    [Header("🗺️ LEVEL DEBUG FOCUS")]
    [Tooltip("When ON, only [LEVEL] logs appear. All other GameLogger tags (HAPTICS, AUDIO, MENU, etc.) are silenced.")]
    [SerializeField] public bool levelDebugOnly = false;
    
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
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPlayer(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enablePlayerDebug)
            UnityEngine.Debug.Log($"[PLAYER] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogMovement(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableMovementDebug)
            UnityEngine.Debug.Log($"[MOVEMENT] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPhysics(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enablePhysicsDebug)
            UnityEngine.Debug.Log($"[PHYSICS] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogState(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableStateDebug)
            UnityEngine.Debug.Log($"[STATE] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogInput(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableInputDebug)
            UnityEngine.Debug.Log($"[INPUT] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogLevel(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableLevelDebug)
            UnityEngine.Debug.Log($"[LEVEL] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogTrigger(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableTriggerDebug)
            UnityEngine.Debug.Log($"[TRIGGER] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogCollision(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableCollisionDebug)
            UnityEngine.Debug.Log($"[COLLISION] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogAudio(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableAudioDebug)
            UnityEngine.Debug.Log($"[AUDIO] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogUI(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableUIDebug)
            UnityEngine.Debug.Log($"[UI] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogAnimation(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableAnimationDebug)
            UnityEngine.Debug.Log($"[ANIMATION] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs)
            UnityEngine.Debug.LogWarning($"[{system.ToUpper()}] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs)
            UnityEngine.Debug.LogError($"[{system.ToUpper()}] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogVerbose(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enableVerboseLogs)
            UnityEngine.Debug.Log($"[{system.ToUpper()}] [VERBOSE] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPerformance(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Settings.enableAllLogs && Settings.enablePerformanceLogs)
            UnityEngine.Debug.Log($"[{system.ToUpper()}] [PERF] {message}");
#endif
    }
    
    [ContextMenu("🔇 Disable All Logs")]
    public void DisableAllLogs()
    {
        debugSettings.enableAllLogs = false;
        Debug.Log("🔇 ALL LOGS DISABLED - This is the last log you'll see");
    }
    
    [ContextMenu("🔊 Enable All Logs")]
    public void EnableAllLogs()
    {
        debugSettings.enableAllLogs = true;
        debugSettings.levelDebugOnly = false;
        Debug.Log("🔊 ALL LOGS ENABLED");
    }
    
    [ContextMenu("🗺️ Enable Level Debug Only (hide all other logs)")]
    public void EnableLevelDebugOnly()
    {
        debugSettings.enableAllLogs = true;
        debugSettings.enableGameLogger = true;
        debugSettings.levelDebugOnly = true;
        Debug.Log("🗺️ LEVEL DEBUG ONLY — only [LEVEL] logs will appear");
    }
    
    [ContextMenu("🗺️ Disable Level Debug Only (restore all logs)")]
    public void DisableLevelDebugOnly()
    {
        debugSettings.levelDebugOnly = false;
        Debug.Log("🗺️ Level Debug Only OFF — all logs restored");
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
        
        if (debugSettings.enableAllLogs)
            Debug.Log("🎮 PLAYER DEBUG ONLY enabled");
    }
    
    [ContextMenu("🔍 Enable Verbose Mode")]
    public void EnableVerboseMode()
    {
        debugSettings.enableVerboseLogs = true;
        if (debugSettings.enableAllLogs)
            Debug.Log("🔍 VERBOSE MODE enabled");
    }
    
    [ContextMenu("⚡ Disable Verbose Mode")]
    public void DisableVerboseMode()
    {
        debugSettings.enableVerboseLogs = false;
        if (debugSettings.enableAllLogs)
            Debug.Log("⚡ VERBOSE MODE disabled");
    }
    
    [ContextMenu("📊 Show Current Settings")]
    public void ShowCurrentSettings()
    {
        if (!debugSettings.enableAllLogs)
        {
            Debug.Log("🔇 ALL LOGS ARE CURRENTLY DISABLED");
            return;
        }
        
        Debug.Log("=== DEBUG MANAGER SETTINGS ===");
        Debug.Log($"Master: {debugSettings.enableAllLogs}");
        Debug.Log($"GameLogger: {debugSettings.enableGameLogger}");
        Debug.Log($"Player: {debugSettings.enablePlayerDebug}");
        Debug.Log($"Movement: {debugSettings.enableMovementDebug}");
        Debug.Log($"Physics: {debugSettings.enablePhysicsDebug}");
        Debug.Log($"State: {debugSettings.enableStateDebug}");
        Debug.Log($"Input: {debugSettings.enableInputDebug}");
        Debug.Log($"Level: {debugSettings.enableLevelDebug}");
        Debug.Log($"Trigger: {debugSettings.enableTriggerDebug}");
        Debug.Log($"UI: {debugSettings.enableUIDebug}");
        Debug.Log("=============================");
    }
}
