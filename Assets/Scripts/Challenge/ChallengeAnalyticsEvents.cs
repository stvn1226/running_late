using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Analytics event constants and tracking helpers for Challenge Sharing.
/// Follows the same pattern as AnalyticsEvents.cs.
///
/// Parameters never include: full file paths, usernames, home directories, or stack traces.
/// Challenge Code = seed as string, Route = route display name (analytics key: "double_decker" / "single_decker").
/// </summary>
public static class ChallengeAnalyticsEvents
{
    // ─── Event Name Constants ─────────────────────────────────────────────────────

    public const string CHALLENGE_CODE_COPIED        = "challenge_code_copied";
    public const string CHALLENGE_CARD_REQUESTED     = "challenge_card_requested";
    public const string CHALLENGE_CARD_CREATED       = "challenge_card_created";
    public const string CHALLENGE_CARD_FAILED        = "challenge_card_failed";
    public const string STEAM_SCREENSHOT_ADDED       = "steam_screenshot_added";
    public const string SOCIAL_LINK_CLICKED          = "social_link_clicked";
    public const string CHALLENGE_STARTED_FROM_CODE  = "challenge_started_from_code";

    // ─── Track Methods ────────────────────────────────────────────────────────────

    /// <summary>Player copied the Challenge Code to clipboard.</summary>
    public static void TrackCodeCopied(ChallengeData data)
    {
        if (AnalyticsManager.Instance == null) return;

        AnalyticsManager.Instance.TrackEvent(CHALLENGE_CODE_COPIED, new Dictionary<string, object>
        {
            { "challenge_code",   data.challengeCode },
            { "route",            GetRouteKey(data.pathMode) },
            { "completion_time",  data.completionTime },
            { "is_personal_best", data.isPersonalBest }
        });
    }

    /// <summary>Player pressed Create Challenge Card.</summary>
    public static void TrackCardRequested(ChallengeData data)
    {
        if (AnalyticsManager.Instance == null) return;

        AnalyticsManager.Instance.TrackEvent(CHALLENGE_CARD_REQUESTED, new Dictionary<string, object>
        {
            { "challenge_code",  data.challengeCode },
            { "route",           GetRouteKey(data.pathMode) },
            { "completion_time", data.completionTime },
            { "platform",        Application.platform.ToString() }
        });
    }

    /// <summary>Challenge Card PNG was successfully saved to disk.</summary>
    public static void TrackCardCreated(
        ChallengeData data, int width, int height, bool steamAvailable, bool usedFallback)
    {
        if (AnalyticsManager.Instance == null) return;

        AnalyticsManager.Instance.TrackEvent(CHALLENGE_CARD_CREATED, new Dictionary<string, object>
        {
            { "challenge_code",       data.challengeCode },
            { "route",                GetRouteKey(data.pathMode) },
            { "completion_time",      data.completionTime },
            { "output_width",         width },
            { "output_height",        height },
            { "steam_available",      steamAvailable },
            { "platform",             Application.platform.ToString() },
            { "storage_fallback_used", usedFallback }
        });
    }

    /// <summary>Challenge Card generation failed at a specific stage.</summary>
    public static void TrackCardFailed(string failureStage, string errorCategory)
    {
        if (AnalyticsManager.Instance == null) return;

        AnalyticsManager.Instance.TrackEvent(CHALLENGE_CARD_FAILED, new Dictionary<string, object>
        {
            { "failure_stage",   failureStage },
            { "error_category",  errorCategory },
            { "platform",        Application.platform.ToString() },
            { "steam_available", SteamManager.IsInitialized }
        });
    }

    /// <summary>Challenge Card was successfully added to Steam Screenshots.</summary>
    public static void TrackSteamScreenshotAdded()
    {
        AnalyticsManager.Instance?.TrackEvent(STEAM_SCREENSHOT_ADDED, null);
    }

    /// <summary>Player clicked a social/external link (Discord, Twitter, etc.).</summary>
    public static void TrackSocialLinkClicked(string platform, string sourcePanel)
    {
        if (AnalyticsManager.Instance == null) return;

        AnalyticsManager.Instance.TrackEvent(SOCIAL_LINK_CLICKED, new Dictionary<string, object>
        {
            { "platform",     platform },
            { "source_panel", sourcePanel }
        });
    }

    /// <summary>
    /// Player started a level using a manually entered Challenge Code.
    /// Note: "source" does not claim the code came from a shared card —
    /// we have no way to verify the origin.
    /// </summary>
    public static void TrackChallengeStartedFromCode(string challengeCode, LevelPathMode pathMode, string source)
    {
        if (AnalyticsManager.Instance == null) return;

        AnalyticsManager.Instance.TrackEvent(CHALLENGE_STARTED_FROM_CODE, new Dictionary<string, object>
        {
            { "challenge_code", challengeCode },
            { "route",          GetRouteKey(pathMode) },
            { "source",         source }
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>Analytics-safe lowercase route key.</summary>
    private static string GetRouteKey(LevelPathMode mode) =>
        mode == LevelPathMode.DualPath ? "double_decker" : "single_decker";
}
