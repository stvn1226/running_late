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
        
        public void SaveTime(int seed, float time)
        {
            TimeRecord existingRecord = leaderboardData.records.Find(r => r.seed == seed);
            
            if (existingRecord != null)
            {
                if (time < existingRecord.time)
                {
                    existingRecord.time = time;
                    GameLogger.Log("LEADERBOARD", $"New best time for seed {seed}: {existingRecord.GetFormattedTime()}");
                }
            }
            else
            {
                TimeRecord newRecord = new TimeRecord(time, seed);
                leaderboardData.records.Add(newRecord);
                GameLogger.Log("LEADERBOARD", $"First time for seed {seed}: {newRecord.GetFormattedTime()}");
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
                leaderboardData = JsonUtility.FromJson<LeaderboardData>(json);
                GameLogger.Log("LEADERBOARD", $"Loaded {leaderboardData.records.Count} records");
            }
            else
            {
                leaderboardData = new LeaderboardData();
                GameLogger.Log("LEADERBOARD", "Created new leaderboard");
            }
        }
    }
}