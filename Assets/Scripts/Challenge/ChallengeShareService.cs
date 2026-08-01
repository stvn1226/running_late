using UnityEngine;

/// <summary>
/// Handles all sharing actions for Challenge data:
///   - Clipboard (Challenge Code, share text)
///   - External URLs (Discord, Twitter, Steam page)
///   - Steam Screenshots (optional, non-blocking)
///
/// All methods are safe to call when Steam is unavailable.
/// URL methods do nothing when the URL is null or empty.
/// Clipboard failures are silently logged — they do not break card generation.
///
/// This service does NOT manage rendering, file storage, or UI state.
/// </summary>
public static class ChallengeShareService
{
    private const string LOG_TAG = "CHALLENGE_SHARE";

    // ─── Clipboard ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Copies the Challenge Code to the system clipboard.
    /// The code is the raw seed as a string — no transformation applied.
    /// </summary>
    public static void CopyCode(ChallengeData data)
    {
        TrySetClipboard(data.challengeCode, "Challenge Code");
        ChallengeAnalyticsEvents.TrackCodeCopied(data);
    }

    /// <summary>
    /// Copies the full share text (from ChallengeData.shareText) to the system clipboard.
    /// Includes the challenge code, time, route, and optional Steam URL.
    /// </summary>
    public static void CopyShareText(ChallengeData data)
    {
        if (string.IsNullOrEmpty(data.shareText))
        {
            GameLogger.LogWarning(LOG_TAG, "Share text is empty — nothing to copy.");
            return;
        }
        TrySetClipboard(data.shareText, "share text");
    }

    // ─── External URLs ────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens an external URL using the default system browser or handler.
    /// Does nothing if the URL is null or empty.
    /// Tracks a social_link_clicked analytics event.
    /// </summary>
    public static void OpenUrl(string url, string platformLabel, string sourcePanel)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            GameLogger.LogWarning(LOG_TAG, $"OpenUrl called with empty URL (label: {platformLabel}).");
            return;
        }

        try
        {
            Application.OpenURL(url);
            GameLogger.Log(LOG_TAG, $"Opened URL for: {platformLabel}");
            ChallengeAnalyticsEvents.TrackSocialLinkClicked(platformLabel, sourcePanel);
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LOG_TAG, $"OpenURL failed for {platformLabel}: {e.Message}");
        }
    }

    // ─── Steam Integration ────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to register the saved Challenge Card PNG with the Steam Screenshot library.
    ///
    /// Uses SteamScreenshots.AddScreenshot(filename, thumbnail, width, height).
    /// The thumbnail parameter is null — Steam will generate one automatically.
    ///
    /// Returns false silently if:
    ///   - Steam is not initialized
    ///   - The file does not exist on disk
    ///   - The Steam API returns an invalid handle
    ///   - Any exception is thrown
    ///
    /// A local failure from this method does NOT affect whether the card was saved to disk.
    /// Never call this before confirming the PNG was written successfully.
    /// </summary>
    public static bool TryAddToSteam(string filePath, int width, int height)
    {
        if (!SteamManager.IsInitialized)
        {
            GameLogger.Log(LOG_TAG, "Steam not initialized — skipping Steam screenshot upload.");
            return false;
        }

        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            GameLogger.LogWarning(LOG_TAG, "Steam screenshot: file path is invalid or file not found on disk.");
            return false;
        }

        try
        {
            // AddScreenshot(filename, thumbnailFilename, width, height)
            // thumbnail = null → Steam generates a thumbnail automatically.
            // Returns a Screenshot struct; default(Screenshot) indicates failure.
            var handle = Steamworks.SteamScreenshots.AddScreenshot(filePath, null, width, height);

            // Facepunch.Steamworks returns a Screenshot struct.
            // A zero/default struct means Steam rejected or failed the call.
            bool success = !handle.Equals(default(Steamworks.Data.Screenshot));

            if (success)
            {
                GameLogger.Log(LOG_TAG, "Challenge Card added to Steam screenshots.");
                ChallengeAnalyticsEvents.TrackSteamScreenshotAdded();
            }
            else
            {
                GameLogger.LogWarning(LOG_TAG, "SteamScreenshots.AddScreenshot returned an invalid handle.");
            }

            return success;
        }
        catch (System.Exception e)
        {
            // Non-critical — the local PNG is already saved regardless of this result.
            GameLogger.LogWarning(LOG_TAG, $"Steam screenshot failed (non-critical): {e.Message}");
            return false;
        }
    }

    // ─── Internal ─────────────────────────────────────────────────────────────────

    private static void TrySetClipboard(string value, string label)
    {
        try
        {
            GUIUtility.systemCopyBuffer = value;
            GameLogger.Log(LOG_TAG, $"Copied {label} to clipboard.");
        }
        catch (System.Exception e)
        {
            // GUIUtility.systemCopyBuffer can fail in Wayland sessions or headless builds.
            // This is non-critical — the card and share text are still available.
            GameLogger.LogWarning(LOG_TAG, $"Clipboard write failed for {label} (non-critical): {e.Message}");
        }
    }
}
