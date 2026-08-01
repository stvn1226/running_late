using System;
using UnityEngine;

/// <summary>
/// Immutable data describing a completed level that can be shared as a challenge.
///
/// Challenge Code = seed.ToString() exactly. No transformation, no encoding.
/// Route = pathMode display name ("Double Decker" or "Single Decker"), a separate field.
///
/// The internal seed remains untouched throughout the system.
/// </summary>
[Serializable]
public class ChallengeData
{
    // ─── Core Identity ───────────────────────────────────────────────────────────

    /// <summary>The raw seed used by the level generator. Never modified.</summary>
    public int seed;

    /// <summary>
    /// The Challenge Code shown to the player. Always equals seed.ToString().
    /// No multiplications, no encoding, no suffixes.
    /// </summary>
    public string challengeCode;

    /// <summary>The path mode (DualPath / SingleSidewalk) used for the level.</summary>
    public LevelPathMode pathMode;

    /// <summary>Player-facing route label: "Double Decker" or "Single Decker".</summary>
    public string routeName;

    // ─── Time ────────────────────────────────────────────────────────────────────

    public float completionTime;
    public string formattedTime;
    public float bestTime;
    public string formattedBestTime;

    /// <summary>True if this run equals the current personal best for this seed.</summary>
    public bool isPersonalBest;

    /// <summary>True if this is the first time this seed was completed.</summary>
    public bool isFirstClear;

    // ─── Meta ────────────────────────────────────────────────────────────────────

    public DateTime timestamp;

    /// <summary>Ready-to-share text built from ChallengeConfig.shareTextTemplate.</summary>
    public string shareText;

    // ─── Factory ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a ChallengeData from the current level state.
    /// Call AFTER checking isFirstClear and BEFORE saving time to leaderboard.
    /// </summary>
    public static ChallengeData Build(
        int seed,
        LevelPathMode pathMode,
        float completionTime,
        float bestTime,
        bool isPersonalBest,
        bool isFirstClear,
        ChallengeConfig config)
    {
        var data = new ChallengeData
        {
            seed            = seed,
            challengeCode   = seed.ToString(),  // Challenge Code = seed as-is
            pathMode        = pathMode,
            routeName       = GetRouteName(pathMode),
            completionTime  = completionTime,
            formattedTime   = FormatTime(completionTime),
            bestTime        = bestTime,
            formattedBestTime = FormatTime(bestTime),
            isPersonalBest  = isPersonalBest,
            isFirstClear    = isFirstClear,
            timestamp       = DateTime.Now
        };

        if (config != null)
            data.shareText = config.BuildShareText(data);

        return data;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    public static string GetRouteName(LevelPathMode mode) => mode switch
    {
        LevelPathMode.DualPath       => "Double Decker",
        LevelPathMode.SingleSidewalk => "Single Decker",
        _                            => "Unknown"
    };

    public static string FormatTime(float time)
    {
        if (time <= 0f) return "--:--.--";
        int minutes      = Mathf.FloorToInt(time / 60f);
        int seconds      = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }
}
