using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_STANDALONE && !UNITY_SERVER
using Steamworks;
using Steamworks.Data;
#endif

namespace Steam
{
    public struct LeaderboardEntryData
    {
        public string PlayerName;
        public float TimeSeconds;
        public int GlobalRank;
        public int Seed;
        public int PathMode; // 0 = DualPath, 1 = SingleSidewalk
    }

    /// <summary>
    /// Bridges the game's completion time with Steamworks Global and Friends Leaderboards.
    /// Safely compiles on Mobile and Standalone.
    /// </summary>
    public class SteamLeaderboardBridge : MonoBehaviour
    {
        private static SteamLeaderboardBridge _instance;
        public static SteamLeaderboardBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SteamLeaderboardBridge>();
                    if (_instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("SteamLeaderboardBridge");
                        _instance = go.AddComponent<SteamLeaderboardBridge>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public const string DEFAULT_LEADERBOARD = "GlobalBestTimes";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Submits a level completion time to Steam Leaderboards.
        /// </summary>
        public async Task<bool> SubmitTimeAsync(float timeSeconds, int seed = 0, int pathMode = 0, string leaderboardName = DEFAULT_LEADERBOARD)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            if (!SteamManager.IsInitialized) return false;

            try
            {
                if (seed <= 0)
                {
                    if (LevelStarter.Instance != null) seed = LevelStarter.Instance.GetCurrentSeed();
                    if (seed <= 0 && LevelManager.Instance != null) seed = LevelManager.Instance.LastSeed;
                    if (seed <= 0) seed = UnityEngine.Random.Range(100000, 999999);
                }

                int timeInMilliseconds = Mathf.RoundToInt(timeSeconds * 1000f);
                var leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync(
                    leaderboardName,
                    LeaderboardSort.Ascending,
                    LeaderboardDisplay.TimeMilliSeconds
                );

                if (leaderboard.HasValue)
                {
                    int[] details = new int[] { seed, pathMode };
                    var result = await leaderboard.Value.SubmitScoreAsync(timeInMilliseconds, details);
                    if (result.HasValue)
                    {
                        GameLogger.Log("STEAM", $"Score submitted to Steam Leaderboard '{leaderboardName}': {timeSeconds:F2}s (Rank: #{result.Value.NewGlobalRank}) Seed: {seed} PathMode: {pathMode}");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Failed to submit score to Steam Leaderboard: {e.Message}");
            }
#endif
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// Retrieves top global scores from Steam Leaderboard.
        /// </summary>
        public async Task<LeaderboardEntryData[]> GetTopGlobalScoresAsync(string leaderboardName = DEFAULT_LEADERBOARD, int maxCount = 10)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            if (!SteamManager.IsInitialized) return Array.Empty<LeaderboardEntryData>();

            try
            {
                var leaderboard = await SteamUserStats.FindLeaderboardAsync(leaderboardName);
                if (leaderboard.HasValue)
                {
                    var entries = await leaderboard.Value.GetScoresAsync(maxCount);
                    if (entries != null && entries.Length > 0)
                    {
                        LeaderboardEntryData[] results = new LeaderboardEntryData[entries.Length];
                        for (int i = 0; i < entries.Length; i++)
                        {
                            int entrySeed = (entries[i].Details != null && entries[i].Details.Length > 0 && entries[i].Details[0] > 0)
                                ? entries[i].Details[0]
                                : Mathf.Abs((entries[i].User.Name + entries[i].Score).GetHashCode() % 900000) + 100000;
                            int entryPathMode = (entries[i].Details != null && entries[i].Details.Length > 1) ? entries[i].Details[1] : 0;
                            results[i] = new LeaderboardEntryData
                            {
                                PlayerName = entries[i].User.Name,
                                TimeSeconds = entries[i].Score / 1000f,
                                GlobalRank = entries[i].GlobalRank,
                                Seed = entrySeed,
                                PathMode = entryPathMode
                            };
                        }
                        return results;
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Failed to fetch Steam Leaderboard scores: {e.Message}");
            }
#endif
            await Task.Yield();
            return Array.Empty<LeaderboardEntryData>();
        }

        /// <summary>
        /// Retrieves scores among Steam Friends.
        /// </summary>
        public async Task<LeaderboardEntryData[]> GetFriendsScoresAsync(string leaderboardName = DEFAULT_LEADERBOARD)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            if (!SteamManager.IsInitialized) return Array.Empty<LeaderboardEntryData>();

            try
            {
                var leaderboard = await SteamUserStats.FindLeaderboardAsync(leaderboardName);
                if (leaderboard.HasValue)
                {
                    var entries = await leaderboard.Value.GetScoresFromFriendsAsync();
                    if (entries != null && entries.Length > 0)
                    {
                        LeaderboardEntryData[] results = new LeaderboardEntryData[entries.Length];
                        for (int i = 0; i < entries.Length; i++)
                        {
                            int entrySeed = (entries[i].Details != null && entries[i].Details.Length > 0 && entries[i].Details[0] > 0)
                                ? entries[i].Details[0]
                                : Mathf.Abs((entries[i].User.Name + entries[i].Score).GetHashCode() % 900000) + 100000;
                            int entryPathMode = (entries[i].Details != null && entries[i].Details.Length > 1) ? entries[i].Details[1] : 0;
                            results[i] = new LeaderboardEntryData
                            {
                                PlayerName = entries[i].User.Name,
                                TimeSeconds = entries[i].Score / 1000f,
                                GlobalRank = entries[i].GlobalRank,
                                Seed = entrySeed,
                                PathMode = entryPathMode
                            };
                        }
                        return results;
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Failed to fetch Steam Friends Leaderboard: {e.Message}");
            }
#endif
            await Task.Yield();
            return Array.Empty<LeaderboardEntryData>();
        }
    }
}
