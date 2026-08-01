using UnityEngine;

/// <summary>
/// Central configuration for the Challenge Sharing feature.
/// Create via: Assets → Create → Running Late → Challenge Config
/// Assign to the VictoryPanel and ChallengeCardGenerator via Inspector.
///
/// All URLs, folder names, file names, visual assets, and text templates
/// are configured here. Empty URLs hide their corresponding buttons automatically.
/// </summary>
[CreateAssetMenu(fileName = "ChallengeConfig", menuName = "Running Late/Challenge Config")]
public class ChallengeConfig : ScriptableObject
{
    // ─── Product ──────────────────────────────────────────────────────────────────

    [Header("Product")]
    [Tooltip("Product name used in share text templates.")]
    public string productName = "Running Late";

    // ─── URLs ─────────────────────────────────────────────────────────────────────

    [Header("URLs")]
    [Tooltip("Steam Store page URL. Appended to share text. Leave empty to omit.")]
    public string steamStoreUrl = "https://store.steampowered.com/app/4724080/";

    [Tooltip("Discord server invite URL. Leave empty to hide the Join Discord button.")]
    public string discordUrl = "";

    [Tooltip("Optional Twitter/X share URL. Leave empty to hide the button.")]
    public string twitterBaseUrl = "";

    // ─── Share Text ───────────────────────────────────────────────────────────────

    [Header("Share Text")]
    [Tooltip("Template for the copyable share text. Tokens: {CODE} {PRODUCT} {TIME} {ROUTE}")]
    [TextArea(3, 5)]
    public string shareTextTemplate =
        "I completed Challenge Code {CODE} in {PRODUCT} with a time of {TIME} on {ROUTE}. Can you beat my time?";

    [Tooltip("Exclamation shown on the left burst of the Challenge Card (e.g. 'PHEW!'). Leave empty to keep the prefab default.")]
    public string cardExpressionText = "PHEW!";

    [Tooltip("Message shown on the Challenge Card image.")]
    public string challengeMessage = "CAN YOU BEAT MY TIME?";

    [Tooltip("Status line shown on the Challenge Card image.")]
    public string challengeCompleteLabel = "✦ CHALLENGE COMPLETE ✦";

    // ─── Storage ──────────────────────────────────────────────────────────────────

    [Header("Storage")]
    [Tooltip("Main folder name inside the Pictures directory.")]
    public string outputFolderName = "Running Late";

    [Tooltip("Subfolder inside outputFolderName.")]
    public string outputSubfolderName = "Challenges";

    [Tooltip("Base name prefix for the saved PNG file.")]
    public string fileBaseName = "RunningLate_Challenge";

    // ─── Output Resolution ────────────────────────────────────────────────────────

    [Header("Output Resolution")]
    [Tooltip("Width of the output Challenge Card in pixels.")]
    public int outputWidth = 1280;

    [Tooltip("Height of the output Challenge Card in pixels.")]
    public int outputHeight = 720;

    [Tooltip("Fraction of the card height used by the info overlay (0.15–0.40).")]
    [Range(0.15f, 0.40f)]
    public float overlayHeightFraction = 0.27f;

    // ─── Steam Integration ────────────────────────────────────────────────────────

    [Header("Steam Integration")]
    [Tooltip("If true and Steam is available, add the saved card to the Steam screenshot library.")]
    public bool attemptSteamScreenshot = true;

    [Tooltip("If true, automatically copy the share text to the clipboard when a card is saved.")]
    public bool autoCopyTextOnCardCreate = true;

    // ─── UI Behavior ─────────────────────────────────────────────────────────────

    [Header("UI Behavior")]
    [Tooltip("Show the Open Folder button after saving. Disabled by default — may not work in Gaming Mode on Steam Deck.")]
    public bool showOpenFolderButton = false;

    // ─── Visual Assets ────────────────────────────────────────────────────────────

    [Header("Visual Assets — Challenge Card")]
    [Tooltip("Logo or wordmark to display on the Challenge Card.")]
    public Sprite logoSprite;

    [Tooltip("Icon representing the Single Decker route on the card.")]
    public Sprite singleDeckerSprite;

    [Tooltip("Icon representing the Double Decker route on the card.")]
    public Sprite doubleDeckerSprite;

    [Tooltip("Background color of the info overlay panel.")]
    public Color overlayBackgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);

    [Tooltip("Accent color used for the Challenge Code text.")]
    public Color accentColor = new Color(0.95f, 0.75f, 0.15f, 1f);

    [Tooltip("Primary text color (code value, time value).")]
    public Color primaryTextColor = Color.white;

    [Tooltip("Secondary text color (labels, message).")]
    public Color secondaryTextColor = new Color(0.70f, 0.70f, 0.70f, 1f);

    // ─── Challenge Card Prefab ────────────────────────────────────────────────────

    [Header("Challenge Card Prefab")]
    [Tooltip("Prefab containing the ChallengeCardOverlay component and its dedicated Camera.")]
    public GameObject challengeCardOverlayPrefab;

    // ─── Utility Methods ──────────────────────────────────────────────────────────

    /// <summary>
    /// Assembles the share text from the template and challenge data.
    /// Appends the Steam URL if configured.
    /// </summary>
    public string BuildShareText(ChallengeData data)
    {
        string text = shareTextTemplate
            .Replace("{CODE}",    data.challengeCode)
            .Replace("{PRODUCT}", productName)
            .Replace("{TIME}",    data.formattedTime)
            .Replace("{ROUTE}",   data.routeName);

        if (!string.IsNullOrWhiteSpace(steamStoreUrl))
            text += $" Play on Steam: {steamStoreUrl}";

        return text;
    }

    /// <summary>
    /// Builds a filesystem-safe filename base (without .png extension).
    /// Uses seed and route as separate components — no transformation applied.
    /// </summary>
    public string BuildSafeFileName(ChallengeData data)
    {
        // Route formatted without spaces for a valid filename
        string routeSafe = data.pathMode == LevelPathMode.DualPath ? "DoubleDecker" : "SingleDecker";

        // Time formatted without : or . characters
        string timeSafe  = data.formattedTime.Replace(":", "").Replace(".", "");

        // Result: RunningLate_Challenge_847291_DoubleDecker_014533
        return $"{fileBaseName}_{data.challengeCode}_{routeSafe}_{timeSafe}";
    }

    // ─── Convenience Properties ───────────────────────────────────────────────────

    public bool HasDiscordUrl  => !string.IsNullOrWhiteSpace(discordUrl);
    public bool HasSteamUrl    => !string.IsNullOrWhiteSpace(steamStoreUrl);
    public bool HasTwitterUrl  => !string.IsNullOrWhiteSpace(twitterBaseUrl);
}
