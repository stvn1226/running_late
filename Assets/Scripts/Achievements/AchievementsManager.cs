using System;
using UnityEngine;

namespace Achievements
{
    /// <summary>
    /// Unified cross-platform Achievement manager for Steam, Mobile (Android/iOS), and Standalone.
    /// Handles unlocking achievements on Steamworks when available, persisting locally via PlayerPrefs,
    /// and reporting events to Analytics.
    /// </summary>
    public class AchievementsManager : MonoBehaviour
    {
        public static AchievementsManager Instance { get; private set; }

        // ─── Achievement ID Constants ──────────────────────────────────────────
        public const string ACH_FIRST_WIN       = "ACH_FIRST_WIN";        // First Day at School (Complete 1st level)
        public const string ACH_PERFECT_TOAST   = "ACH_PERFECT_TOAST";    // Breakfast Intact (Win without losing Toast)
        public const string ACH_SPEED_DEMON     = "ACH_SPEED_DEMON";      // Speed Demon (Win level in under 45s)
        public const string ACH_POWER_COLLECTOR = "ACH_POWER_COLLECTOR";  // Power Hoarder (Collect 5 power-ups in 1 run)
        public const string ACH_KICK_MASTER     = "ACH_KICK_MASTER";      // Obstacle Breaker (Kick 5 obstacles)
        public const string ACH_AIR_GLIDER      = "ACH_AIR_GLIDER";       // Urban Flyer (Use AirFloat slow-fall)

        public static event Action<string, string, string> OnAchievementUnlocked; // (id, title, description)

        private int runPowerUpsCollected = 0;
        private int runKicksPerformed = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameLogger.Log("ACHIEVEMENT", "AchievementsManager initialized");
        }

        /// <summary>Reset run-specific counters when a new level starts.</summary>
        public void ResetRunCounters()
        {
            runPowerUpsCollected = 0;
            runKicksPerformed = 0;
        }

        public void ReportPowerUpCollected()
        {
            runPowerUpsCollected++;
            if (runPowerUpsCollected >= 5)
            {
                Unlock(ACH_POWER_COLLECTOR, "Coleccionista Urbano", "Recoge 5 power-ups en una sola partida");
            }
        }

        public void ReportKickPerformed()
        {
            runKicksPerformed++;
            if (runKicksPerformed >= 5)
            {
                Unlock(ACH_KICK_MASTER, "Rompedor de Obstáculos", "Patea 5 obstáculos en una sola partida");
            }
        }

        public void ReportAirFloatUsed()
        {
            Unlock(ACH_AIR_GLIDER, "Volador Urbano", "Planea en el aire usando el Paraguas");
        }

        /// <summary>
        /// Unlocks an achievement across Steam and local storage.
        /// </summary>
        public void Unlock(string achievementId, string title = "", string desc = "")
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            bool alreadyUnlocked = IsUnlocked(achievementId);
            if (alreadyUnlocked) return;

            // 1. Save locally
            PlayerPrefs.SetInt("ACH_" + achievementId, 1);
            PlayerPrefs.Save();

            // 2. Trigger on Steam if available
#if !UNITY_ANDROID && !UNITY_IOS
            if (SteamManager.IsInitialized)
            {
                try
                {
                    var ach = new Steamworks.Data.Achievement(achievementId);
                    ach.Trigger();
                    GameLogger.Log("ACHIEVEMENT", $"Steam achievement unlocked: {achievementId}");
                }
                catch (Exception e)
                {
                    GameLogger.LogWarning("ACHIEVEMENT", $"Failed to trigger Steam achievement {achievementId}: {e.Message}");
                }
            }
#endif

            // 3. Track in Analytics
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackDesignEvent($"achievement:{achievementId}");
            }

            // 4. Dispatch event for in-game notification UI banner
            OnAchievementUnlocked?.Invoke(achievementId, title, desc);
            GameLogger.Log("ACHIEVEMENT", $"[Achievement Unlocked] {achievementId}: {title}");
        }

        public bool IsUnlocked(string achievementId)
        {
            return PlayerPrefs.GetInt("ACH_" + achievementId, 0) == 1;
        }
    }
}
