using UnityEngine;
using Steamworks;

/// <summary>
/// Manages the Steam SDK lifecycle using Steamworks.NET.
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

        InitializeSteam();
    }

    private void InitializeSteam()
    {
        try
        {
            if (!SteamAPI.Init())
            {
                throw new System.InvalidOperationException("SteamAPI.Init returned false.");
            }

            IsInitialized = true;

            GameLogger.Log("STEAM", $"Steam initialized OK — Player: {SteamFriends.GetPersonaName()} | SteamID: {SteamUser.GetSteamID()}");

            // Pass the SteamID to GameAnalytics as the custom user ID.
            // SetCustomId must be called before GA initializes — guaranteed by Script Execution Order.
            GameAnalyticsSDK.GameAnalytics.SetCustomId(SteamUser.GetSteamID().ToString());
            GameLogger.Log("STEAM", $"SteamID enviado a GameAnalytics: {SteamUser.GetSteamID()}");
        }
        catch (System.Exception e)
        {
            // Steam is not running, or the App ID is wrong.
            // The game will continue normally without Steam features.
            // This is expected on itch.io builds and WebGL.
            IsInitialized = false;
            GameLogger.LogWarning("STEAM", $"Steam init failed (Steam not running or wrong App ID): {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (IsInitialized)
        {
            SteamAPI.Shutdown();
            IsInitialized = false;
            GameLogger.Log("STEAM", "Steam shut down.");
        }
    }

    private void Update()
    {
        if (IsInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    // ─── Public API ───────────────────────────────────────────────

    /// <summary>Returns the player's Steam ID as a string, or null if Steam is unavailable.</summary>
    public static string GetSteamId()
    {
        if (!IsInitialized) return null;
        return SteamUser.GetSteamID().ToString();
    }

    /// <summary>Returns the player's Steam display name, or null if Steam is unavailable.</summary>
    public static string GetPlayerName()
    {
        if (!IsInitialized) return null;
        return SteamFriends.GetPersonaName();
    }

    /// <summary>Returns true if the player owns this app on Steam.</summary>
    public static bool IsOwned()
    {
        if (!IsInitialized) return false;
        return SteamApps.BIsSubscribed();
    }
}
