using UnityEngine;
#if UNITY_STANDALONE && !UNITY_SERVER
using Steamworks;
#endif
using Steam;

/// <summary>
/// Manages the Steam SDK lifecycle using Facepunch Steamworks.
/// Persists across scenes via DontDestroyOnLoad.
///
/// HOW TO FINISH SETUP:
/// -------------------------------------------------------
/// 1. Replace APP_ID below with your real Steam App ID
///    (get it from https://partner.steamgames.com/)
///
/// 2. Create a file called "steam_appid.txt" in the root
///    of your Unity project (same level as Assets/)
///    with just your App ID number inside, e.g.: 2345678
///
/// 3. In Project Settings -> Script Execution Order,
///    make sure SteamManager runs BEFORE AnalyticsManager
///    (set SteamManager to a lower number, e.g. -100)
/// -------------------------------------------------------
/// </summary>
public class SteamManager : MonoBehaviour
{
    private const uint APP_ID = 4724080;
#if UNITY_STANDALONE && !UNITY_SERVER
    private const bool SteamSupportedOnThisBuild = true;
#else
    private const bool SteamSupportedOnThisBuild = false;
#endif

    public static SteamManager Instance { get; private set; }
    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure sub-systems are present on this persistent manager
        if (GetComponent<SteamRichPresence>() == null)
            gameObject.AddComponent<SteamRichPresence>();

        if (GetComponent<SteamLeaderboardBridge>() == null)
            gameObject.AddComponent<SteamLeaderboardBridge>();

        if (FindObjectOfType<Achievements.AchievementsManager>() == null)
            gameObject.AddComponent<Achievements.AchievementsManager>();

        InitializeSteam();
    }

    private void InitializeSteam()
    {
#if UNITY_STANDALONE && !UNITY_SERVER
        try
        {
            if (SteamClient.IsValid)
            {
                IsInitialized = true;
                return;
            }

            SteamClient.Init(APP_ID);

            IsInitialized = true;

            GameLogger.Log("STEAM", $"Steam initialized OK - Player: {SteamClient.Name} | SteamID: {SteamClient.SteamId}");

            // Pass the SteamID to GameAnalytics as the custom user ID.
            // SetCustomId must be called before GA initializes — guaranteed by Script Execution Order.
            GameAnalyticsSDK.GameAnalytics.SetCustomId(SteamClient.SteamId.ToString());
            GameLogger.Log("STEAM", $"SteamID sent to GameAnalytics: {SteamClient.SteamId}");
            
            Application.quitting += ShutdownSteam;
        }
        catch (System.Exception e)
        {
            // Steam is not running, or another process has the lock, or wrong App ID.
            // The game will continue normally without crashing in offline/standalone mode.
            IsInitialized = false;
            GameLogger.LogWarning("STEAM", $"Steam init skipped. AppId={APP_ID}, Platform={Application.platform}, BuildTarget=Standalone. Error: {e.GetType().Name}: {e.Message}");
        }
#else
        IsInitialized = false;
        GameLogger.Log("STEAM", $"Steam disabled for this build target. Platform={Application.platform}, Supported={SteamSupportedOnThisBuild}");
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ShutdownSteam();
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        ShutdownSteam();
    }

    private void ShutdownSteam()
    {
        if (IsInitialized)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            try
            {
                SteamClient.Shutdown();
                IsInitialized = false;
                GameLogger.Log("STEAM", "Steam safely shut down.");
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Error during Steam shutdown: {e.Message}");
            }
#endif
        }
    }

    private void Update()
    {
        if (IsInitialized)
        {
#if UNITY_STANDALONE && !UNITY_SERVER
            try
            {
                SteamClient.RunCallbacks();
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning("STEAM", $"Error in SteamClient callbacks: {e.Message}");
            }
#endif
        }
    }

    // ─── Public API ───────────────────────────────────────────────

    /// <summary>Returns the player's Steam ID as a string, or null if Steam is unavailable.</summary>
    public static string GetSteamId()
    {
        if (!IsInitialized) return null;
#if UNITY_STANDALONE && !UNITY_SERVER
        return SteamClient.SteamId.ToString();
#else
        return null;
#endif
    }

    /// <summary>Returns the player's Steam display name, or null if Steam is unavailable.</summary>
    public static string GetPlayerName()
    {
        if (!IsInitialized) return null;
#if UNITY_STANDALONE && !UNITY_SERVER
        return SteamClient.Name;
#else
        return null;
#endif
    }

    /// <summary>Returns true if the player owns this app on Steam.</summary>
    public static bool IsOwned()
    {
        if (!IsInitialized) return false;
#if UNITY_STANDALONE && !UNITY_SERVER
        return SteamApps.IsSubscribed;
#else
        return false;
#endif
    }
}
