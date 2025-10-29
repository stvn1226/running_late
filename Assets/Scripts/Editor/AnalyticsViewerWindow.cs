using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class AnalyticsViewerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private Vector2 statsScrollPosition;
    private string searchFilter = "";
    private bool autoScroll = true;
    private bool showParameters = true;
    private bool showTimestamps = true;
    private bool showProvider = true;
    private int selectedTab = 0;
    private string[] tabNames = { "Events", "Statistics", "Session Info", "History" };
    
    private Dictionary<string, bool> eventTypeFilters = new Dictionary<string, bool>();
    private GUIStyle eventStyle;
    private GUIStyle headerStyle;
    private GUIStyle parameterStyle;
    
    private bool stylesInitialized = false;
    
    private Vector2 historyScrollPosition;
    private List<SerializedAnalyticsSession> loadedSessions;
    private SerializedAnalyticsSession selectedHistorySession;

    
    [MenuItem("Tools/Running Late/Analytics Viewer")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnalyticsViewerWindow>("Analytics Viewer");
        window.minSize = new Vector2(600, 400);
    }
    
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        eventStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(5, 5, 2, 2)
        };
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.8f, 0.8f, 1f) }
        };
        
        parameterStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            wordWrap = true,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        
        stylesInitialized = true;
    }
    
    private void OnGUI()
    {
        InitializeStyles();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Analytics Viewer only works in Play Mode", MessageType.Info);
            return;
        }
        
        if (AnalyticsManager.Instance == null)
        {
            EditorGUILayout.HelpBox("AnalyticsManager not found in scene", MessageType.Warning);
            return;
        }
        
        DrawToolbar();
        
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        
        EditorGUILayout.Space(5);
        
        switch (selectedTab)
        {
            case 0:
                DrawEventsTab();
                break;
            case 1:
                DrawStatisticsTab();
                break;
            case 2:
                DrawSessionInfoTab();
                break;
            case 3:
                DrawHistoryTab();
                break;
        }
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Clear Events", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("Clear Events", 
                "Are you sure you want to clear all tracked events?", "Yes", "Cancel"))
            {
                AnalyticsManager.Instance.ClearEventBuffer();
            }
        }
        
        if (GUILayout.Button("New Session", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            AnalyticsManager.Instance.StartNewSession();
            AnalyticsManager.Instance.ClearEventBuffer();
        }
        
        if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ExportToJSON();
        }
        
        if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ExportToCSV();
        }
        
        GUILayout.FlexibleSpace();
        
        autoScroll = GUILayout.Toggle(autoScroll, "Auto Scroll", EditorStyles.toolbarButton, GUILayout.Width(80));
        
        GUILayout.Label($"Events: {AnalyticsManager.Instance.EventBuffer.Count}", EditorStyles.toolbarButton);
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawEventsTab()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        showTimestamps = EditorGUILayout.ToggleLeft("Show Timestamps", showTimestamps, GUILayout.Width(150));
        showProvider = EditorGUILayout.ToggleLeft("Show Provider", showProvider, GUILayout.Width(150));
        showParameters = EditorGUILayout.ToggleLeft("Show Parameters", showParameters, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        DrawEventFilters();
        
        EditorGUILayout.Space(5);
        
        var events = AnalyticsManager.Instance.EventBuffer;
        var filteredEvents = FilterEvents(events);
        
        if (autoScroll)
        {
            scrollPosition = new Vector2(0, float.MaxValue);
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (filteredEvents.Count == 0)
        {
            EditorGUILayout.HelpBox("No events tracked yet", MessageType.Info);
        }
        else
        {
            foreach (var evt in filteredEvents)
            {
                DrawEventRecord(evt);
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawEventFilters()
    {
        var events = AnalyticsManager.Instance.EventBuffer;
        var uniqueEventTypes = events.Select(e => e.eventName).Distinct().ToList();
        
        if (uniqueEventTypes.Count == 0) return;
        
        EditorGUILayout.LabelField("Filter by Event Type:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("All", GUILayout.Width(50)))
        {
            foreach (var eventType in uniqueEventTypes)
            {
                eventTypeFilters[eventType] = true;
            }
        }
        
        if (GUILayout.Button("None", GUILayout.Width(50)))
        {
            foreach (var eventType in uniqueEventTypes)
            {
                eventTypeFilters[eventType] = false;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        int columns = Mathf.Max(1, Mathf.FloorToInt(position.width / 200));
        int currentColumn = 0;
        
        EditorGUILayout.BeginHorizontal();
        
        foreach (var eventType in uniqueEventTypes)
        {
            if (!eventTypeFilters.ContainsKey(eventType))
            {
                eventTypeFilters[eventType] = true;
            }
            
            eventTypeFilters[eventType] = EditorGUILayout.ToggleLeft(
                $"{eventType} ({events.Count(e => e.eventName == eventType)})", 
                eventTypeFilters[eventType],
                GUILayout.Width(180)
            );
            
            currentColumn++;
            if (currentColumn >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                currentColumn = 0;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    
    private void DrawHistoryTab()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh History", GUILayout.Width(120)))
        {
            LoadSessionHistory();
        }
        
        if (GUILayout.Button("Open Data Folder", GUILayout.Width(120)))
        {
            if (AnalyticsPersistence.Instance != null)
            {
                AnalyticsPersistence.Instance.OpenPersistentDataFolder();
            }
            else
            {
                string path = Path.Combine(Application.persistentDataPath, "AnalyticsData");
                if (Directory.Exists(path))
                {
                    Application.OpenURL("file://" + path);
                }
            }
        }
        
        if (GUILayout.Button("Export All to CSV", GUILayout.Width(120)))
        {
            ExportAllHistoryToCSV();
        }
        
        if (GUILayout.Button("Delete All", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("Delete All Sessions", 
                "Are you sure you want to delete all saved analytics sessions?", "Delete", "Cancel"))
            {
                if (AnalyticsPersistence.Instance != null)
                {
                    AnalyticsPersistence.Instance.DeleteAllSessions();
                    LoadSessionHistory();
                }
            }
        }
        
        GUILayout.FlexibleSpace();
        
        if (loadedSessions != null)
        {
            GUILayout.Label($"Total Sessions: {loadedSessions.Count}", EditorStyles.boldLabel);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if (loadedSessions == null)
        {
            LoadSessionHistory();
        }
        
        if (loadedSessions == null || loadedSessions.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved sessions found", MessageType.Info);
            return;
        }
        
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        EditorGUILayout.LabelField("Saved Sessions", EditorStyles.boldLabel);
        
        historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition);
        
        foreach (var session in loadedSessions)
        {
            bool isSelected = selectedHistorySession == session;
            
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button(isSelected ? "▼" : "▶", GUILayout.Width(20)))
            {
                selectedHistorySession = isSelected ? null : session;
            }
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Session: {session.sessionStartTime}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Events: {session.events.Count} | Duration: {FormatDuration(session.sessionDuration)}");
            EditorGUILayout.LabelField($"Status: {session.sessionEndTime}");
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Delete Session", 
                    $"Delete session from {session.sessionStartTime}?", "Delete", "Cancel"))
                {
                    if (AnalyticsPersistence.Instance != null)
                    {
                        AnalyticsPersistence.Instance.DeleteSession(session.sessionId);
                        LoadSessionHistory();
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical();
        
        if (selectedHistorySession != null)
        {
            DrawSessionDetails(selectedHistorySession);
        }
        else
        {
            EditorGUILayout.HelpBox("Select a session to view details", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawSessionDetails(SerializedAnalyticsSession session)
    {
        EditorGUILayout.LabelField("Session Details", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Session ID: {session.sessionId}");
        EditorGUILayout.LabelField($"Started: {session.sessionStartTime}");
        EditorGUILayout.LabelField($"Ended: {session.sessionEndTime}");
        EditorGUILayout.LabelField($"Duration: {FormatDuration(session.sessionDuration)}");
        EditorGUILayout.LabelField($"Total Events: {session.events.Count}");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        
        var eventCounts = new Dictionary<string, int>();
        foreach (var evt in session.events)
        {
            if (!eventCounts.ContainsKey(evt.eventName))
                eventCounts[evt.eventName] = 0;
            eventCounts[evt.eventName]++;
        }
        
        foreach (var kvp in eventCounts.OrderByDescending(x => x.Value))
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));
            EditorGUILayout.LabelField($"{kvp.Value} events", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void LoadSessionHistory()
    {
        if (AnalyticsPersistence.Instance != null)
        {
            loadedSessions = AnalyticsPersistence.Instance.LoadAllSessions();
            loadedSessions.Sort((a, b) => b.sessionStartTime.CompareTo(a.sessionStartTime));
        }
        else
        {
            string path = Path.Combine(Application.persistentDataPath, "AnalyticsData");
            if (Directory.Exists(path))
            {
                loadedSessions = new List<SerializedAnalyticsSession>();
                var files = Directory.GetFiles(path, "analytics_session_*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var session = JsonUtility.FromJson<SerializedAnalyticsSession>(json);
                        loadedSessions.Add(session);
                    }
                    catch { }
                }
                
                loadedSessions.Sort((a, b) => b.sessionStartTime.CompareTo(a.sessionStartTime));
            }
        }
    }

    private void ExportAllHistoryToCSV()
    {
        string path = EditorUtility.SaveFilePanel(
            "Export All Sessions to CSV",
            "",
            $"all_analytics_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv",
            "csv"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        if (AnalyticsPersistence.Instance != null)
        {
            AnalyticsPersistence.Instance.ExportAllSessionsToCSV(path);
        }
        
        EditorUtility.DisplayDialog("Export Complete", $"All sessions exported to:\n{path}", "OK");
    }
    
    private List<AnalyticsEventRecord> FilterEvents(List<AnalyticsEventRecord> events)
    {
        return events.Where(evt => 
        {
            bool matchesSearch = string.IsNullOrEmpty(searchFilter) || 
                                evt.eventName.ToLower().Contains(searchFilter.ToLower()) ||
                                evt.GetParametersAsString().ToLower().Contains(searchFilter.ToLower());
            
            bool matchesFilter = !eventTypeFilters.ContainsKey(evt.eventName) || 
                                eventTypeFilters[evt.eventName];
            
            return matchesSearch && matchesFilter;
        }).ToList();
    }
    
    private void DrawEventRecord(AnalyticsEventRecord evt)
    {
        EditorGUILayout.BeginVertical(eventStyle);
        
        EditorGUILayout.BeginHorizontal();
        
        Color eventColor = GetEventColor(evt.eventName);
        GUI.color = eventColor;
        GUILayout.Label("●", headerStyle, GUILayout.Width(15));
        GUI.color = Color.white;
        
        EditorGUILayout.LabelField(evt.eventName, headerStyle, GUILayout.MinWidth(150));
        
        if (showTimestamps)
        {
            GUILayout.Label($"[{evt.timestamp}]", EditorStyles.miniLabel, GUILayout.Width(100));
        }
        
        if (showProvider)
        {
            GUILayout.Label($"({evt.providerName})", EditorStyles.miniLabel, GUILayout.Width(120));
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            CopyEventToClipboard(evt);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (showParameters && evt.parameters != null && evt.parameters.Count > 0)
        {
            EditorGUILayout.Space(3);
            
            foreach (var param in evt.parameters)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.LabelField($"{param.Key}:", parameterStyle, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{param.Value}", parameterStyle);
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStatisticsTab()
    {
        if (AnalyticsManager.Instance.CurrentSession == null)
        {
            EditorGUILayout.HelpBox("No active session", MessageType.Info);
            return;
        }
        
        var session = AnalyticsManager.Instance.CurrentSession;
        var eventCounts = session.GetEventCountByType();
        
        EditorGUILayout.LabelField("Event Statistics", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Total Events: {session.totalEvents}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Unique Event Types: {eventCounts.Count}");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        statsScrollPosition = EditorGUILayout.BeginScrollView(statsScrollPosition);
        
        var sortedEvents = eventCounts.OrderByDescending(kvp => kvp.Value);
        
        foreach (var kvp in sortedEvents)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            Color eventColor = GetEventColor(kvp.Key);
            GUI.color = eventColor;
            GUILayout.Label("■", GUILayout.Width(15));
            GUI.color = Color.white;
            
            EditorGUILayout.LabelField(kvp.Key, GUILayout.MinWidth(200));
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField($"{kvp.Value} events", EditorStyles.boldLabel, GUILayout.Width(100));
            
            float percentage = (float)kvp.Value / session.totalEvents * 100f;
            EditorGUILayout.LabelField($"({percentage:F1}%)", GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawSessionInfoTab()
    {
        if (AnalyticsManager.Instance.CurrentSession == null)
        {
            EditorGUILayout.HelpBox("No active session", MessageType.Info);
            return;
        }
        
        var session = AnalyticsManager.Instance.CurrentSession;
        
        EditorGUILayout.LabelField("Session Information", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Session ID:", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(session.sessionId, EditorStyles.textField, GUILayout.Height(20));
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField($"Started: {session.sessionStartTime}");
        EditorGUILayout.LabelField($"Duration: {FormatDuration(Time.realtimeSinceStartup)}");
        EditorGUILayout.LabelField($"Total Events: {session.totalEvents}");
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        if (session.events.Count > 0)
        {
            EditorGUILayout.LabelField("First Event:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var firstEvent = session.events.First();
            EditorGUILayout.LabelField($"Type: {firstEvent.eventName}");
            EditorGUILayout.LabelField($"Time: {firstEvent.timestamp}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Last Event:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var lastEvent = session.events.Last();
            EditorGUILayout.LabelField($"Type: {lastEvent.eventName}");
            EditorGUILayout.LabelField($"Time: {lastEvent.timestamp}");
            EditorGUILayout.EndVertical();
        }
    }
    
    private Color GetEventColor(string eventName)
    {
        int hash = eventName.GetHashCode();
        Random.InitState(hash);
        return new Color(
            Random.Range(0.5f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(0.5f, 1f)
        );
    }
    
    private void CopyEventToClipboard(AnalyticsEventRecord evt)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Event: {evt.eventName}");
        sb.AppendLine($"Timestamp: {evt.timestamp}");
        sb.AppendLine($"Provider: {evt.providerName}");
        
        if (evt.parameters != null && evt.parameters.Count > 0)
        {
            sb.AppendLine("Parameters:");
            foreach (var param in evt.parameters)
            {
                sb.AppendLine($"  {param.Key}: {param.Value}");
            }
        }
        
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Event copied to clipboard");
    }
    
    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel(
            "Export Analytics to JSON",
            "",
            $"analytics_{System.DateTime.Now:yyyyMMdd_HHmmss}.json",
            "json"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        var session = AnalyticsManager.Instance.CurrentSession;
        string json = JsonUtility.ToJson(session, true);
        System.IO.File.WriteAllText(path, json);
        
        Debug.Log($"Analytics exported to: {path}");
        EditorUtility.DisplayDialog("Export Complete", $"Analytics data exported to:\n{path}", "OK");
    }
    
    private void ExportToCSV()
    {
        string path = EditorUtility.SaveFilePanel(
            "Export Analytics to CSV",
            "",
            $"analytics_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv",
            "csv"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Timestamp,Event Name,Provider,Session Time,Parameters");
        
        foreach (var evt in AnalyticsManager.Instance.EventBuffer)
        {
            string parameters = evt.GetParametersAsString().Replace(",", ";");
            csv.AppendLine($"{evt.timestamp},{evt.eventName},{evt.providerName},{evt.sessionTime:F2},{parameters}");
        }
        
        System.IO.File.WriteAllText(path, csv.ToString());
        
        Debug.Log($"Analytics exported to: {path}");
        EditorUtility.DisplayDialog("Export Complete", $"Analytics data exported to:\n{path}", "OK");
    }
    
    private string FormatDuration(float seconds)
    {
        int hours = Mathf.FloorToInt(seconds / 3600f);
        int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        
        if (hours > 0)
            return $"{hours:D2}:{minutes:D2}:{secs:D2}";
        else
            return $"{minutes:D2}:{secs:D2}";
    }
    
    private void OnInspectorUpdate()
    {
        if (Application.isPlaying && autoScroll)
        {
            Repaint();
        }
    }
}
