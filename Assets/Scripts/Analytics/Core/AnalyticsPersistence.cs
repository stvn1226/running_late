using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

[Serializable]
public class AnalyticsSessionContainer
{
    public List<SerializedAnalyticsSession> sessions = new List<SerializedAnalyticsSession>();
}

[Serializable]
public class SerializedAnalyticsSession
{
    public string sessionId;
    public string sessionStartTime;
    public string sessionEndTime;
    public float sessionDuration;
    public List<SerializedAnalyticsEvent> events = new List<SerializedAnalyticsEvent>();
}

[Serializable]
public class SerializedAnalyticsEvent
{
    public string eventName;
    public string timestamp;
    public string providerName;
    public float sessionTime;
    public List<SerializedParameter> parameters = new List<SerializedParameter>();
}

[Serializable]
public class SerializedParameter
{
    public string key;
    public string value;
}

public class AnalyticsPersistence : MonoBehaviour
{
    [Header("Persistence Settings")]
    [SerializeField] private bool enablePersistence = true;
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private int maxSessionsToKeep = 50;
    
    [Header("File Settings")]
    [SerializeField] private string saveDirectory = "AnalyticsData";
    [SerializeField] private string filePrefix = "analytics_session_";
    
    private float lastSaveTime;
    private string currentSessionFilePath;
    private string persistentDataPath;
    
    public static AnalyticsPersistence Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        persistentDataPath = Path.Combine(Application.persistentDataPath, saveDirectory);
        
