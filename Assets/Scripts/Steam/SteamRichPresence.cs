using System;
using UnityEngine;

#if UNITY_STANDALONE && !UNITY_SERVER
using Steamworks;
#endif

namespace Steam
{
    /// <summary>
    /// Updates Steam Rich Presence so friends on Steam can see live game state
    /// (e.g. "Running Late: Seed #12345", "In Main Menu", "Completed Seed in 00:32").
    /// Cross-platform: fully safe no-op on Android, iOS, and WebGL.
    /// </summary>
    public class SteamRichPresence : MonoBehaviour
    {
        public static SteamRichPresence Instance { get; private set; }

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

        private void Start()
        {
            SetInMenu();
        }

        /// <summary>Set status to Main Menu.</summary>
        public void SetInMenu()
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            if (!SteamManager.IsInitialized) return;
            try
            {
                SteamFriends.SetRichPresence("status", "In Main Menu");
                SteamFriends.SetRichPresence("steam_display", "#Status_Menu");
                GameLogger.Log("STEAM", "Rich Presence updated: Main Menu");
            }
            catch (Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Failed to update Rich Presence: {e.Message}");
            }
#endif
        }

        /// <summary>Set status to actively playing a specific seed.</summary>
        public void SetPlayingLevel(int seed)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            if (!SteamManager.IsInitialized) return;
            try
            {
                SteamFriends.SetRichPresence("status", $"Running Late (Seed #{seed})");
                SteamFriends.SetRichPresence("seed", seed.ToString());
                SteamFriends.SetRichPresence("steam_display", "#Status_Playing");
                GameLogger.Log("STEAM", $"Rich Presence updated: Playing Seed {seed}");
            }
            catch (Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Failed to update Rich Presence: {e.Message}");
            }
#endif
        }

        /// <summary>Set status to Level Complete with time.</summary>
        public void SetLevelCompleted(int seed, string timeFormatted)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            if (!SteamManager.IsInitialized) return;
            try
            {
                SteamFriends.SetRichPresence("status", $"Arrived on time in {timeFormatted}! (Seed #{seed})");
                SteamFriends.SetRichPresence("steam_display", "#Status_Won");
                GameLogger.Log("STEAM", $"Rich Presence updated: Won Seed {seed} in {timeFormatted}");
            }
            catch (Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Failed to update Rich Presence: {e.Message}");
            }
#endif
        }
    }
}
