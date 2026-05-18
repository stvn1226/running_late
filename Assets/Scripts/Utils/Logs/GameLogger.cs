using UnityEngine;

public static class GameLogger
{
    // Basic logging methods
    public static void Log(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.Log(message);
#endif
    }
    
    public static void LogWarning(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.LogWarning(message);
#endif
    }
    
    public static void LogError(object message)
    {
        // Errors always show, even in builds
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.LogError(message);
    }
    
    // Enhanced logging methods with system prefixes
    public static void Log(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
        {
            if (DebugManager.Settings.levelDebugOnly && system.ToUpper() != "LEVEL") return;
            Debug.Log($"[{system.ToUpper()}] {message}");
        }
#endif
    }
    
    public static void LogWarning(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
        {
            if (DebugManager.Settings.levelDebugOnly && system.ToUpper() != "LEVEL") return;
            Debug.LogWarning($"[{system.ToUpper()}] {message}");
        }
#endif
    }
    
    public static void LogError(string system, object message)
    {
        // Errors always show, even in builds
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            Debug.LogError($"[{system.ToUpper()}] {message}");
    }
    
    // Special logging methods
    public static void LogVerbose(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableVerboseLogs)
            Debug.Log($"[VERBOSE] {message}");
#endif
    }
    
    public static void LogVerbose(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableVerboseLogs)
            Debug.Log($"[{system.ToUpper()}] [VERBOSE] {message}");
#endif
    }
    
    public static void LogEditor(object message)
    {
#if UNITY_EDITOR
        if (DebugManager.Settings.enableAllLogs)
            Debug.Log($"[EDITOR] {message}");
#endif
    }
    
    public static void LogEditor(string system, object message)
    {
#if UNITY_EDITOR
        if (DebugManager.Settings.enableAllLogs)
            Debug.Log($"[{system.ToUpper()}] [EDITOR] {message}");
#endif
    }
    
    public static void LogPerformance(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enablePerformanceLogs)
            Debug.Log($"[PERF] {message}");
#endif
    }
    
    public static void LogPerformance(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enablePerformanceLogs)
            Debug.Log($"[{system.ToUpper()}] [PERF] {message}");
#endif
    }
    
    // Convenience methods for common systems
    public static void LogPlayer(object message) => DebugManager.LogPlayer(message);
    public static void LogMovement(object message) => DebugManager.LogMovement(message);
    public static void LogPhysics(object message) => DebugManager.LogPhysics(message);
    public static void LogState(object message) => DebugManager.LogState(message);
    public static void LogInput(object message) => DebugManager.LogInput(message);
    public static void LogTrigger(object message) => DebugManager.LogTrigger(message);
    public static void LogCollision(object message) => DebugManager.LogCollision(message);
    public static void LogUI(object message) => DebugManager.LogUI(message);
    public static void LogAnimation(object message) => DebugManager.LogAnimation(message);
    public static void LogAudio(object message) => DebugManager.LogAudio(message);
    public static void LogLevel(object message) => DebugManager.LogLevel(message);
}