        if (enablePersistence)
        {
            EnsureDirectoryExists();
        }
    }
    
    private void Update()
    {
        if (!enablePersistence || !autoSave) return;
        
        if (Time.time - lastSaveTime >= autoSaveInterval)
        {
            SaveCurrentSession();
            lastSaveTime = Time.time;
        }
    }
    
    private void OnApplicationQuit()
    {
        if (enablePersistence)
        {
            SaveCurrentSession(true);
        }
    }
    
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(persistentDataPath))
        {
            Directory.CreateDirectory(persistentDataPath);
            GameLogger.Log("ANALYTICS", $"Created analytics directory: {persistentDataPath}");
        }
    }
    
    public void SaveCurrentSession(bool isSessionEnd = false)
    {
        if (AnalyticsManager.Instance == null || AnalyticsManager.Instance.CurrentSession == null)
            return;
        
        var session = AnalyticsManager.Instance.CurrentSession;
        
        if (session.events.Count == 0)
        {
            GameLogger.Log("ANALYTICS", "No events to save, skipping session save");
            return;
        }
        
        var serializedSession = ConvertToSerializedSession(session, isSessionEnd);
        
        if (string.IsNullOrEmpty(currentSessionFilePath))
        {
            currentSessionFilePath = Path.Combine(
                persistentDataPath, 
                $"{filePrefix}{session.sessionId}.json"
            );
        }
        
        try
        {
            string json = JsonUtility.ToJson(serializedSession, true);
            File.WriteAllText(currentSessionFilePath, json);
            
            GameLogger.Log("ANALYTICS", $"Session saved: {Path.GetFileName(currentSessionFilePath)} ({session.events.Count} events)");
            
            CleanupOldSessions();
        }
        catch (Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to save session: {e.Message}");
        }
    }
    
    private SerializedAnalyticsSession ConvertToSerializedSession(AnalyticsSession session, bool isSessionEnd)
    {
        var serialized = new SerializedAnalyticsSession
        {
            sessionId = session.sessionId,
            sessionStartTime = session.sessionStartTime,
            sessionEndTime = isSessionEnd ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "In Progress",
            sessionDuration = Time.realtimeSinceStartup
        };
        
        foreach (var evt in session.events)
        {
            var serializedEvent = new SerializedAnalyticsEvent
            {
                eventName = evt.eventName,
                timestamp = evt.timestamp,
                providerName = evt.providerName,
                sessionTime = evt.sessionTime
            };
            
            if (evt.parameters != null)
            {
                foreach (var param in evt.parameters)
                {
                    serializedEvent.parameters.Add(new SerializedParameter
                    {
                        key = param.Key,
                        value = param.Value?.ToString() ?? "null"
                    });
                }
            }
            
            serialized.events.Add(serializedEvent);
        }
        
        return serialized;
    }
    
    public List<SerializedAnalyticsSession> LoadAllSessions()
    {
        var sessions = new List<SerializedAnalyticsSession>();
        
        if (!Directory.Exists(persistentDataPath))
            return sessions;
        
        try
        {
            var files = Directory.GetFiles(persistentDataPath, $"{filePrefix}*.json");
            
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var session = JsonUtility.FromJson<SerializedAnalyticsSession>(json);
                    sessions.Add(session);
                }
                catch (Exception e)
                {
                    GameLogger.LogError("ANALYTICS", $"Failed to load session from {Path.GetFileName(file)}: {e.Message}");
                }
            }
            
            GameLogger.Log("ANALYTICS", $"Loaded {sessions.Count} sessions from disk");
        }
        catch (Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to load sessions: {e.Message}");
        }
        
        return sessions;
    }
    
    public SerializedAnalyticsSession LoadSession(string sessionId)
    {
        string filePath = Path.Combine(persistentDataPath, $"{filePrefix}{sessionId}.json");
        
        if (!File.Exists(filePath))
        {
            GameLogger.LogWarning("ANALYTICS", $"Session file not found: {sessionId}");
            return null;
        }
        
        try
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SerializedAnalyticsSession>(json);
        }
        catch (Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to load session {sessionId}: {e.Message}");
            return null;
        }
    }
    
    public void DeleteSession(string sessionId)
    {
        string filePath = Path.Combine(persistentDataPath, $"{filePrefix}{sessionId}.json");
        
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                GameLogger.Log("ANALYTICS", $"Deleted session: {sessionId}");
            }
            catch (Exception e)
            {
                GameLogger.LogError("ANALYTICS", $"Failed to delete session {sessionId}: {e.Message}");
            }
        }
    }
    
    public void DeleteAllSessions()
    {
        if (!Directory.Exists(persistentDataPath))
            return;
        
        try
        {
            var files = Directory.GetFiles(persistentDataPath, $"{filePrefix}*.json");
            
            foreach (var file in files)
            {
                File.Delete(file);
            }
            
            GameLogger.Log("ANALYTICS", $"Deleted {files.Length} session files");
        }
        catch (Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to delete sessions: {e.Message}");
        }
    }
    
    private void CleanupOldSessions()
    {
        try
        {
            var files = Directory.GetFiles(persistentDataPath, $"{filePrefix}*.json");
            
            if (files.Length <= maxSessionsToKeep)
                return;
            
            var fileInfos = new List<FileInfo>();
            foreach (var file in files)
            {
                fileInfos.Add(new FileInfo(file));
            }
            
            fileInfos.Sort((a, b) => a.CreationTime.CompareTo(b.CreationTime));
            
            int filesToDelete = fileInfos.Count - maxSessionsToKeep;
            
            for (int i = 0; i < filesToDelete; i++)
            {
                fileInfos[i].Delete();
                GameLogger.Log("ANALYTICS", $"Deleted old session: {fileInfos[i].Name}");
            }
        }
        catch (Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to cleanup old sessions: {e.Message}");
        }
    }
    
    public string GetPersistentDataPath()
    {
        return persistentDataPath;
    }
    
    public int GetSessionCount()
    {
        if (!Directory.Exists(persistentDataPath))
            return 0;
        
        return Directory.GetFiles(persistentDataPath, $"{filePrefix}*.json").Length;
    }
    
    public void ExportAllSessionsToCSV(string filePath)
    {
        var sessions = LoadAllSessions();
        
        if (sessions.Count == 0)
        {
            GameLogger.LogWarning("ANALYTICS", "No sessions to export");
            return;
        }
        
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Session ID,Session Start,Session End,Duration,Event Name,Event Time,Provider,Parameters");
                
                foreach (var session in sessions)
                {
                    foreach (var evt in session.events)
                    {
                        string parameters = string.Join("; ", evt.parameters.ConvertAll(p => $"{p.key}={p.value}"));
                        
                        writer.WriteLine($"{session.sessionId},{session.sessionStartTime},{session.sessionEndTime}," +
                                       $"{session.sessionDuration:F2},{evt.eventName},{evt.timestamp}," +
                                       $"{evt.providerName},\"{parameters}\"");
                    }
                }
            }
            
            GameLogger.Log("ANALYTICS", $"Exported {sessions.Count} sessions to CSV: {filePath}");
        }
        catch (Exception e)
        {
            GameLogger.LogError("ANALYTICS", $"Failed to export CSV: {e.Message}");
        }
    }
    
    public void OpenPersistentDataFolder()
    {
        if (Directory.Exists(persistentDataPath))
        {
            Application.OpenURL("file://" + persistentDataPath);
        }
        else
        {
            GameLogger.LogWarning("ANALYTICS", "Persistent data folder doesn't exist yet");
        }
    }
}
