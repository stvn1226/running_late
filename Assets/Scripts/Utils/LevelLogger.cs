using System;
using System.IO;
using UnityEngine;
using System.Text;

public class LevelLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    public bool enableLogging = true;
    public bool logToConsole = true;
    public bool logToFile = true;
    
    [Header("Log Format")]
    public LogFormat logFormat = LogFormat.CSV;
    public bool includeDetailedStats = true;
    
    [Header("File Settings")]
    public string logFileName = "level_gameplay_log";
    
    private string logFilePath;
    private bool isInitialized = false;
    
    public static LevelLogger Instance { get; private set; }
    
    public enum LogFormat
    {
        CSV,
        TXT,
        JSON
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeLogger();
    }
    
    private void InitializeLogger()
    {
        if (!enableLogging) return;
        
        string extension = logFormat == LogFormat.CSV ? ".csv" : 
                          logFormat == LogFormat.JSON ? ".json" : ".txt";
        
        string fileName = $"{logFileName}_{DateTime.Now:yyyyMMdd}{extension}";
        logFilePath = Path.Combine(Application.persistentDataPath, fileName);
        
        if (logToFile && !File.Exists(logFilePath))
        {
            CreateLogHeader();
        }
        
        isInitialized = true;
        Debug.Log($"Level Logger initialized. Logging to: {logFilePath}");
    }
    
    private void CreateLogHeader()
    {
        if (logFormat == LogFormat.CSV)
        {
            string header = "Timestamp,Session,Seed,GenerationType,LevelWidth,Modules,Buildings," +
                           "LoadTime,TotalGameObjects,BuildingDensity,CameraBoundsWidth," +
                           "ModuleSequence,PlayerSpawnTime,Notes";
            File.WriteAllText(logFilePath, header + Environment.NewLine);
        }
        else if (logFormat == LogFormat.TXT)
        {
            string header = "=== LEVEL GAMEPLAY LOG ===" + Environment.NewLine +
                           $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine +
                           "Format: [Timestamp] Level Data" + Environment.NewLine +
                           new string('=', 50) + Environment.NewLine;
            File.WriteAllText(logFilePath, header);
        }
    }
    
    public void LogLevel(LevelData data)
    {
        if (!enableLogging || !isInitialized) return;
        
        if (logToConsole)
        {
            LogToConsole(data);
        }
        
        if (logToFile)
        {
            LogToFile(data);
        }
    }
    
    private void LogToConsole(LevelData data)
    {
        string logMessage = $"LEVEL PLAYED:\n" +
                           $"  Time: {data.timestamp:HH:mm:ss}\n" +
                           $"  Seed: {data.seed}\n" +
                           $"  Size: {data.levelWidth} units\n" +
                           $"  Modules: {data.moduleCount}\n" +
                           $"  Buildings: {data.buildingCount}\n" +
                           $"  Load Time: {data.loadTime:F2}s\n" +
                           $"  Type: {data.generationType}";
        
        if (includeDetailedStats)
        {
            logMessage += $"\n GameObjects: {data.totalGameObjects}\n" +
                         $" Density: {data.buildingDensity:F1}/unit";
        }
        
        Debug.Log(logMessage);
    }
    
    private void LogToFile(LevelData data)
    {
        string logEntry = "";
        
        switch (logFormat)
        {
            case LogFormat.CSV:
                logEntry = $"{data.timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"{data.sessionId}," +
                          $"{data.seed}," +
                          $"{data.generationType}," +
                          $"{data.levelWidth}," +
                          $"{data.moduleCount}," +
                          $"{data.buildingCount}," +
                          $"{data.loadTime:F3}," +
                          $"{data.totalGameObjects}," +
                          $"{data.buildingDensity:F2}," +
                          $"{data.cameraBoundsWidth}," +
                          $"\"{string.Join(";", data.moduleSequence)}\"," +
                          $"{data.playerSpawnTime:F3}," +
                          $"\"{data.notes}\"";
                break;
                
            case LogFormat.TXT:
                logEntry = $"[{data.timestamp:HH:mm:ss}] " +
                          $"Seed:{data.seed} | {data.generationType} | " +
                          $"W:{data.levelWidth} | M:{data.moduleCount} | " +
                          $"B:{data.buildingCount} | Load:{data.loadTime:F2}s | " +
                          $"Objs:{data.totalGameObjects}";
                if (!string.IsNullOrEmpty(data.notes))
                    logEntry += $" | Notes: {data.notes}";
                break;
                
            case LogFormat.JSON:
                logEntry = JsonUtility.ToJson(data, false);
                break;
        }
        
        try
        {
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
    }
    
    [ContextMenu("Open Log File")]
    public void OpenLogFile()
    {
        if (File.Exists(logFilePath))
        {
            Application.OpenURL("file://" + logFilePath);
        }
        else
        {
            Debug.LogWarning("Log file doesn't exist yet");
        }
    }
    
    [ContextMenu("Show Log Stats")]
    public void ShowLogStats()
    {
        if (!File.Exists(logFilePath)) return;
        
        string[] lines = File.ReadAllLines(logFilePath);
        int totalLevels = logFormat == LogFormat.CSV ? lines.Length - 1 : lines.Length - 4;
        
        Debug.Log($" LOG STATISTICS:\n" +
                 $"  File: {Path.GetFileName(logFilePath)}\n" +
                 $"  Total Levels Logged: {totalLevels}\n" +
                 $"  Session Start: {File.GetCreationTime(logFilePath):yyyy-MM-dd HH:mm}\n" +
                 $"  File Size: {new FileInfo(logFilePath).Length / 1024f:F1} KB");
    }
    
    [ContextMenu("Clear Today's Log")]
    public void ClearTodaysLog()
    {
        if (File.Exists(logFilePath))
        {
            File.Delete(logFilePath);
            CreateLogHeader();
            Debug.Log("Today's log cleared");
        }
    }
}

[System.Serializable]
public class LevelData
{
    public DateTime timestamp;
    public string sessionId;
    public int seed;
    public string generationType;
    public int levelWidth;
    public int moduleCount;
    public int buildingCount;
    public float loadTime;
    public int totalGameObjects;
    public float buildingDensity;
    public int cameraBoundsWidth;
    public string[] moduleSequence;
    public float playerSpawnTime;
    public string notes;
    
    public LevelData()
    {
        timestamp = DateTime.Now;
        sessionId = SystemInfo.deviceUniqueIdentifier.Substring(0, 8);
        moduleSequence = new string[0];
        notes = "";
    }
}
