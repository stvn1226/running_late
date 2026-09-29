using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls a single row in the Leaderboard panel.
/// All elements (RankLabel, NameLabel, TimeLabel, SeedLabel, PlayButton)
/// are defined directly in the prefab and assigned in the Inspector.
/// No elements are instantiated at runtime.
/// </summary>
public class LeaderboardRowUI : MonoBehaviour
{
    [Header("Labels — assign in Inspector")]
    [SerializeField] private TextMeshProUGUI rankLabel;
    [SerializeField] private TextMeshProUGUI playerNameLabel;
    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI seedLabel;
    [SerializeField] private Button playButton;

    [Header("Optional — highlight local player row")]
    [SerializeField] private Image rowBackground;
    [SerializeField] private Color localPlayerColor = new Color(1f, 0.85f, 0.2f, 0.25f);
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0f);

    private int _seed;
    private int _pathMode;
    private System.Action<int, int> _onPlayClicked;

    public Selectable PrimarySelectable
    {
        get
        {
            if (playButton != null && playButton.gameObject.activeInHierarchy && playButton.interactable)
                return playButton;
            return GetComponent<Selectable>() ?? GetComponentInChildren<Selectable>();
        }
    }

    private void Awake()
    {
        // Safe binding if not assigned in inspector
        if (seedLabel == null)
        {
            Transform t = transform.Find("SeedLabel");
            if (t != null) seedLabel = t.GetComponent<TextMeshProUGUI>();
        }

        if (playButton == null)
        {
            Transform t = transform.Find("PlayButton");
            if (t != null) playButton = t.GetComponent<Button>();
        }

        if (seedLabel != null)
            seedLabel.raycastTarget = false;

        if (playButton != null)
        {
            // Ensure no child overlays block raycasting to the button
            foreach (var g in playButton.GetComponentsInChildren<Graphic>(true))
            {
                if (g.gameObject != playButton.gameObject)
                    g.raycastTarget = false;
                else
                    g.raycastTarget = true;
            }
        }
    }

    /// <summary>
    /// Populate this row with data coming from SteamLeaderboardBridge or LeaderboardManager.
    /// Order: Rank, Jugador, Tiempo, Seed, Botón de Play.
    /// </summary>
    public void SetData(int rank, string playerName, float timeSeconds, int seed = 0, int pathMode = 0, bool isLocalPlayer = false, System.Action<int, int> onPlayClicked = null)
    {
        if (seed <= 0)
        {
            seed = Mathf.Abs((playerName + (int)(timeSeconds * 1000)).GetHashCode() % 900000) + 100000;
        }

        _seed = seed;
        _pathMode = pathMode;
        _onPlayClicked = onPlayClicked;

        if (rankLabel != null)
            rankLabel.SetText($"#{rank}");

        if (playerNameLabel != null)
            playerNameLabel.SetText(playerName);

        if (timeLabel != null)
            timeLabel.SetText(FormatTime(timeSeconds));

        if (seedLabel != null)
        {
            seedLabel.SetText(seed.ToString());
        }

        if (rank == 1 && rankLabel != null && rankLabel.font != null)
        {
            var font = rankLabel.font;
            Debug.Log($"[TMP_DIAGNOSTIC] rankLabel.font='{font.name}', instanceID={font.GetInstanceID()}, family='{font.faceInfo.familyName}', atlasMode={font.atlasPopulationMode}, fallbacks={(font.fallbackFontAssetTable != null ? font.fallbackFontAssetTable.Count : 0)}");
            if (font.characterLookupTable != null)
            {
                if (font.characterLookupTable.TryGetValue('#', out var chPound))
                    Debug.Log($"[TMP_DIAGNOSTIC] '#' (35) -> glyphIndex: {chPound.glyphIndex}, rect: {chPound.glyph?.glyphRect.x},{chPound.glyph?.glyphRect.y},{chPound.glyph?.glyphRect.width}x{chPound.glyph?.glyphRect.height}");
                if (font.characterLookupTable.TryGetValue('0', out var chZero))
                    Debug.Log($"[TMP_DIAGNOSTIC] '0' (48) -> glyphIndex: {chZero.glyphIndex}, rect: {chZero.glyph?.glyphRect.x},{chZero.glyph?.glyphRect.y},{chZero.glyph?.glyphRect.width}x{chZero.glyph?.glyphRect.height}");
                if (font.characterLookupTable.TryGetValue('W', out var chW))
                    Debug.Log($"[TMP_DIAGNOSTIC] 'W' (87) -> glyphIndex: {chW.glyphIndex}, rect: {chW.glyph?.glyphRect.x},{chW.glyph?.glyphRect.y},{chW.glyph?.glyphRect.width}x{chW.glyph?.glyphRect.height}");
                if (font.characterLookupTable.TryGetValue('N', out var chN))
                    Debug.Log($"[TMP_DIAGNOSTIC] 'N' (78) -> glyphIndex: {chN.glyphIndex}, rect: {chN.glyph?.glyphRect.x},{chN.glyph?.glyphRect.y},{chN.glyph?.glyphRect.width}x{chN.glyph?.glyphRect.height}");
            }
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            if (onPlayClicked != null)
            {
                playButton.gameObject.SetActive(true);
                playButton.interactable = true;
                playButton.onClick.AddListener(OnPlayButtonPressed);
            }
            else
            {
                playButton.gameObject.SetActive(false);
            }
        }

        // Row background visual is controlled via sprite in the Inspector.
        // No code-side color override — leave it to the prefab.
    }

    /// <summary>
    /// Legacy overload for backwards compatibility.
    /// </summary>
    public void SetData(int rank, string playerName, float timeSeconds, int seed, bool isLocalPlayer, System.Action<int> onPlayClicked)
    {
        System.Action<int, int> adaptedAction = onPlayClicked != null ? (s, pm) => onPlayClicked(s) : null;
        SetData(rank, playerName, timeSeconds, seed, 0, isLocalPlayer, adaptedAction);
    }

    private void OnPlayButtonPressed()
    {
        GameLogger.Log("LEADERBOARD", $"Row Play clicked: Seed={_seed}, PathMode={_pathMode}");
        _onPlayClicked?.Invoke(_seed, _pathMode);
    }

    private static string FormatTime(float seconds)
    {
        return Utils.TimeRecord.FormatTime(seconds);
    }
}
