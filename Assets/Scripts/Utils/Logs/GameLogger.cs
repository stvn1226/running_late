using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class GameLogger
{
    // Basic logging methods
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            UnityEngine.Debug.Log(message);
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            UnityEngine.Debug.LogWarning(message);
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            UnityEngine.Debug.LogError(message);
#endif
    }
    
    // Enhanced logging methods with system prefixes
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
        {
            if (DebugManager.Settings.levelDebugOnly && system.ToUpper() != "LEVEL") return;
            UnityEngine.Debug.Log($"[{system.ToUpper()}] {message}");
        }
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
        {
            if (DebugManager.Settings.levelDebugOnly && system.ToUpper() != "LEVEL") return;
            UnityEngine.Debug.LogWarning($"[{system.ToUpper()}] {message}");
        }
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableGameLogger)
            UnityEngine.Debug.LogError($"[{system.ToUpper()}] {message}");
#endif
    }
    
    // Special logging methods
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogVerbose(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableVerboseLogs)
            UnityEngine.Debug.Log($"[VERBOSE] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogVerbose(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enableVerboseLogs)
            UnityEngine.Debug.Log($"[{system.ToUpper()}] [VERBOSE] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void LogEditor(object message)
    {
#if UNITY_EDITOR
        if (DebugManager.Settings.enableAllLogs)
            UnityEngine.Debug.Log($"[EDITOR] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void LogEditor(string system, object message)
    {
#if UNITY_EDITOR
        if (DebugManager.Settings.enableAllLogs)
            UnityEngine.Debug.Log($"[{system.ToUpper()}] [EDITOR] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPerformance(object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enablePerformanceLogs)
            UnityEngine.Debug.Log($"[PERF] {message}");
#endif
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPerformance(string system, object message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.Settings.enableAllLogs && DebugManager.Settings.enablePerformanceLogs)
            UnityEngine.Debug.Log($"[{system.ToUpper()}] [PERF] {message}");
#endif
    }
    
    // Convenience methods for common systems
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPlayer(object message) => DebugManager.LogPlayer(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogMovement(object message) => DebugManager.LogMovement(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogPhysics(object message) => DebugManager.LogPhysics(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogState(object message) => DebugManager.LogState(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogInput(object message) => DebugManager.LogInput(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogTrigger(object message) => DebugManager.LogTrigger(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogCollision(object message) => DebugManager.LogCollision(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogUI(object message) => DebugManager.LogUI(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogAnimation(object message) => DebugManager.LogAnimation(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogAudio(object message) => DebugManager.LogAudio(message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogLevel(object message) => DebugManager.LogLevel(message);
}
