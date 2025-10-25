using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Utils;

public class LeaderboardEditorWindow : EditorWindow
{
    private const string LEADERBOARD_KEY = "LocalLeaderboard";
    
    private LeaderboardData leaderboardData;
    private Vector2 scrollPosition;
    private bool showRawJson = false;
    private string rawJson = "";
    
    private enum SortMode
    {
        BySeed,
        ByTime,
        ByTimeDescending
    }
    
    private SortMode currentSortMode = SortMode.ByTime;
    
    [MenuItem("Tools/Leaderboard Viewer")]
    public static void ShowWindow()
    {
        LeaderboardEditorWindow window = GetWindow<LeaderboardEditorWindow>("Leaderboard Viewer");
        window.minSize = new Vector2(400, 300);
        window.LoadLeaderboard();
    }
    
    private void OnEnable()
    {
        LoadLeaderboard();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        DrawHeader();
        
        EditorGUILayout.Space(10);
        
        DrawControls();
        
        EditorGUILayout.Space(10);
        
        if (leaderboardData != null && leaderboardData.records.Count > 0)
        {
            DrawRecordsList();
        }
        else
        {
            DrawEmptyState();
        }
        
        EditorGUILayout.Space(10);
        
        if (showRawJson)
        {
            DrawRawJson();
        }
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("Local Leaderboard", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        if (leaderboardData != null)
        {
            GUILayout.Label($"Total Records: {leaderboardData.records.Count}", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawControls()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            LoadLeaderboard();
        }
        
        EditorGUI.BeginDisabledGroup(leaderboardData == null || leaderboardData.records.Count == 0);
        
        if (GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Leaderboard",
                "Are you sure you want to delete all records?",
                "Yes, Clear All",
                "Cancel"))
            {
                ClearAllRecords();
            }
        }
        
        EditorGUI.EndDisabledGroup();
        
        GUILayout.FlexibleSpace();
        
        GUILayout.Label("Sort by:", GUILayout.Width(55));
        currentSortMode = (SortMode)EditorGUILayout.EnumPopup(currentSortMode, GUILayout.Width(150));
        
        showRawJson = GUILayout.Toggle(showRawJson, "Show JSON", GUILayout.Width(90));
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawRecordsList()
    {
        List<TimeRecord> sortedRecords = GetSortedRecords();
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        DrawTableHeader();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < sortedRecords.Count; i++)
        {
            DrawRecordRow(sortedRecords[i], i);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Seed", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Time", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Raw Time", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Actions", EditorStyles.boldLabel, GUILayout.Width(60));
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawRecordRow(TimeRecord record, int index)
    {
        EditorGUILayout.BeginHorizontal(index % 2 == 0 ? EditorStyles.helpBox : GUIStyle.none);
        
        GUILayout.Label((index + 1).ToString(), GUILayout.Width(30));
        
        GUILayout.Label(record.seed.ToString(), GUILayout.Width(100));
        
        GUILayout.Label(record.GetFormattedTime(), GUILayout.Width(100));
        
        GUILayout.Label($"{record.time:F2}s", EditorStyles.miniLabel, GUILayout.Width(80));
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog(
                "Delete Record",
                $"Delete record for seed {record.seed}?",
                "Delete",
                "Cancel"))
            {
                DeleteRecord(record.seed);
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawEmptyState()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.FlexibleSpace();
        
        GUILayout.Label("No records found", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label("Play the game to create some records!", EditorStyles.centeredGreyMiniLabel);
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }
    
    private void DrawRawJson()
    {
        EditorGUILayout.LabelField("Raw JSON Data:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.TextArea(rawJson, GUILayout.Height(100));
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Copy to Clipboard", GUILayout.Width(150)))
        {
            EditorGUIUtility.systemCopyBuffer = rawJson;
            Debug.Log("JSON copied to clipboard!");
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private List<TimeRecord> GetSortedRecords()
    {
        if (leaderboardData == null || leaderboardData.records == null)
            return new List<TimeRecord>();
        
        switch (currentSortMode)
        {
            case SortMode.BySeed:
                return leaderboardData.records.OrderBy(r => r.seed).ToList();
            
            case SortMode.ByTime:
                return leaderboardData.records.OrderBy(r => r.time).ToList();
            
            case SortMode.ByTimeDescending:
                return leaderboardData.records.OrderByDescending(r => r.time).ToList();
            
            default:
                return leaderboardData.records;
        }
    }
    
    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
        {
            rawJson = PlayerPrefs.GetString(LEADERBOARD_KEY);
            leaderboardData = JsonUtility.FromJson<LeaderboardData>(rawJson);
            
            if (leaderboardData == null)
            {
                leaderboardData = new LeaderboardData();
            }
        }
        else
        {
            leaderboardData = new LeaderboardData();
            rawJson = "{}";
        }
        
        Repaint();
    }
    
    private void SaveLeaderboard()
    {
        rawJson = JsonUtility.ToJson(leaderboardData, true);
        PlayerPrefs.SetString(LEADERBOARD_KEY, rawJson);
        PlayerPrefs.Save();
        Repaint();
    }
    
    private void DeleteRecord(int seed)
    {
        leaderboardData.records.RemoveAll(r => r.seed == seed);
        SaveLeaderboard();
    }
    
    private void ClearAllRecords()
    {
        leaderboardData.records.Clear();
        SaveLeaderboard();
    }
}
