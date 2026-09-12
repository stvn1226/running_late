using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public class LeaderboardManager : MonoBehaviour
    {
        private const string LEADERBOARD_KEY = "LocalLeaderboard";
        
        private LeaderboardData leaderboardData;
        
        public static LeaderboardManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadLeaderboard();
        }

        public static string ResolveLevelName(int seed)
        {
            if (SavedLevelsList.instance != null)
            {
                var saved = SavedLevelsList.instance.GetSavedLevels()?.Find(l => l.savedLevelSeed == seed);
                if (saved != null && !string.IsNullOrEmpty(saved.savedLevelName) && saved.savedLevelName != seed.ToString())
                {
                    return saved.savedLevelName;
                }
            }

            string key = $"SavedLevel_{seed}_Name";
            if (PlayerPrefs.HasKey(key))
            {
                string name = PlayerPrefs.GetString(key);
                if (!string.IsNullOrEmpty(name) && name != seed.ToString())
                {
                    return name;
                }
            }

            return $"Level {seed}";
        }
        
        public void SaveTime(int seed, float time, string playerName = null, string levelName = null)
        {
            if (leaderboardData == null)
            {
                GameLogger.LogError("LEADERBOARD", "LeaderboardData is null! Cannot save time.");
                return;
            }

            string resolvedPlayer = !string.IsNullOrEmpty(playerName)
                ? playerName
                : (SteamManager.IsInitialized ? SteamManager.GetPlayerName() : "You");

            string resolvedLevelName = !string.IsNullOrEmpty(levelName)
                ? levelName
                : ResolveLevelName(seed);

            TimeRecord existingRecord = leaderboardData.records.Find(r => r.seed == seed);
            
            if (existingRecord != null)
            {
                if (time < existingRecord.time)
                {
                    existingRecord.time = time;
                    existingRecord.playerName = resolvedPlayer;
                    existingRecord.levelName = resolvedLevelName;
                    GameLogger.Log("LEADERBOARD", $"New best time for seed {seed} ({resolvedLevelName}): {existingRecord.GetFormattedTime()}");
                }
            }
            else
            {
                TimeRecord newRecord = new TimeRecord(time, seed, resolvedPlayer, resolvedLevelName);
                leaderboardData.records.Add(newRecord);
                GameLogger.Log("LEADERBOARD", $"First time for seed {seed} ({resolvedLevelName}): {newRecord.GetFormattedTime()}");
            }
            
            SaveLeaderboard();
        }
        
        public TimeRecord GetBestTime(int seed)
        {
            return leaderboardData.records.Find(r => r.seed == seed);
        }
        
        public bool HasRecord(int seed)
        {
            return leaderboardData.records.Any(r => r.seed == seed);
        }
        
        public List<TimeRecord> GetTopRecords(int count = 10)
        {
            return leaderboardData.records
                .OrderBy(r => r.time)
                .Take(count)
                .ToList();
        }
        
        public List<TimeRecord> GetAllRecords()
        {
            return new List<TimeRecord>(leaderboardData.records);
        }
        
        public int GetTotalRecordsCount()
        {
            return leaderboardData.records.Count;
        }
        
        public void ClearAllRecords()
        {
            leaderboardData.records.Clear();
            SaveLeaderboard();
            GameLogger.Log("LEADERBOARD", "All records cleared");
        }
        
        public void DeleteRecord(int seed)
        {
            leaderboardData.records.RemoveAll(r => r.seed == seed);
            SaveLeaderboard();
            GameLogger.Log("LEADERBOARD", $"Record for seed {seed} deleted");
        }
        
        private void SaveLeaderboard()
        {
            string json = JsonUtility.ToJson(leaderboardData, true);
            PlayerPrefs.SetString(LEADERBOARD_KEY, json);
            PlayerPrefs.Save();
        }
        
        private void LoadLeaderboard()
        {
            if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
            {
                string json = PlayerPrefs.GetString(LEADERBOARD_KEY);
                try
                {
                    leaderboardData = JsonUtility.FromJson<LeaderboardData>(json);
                    
                    if (leaderboardData == null)
                    {
                        GameLogger.LogWarning("LEADERBOARD", "Failed to deserialize leaderboard data, creating new");
                        leaderboardData = new LeaderboardData();
                    }
                    else if (leaderboardData.records == null)
                    {
                        GameLogger.LogWarning("LEADERBOARD", "Records list is null, initializing");
                        leaderboardData.records = new List<TimeRecord>();
                    }
                    
                    GameLogger.Log("LEADERBOARD", $"Loaded {leaderboardData.records.Count} records");
                }
                catch (System.Exception e)
                {
                    GameLogger.LogError("LEADERBOARD", $"Error loading leaderboard: {e.Message}. Creating new.");
                    leaderboardData = new LeaderboardData();
                }
            }
            else
            {
                leaderboardData = new LeaderboardData();
                GameLogger.Log("LEADERBOARD", "Created new leaderboard");
            }
        }
    }
}