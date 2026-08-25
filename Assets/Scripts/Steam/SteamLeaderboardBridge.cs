using System;
using System.Threading.Tasks;
using UnityEngine;

#if !UNITY_ANDROID && !UNITY_IOS
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
    }

    /// <summary>
    /// Bridges the game's completion time with Steamworks Global and Friends Leaderboards.
    /// Safely compiles on Mobile and Standalone.
    /// </summary>
    public class SteamLeaderboardBridge : MonoBehaviour
    {
        public static SteamLeaderboardBridge Instance { get; private set; }

        public const string DEFAULT_LEADERBOARD = "GlobalBestTimes";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Submits a level completion time to Steam Leaderboards.
        /// </summary>
        public async Task<bool> SubmitTimeAsync(float timeSeconds, string leaderboardName = DEFAULT_LEADERBOARD)
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (!SteamManager.IsInitialized) return false;

            try
            {
                int timeInMilliseconds = Mathf.RoundToInt(timeSeconds * 1000f);
                var leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync(
                    leaderboardName,
                    LeaderboardSort.Ascending,
                    LeaderboardDisplay.TimeMilliSeconds
                );

                if (leaderboard.HasValue)
                {
                    var result = await leaderboard.Value.SubmitScoreAsync(timeInMilliseconds);
                    if (result.HasValue)
                    {
                        GameLogger.Log("STEAM", $"Score submitted to Steam Leaderboard '{leaderboardName}': {timeSeconds:F2}s (Rank: #{result.Value.NewGlobalRank})");
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
#if !UNITY_ANDROID && !UNITY_IOS
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
                            results[i] = new LeaderboardEntryData
                            {
                                PlayerName = entries[i].User.Name,
                                TimeSeconds = entries[i].Score / 1000f,
                                GlobalRank = entries[i].GlobalRank
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
#if !UNITY_ANDROID && !UNITY_IOS
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
                            results[i] = new LeaderboardEntryData
                            {
                                PlayerName = entries[i].User.Name,
                                TimeSeconds = entries[i].Score / 1000f,
                                GlobalRank = entries[i].GlobalRank
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
