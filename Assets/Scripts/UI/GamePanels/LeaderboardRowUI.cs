using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls a single row in the Leaderboard panel.
/// Assign the TMP labels in the Inspector — the layout is fully under your control.
/// </summary>
public class LeaderboardRowUI : MonoBehaviour
{
    [Header("Labels — assign in Inspector")]
    [SerializeField] private TextMeshProUGUI rankLabel;
    [SerializeField] private TextMeshProUGUI playerNameLabel;
    [SerializeField] private TextMeshProUGUI timeLabel;

    [Header("Optional — highlight local player row")]
    [SerializeField] private Image rowBackground;
    [SerializeField] private Color localPlayerColor = new Color(1f, 0.85f, 0.2f, 0.25f);
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0f);

    /// <summary>
    /// Populate this row with data coming from SteamLeaderboardBridge or LeaderboardManager.
    /// </summary>
    public void SetData(int rank, string playerName, float timeSeconds, bool isLocalPlayer = false)
    {
        if (rankLabel != null)
            rankLabel.SetText($"#{rank}");

        if (playerNameLabel != null)
            playerNameLabel.SetText(playerName);

        if (timeLabel != null)
            timeLabel.SetText(FormatTime(timeSeconds));

        if (rowBackground != null)
            rowBackground.color = isLocalPlayer ? localPlayerColor : defaultColor;
    }

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        float s = seconds % 60f;
        return m > 0 ? $"{m}:{s:00.00}" : $"{s:00.00}s";
    }
}
