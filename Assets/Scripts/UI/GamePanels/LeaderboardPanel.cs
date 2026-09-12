using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main Leaderboard panel shown from the Main Menu.
/// Assign:
///    · rowPrefab       → a prefab that has LeaderboardRowUI on it
///    · rowContainer    → the parent transform where rows spawn
///    · tabGlobal       → "Global" tab Button
///    · tabFriends      → "Friends" tab Button
///    · backButton      → Back button
///    · loadingOverlay  → (optional) a CanvasGroup or GameObject shown while fetching
///    · emptyLabel      → (optional) TMP text shown when there are no entries
///    · titleLabel      → (optional) title text (e.g. "Leaderboard")
///
/// </summary>
public class LeaderboardPanel : BaseMainMenuPanel
{
    [Header("Prefab & Container")]
    [SerializeField] private LeaderboardRowUI rowPrefab;
    [SerializeField] private Transform rowContainer;

    [Header("Tab Buttons")]
    [SerializeField] private Button tabGlobal;
    [SerializeField] private Button tabFriends;
    [SerializeField] private Button backButton;

    [Header("State UI (optional)")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI emptyLabel;
    [SerializeField] private TextMeshProUGUI titleLabel;

    [Header("Settings")]
    [SerializeField] private int maxRows = 10;

    private readonly List<LeaderboardRowUI> _rowPool = new();
    private bool _isFetching = false;
    private bool _showingGlobal = true;

    protected override void SetupPanel()
    {
        if (tabGlobal != null)
            tabGlobal.onClick.AddListener(() => SwitchTab(true));

        if (tabFriends != null)
            tabFriends.onClick.AddListener(() => SwitchTab(false));

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            var backLbl = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backLbl != null) backLbl.SetText("← Back");
        }

        if (titleLabel != null)
            titleLabel.SetText("Leaderboard");

        if (emptyLabel != null)
            emptyLabel.SetText("No records yet");
    }

    private void OnDestroy()
    {
        if (tabGlobal  != null) tabGlobal.onClick.RemoveAllListeners();
        if (tabFriends != null) tabFriends.onClick.RemoveAllListeners();
        if (backButton != null) backButton.onClick.RemoveAllListeners();
    }

    protected override void OnShow()
    {
        _showingGlobal = true;
        // Kick off a fresh fetch every time the panel opens.
        _ = FetchAndPopulateAsync(_showingGlobal);
    }

    protected override void SetDefaultSelection()
    {
        if (tabGlobal != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(tabGlobal.gameObject);
    }

    // ─── Tab Logic ────────────────────────────────────────────
    private void SwitchTab(bool global)
    {
        if (_isFetching) return;
        _showingGlobal = global;
        _ = FetchAndPopulateAsync(global);
    }

    // ─── Data Fetch ───────────────────────────────────────────
    private async Task FetchAndPopulateAsync(bool global)
    {
        if (_isFetching) return;
        _isFetching = true;

        SetLoadingVisible(true);
        ClearRows();

        // ── Steam path (PC Standalone) ──────────────────────
#if !UNITY_ANDROID && !UNITY_IOS
        var bridge = Steam.SteamLeaderboardBridge.Instance;
        if (bridge != null && SteamManager.IsInitialized)
        {
            if (titleLabel != null)
                titleLabel.SetText(global ? "Global Leaderboard" : "Friends Leaderboard");

            Steam.LeaderboardEntryData[] entries = global
                ? await bridge.GetTopGlobalScoresAsync(Steam.SteamLeaderboardBridge.DEFAULT_LEADERBOARD, maxRows)
                : await bridge.GetFriendsScoresAsync(Steam.SteamLeaderboardBridge.DEFAULT_LEADERBOARD);

            if (entries != null && entries.Length > 0)
            {
                string localName = SteamManager.GetPlayerName() ?? "";
                foreach (var e in entries)
                {
                    bool isLocal = e.PlayerName == localName;
                    SpawnRow(e.GlobalRank, e.PlayerName, e.TimeSeconds, isLocal);
                }

                SetLoadingVisible(false);
                SetEmptyVisible(false);
                _isFetching = false;
                return;
            }
        }
#endif

        // ── Local fallback (offline / mobile) ───────────────
        var localMgr = Utils.LeaderboardManager.Instance;
        if (localMgr != null)
        {
            var records = localMgr.GetTopRecords(maxRows);
            if (records != null && records.Count > 0)
            {
                if (titleLabel != null)
                    titleLabel.SetText("Local Best Times");

                for (int i = 0; i < records.Count; i++)
                {
                    var rec = records[i];
                    string lvlName = !string.IsNullOrEmpty(rec.levelName)
                        ? rec.levelName
                        : Utils.LeaderboardManager.ResolveLevelName(rec.seed);

                    string holder = !string.IsNullOrEmpty(rec.playerName)
                        ? rec.playerName
                        : (SteamManager.IsInitialized ? SteamManager.GetPlayerName() : "You");

                    string rowDisplay = $"{lvlName} ({holder})";
                    SpawnRow(i + 1, rowDisplay, rec.time, true);
                }
                SetLoadingVisible(false);
                SetEmptyVisible(false);
                _isFetching = false;
                return;
            }
        }

        // ── Nothing to show ──────────────────────────────────
        SetLoadingVisible(false);
        SetEmptyVisible(true);
        _isFetching = false;
    }

    // ─── Row Pool ─────────────────────────────────────────────
    private void SpawnRow(int rank, string playerName, float timeSeconds, bool isLocal)
    {
        if (rowPrefab == null || rowContainer == null) return;

        LeaderboardRowUI row = Instantiate(rowPrefab, rowContainer);
        row.SetData(rank, playerName, timeSeconds, isLocal);
        _rowPool.Add(row);
    }

    private void ClearRows()
    {
        foreach (var row in _rowPool)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        _rowPool.Clear();
    }

    // ─── State Helpers ────────────────────────────────────────
    private void SetLoadingVisible(bool visible)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(visible);
    }

    private void SetEmptyVisible(bool visible)
    {
        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(visible);
    }

    // ─── Navigation ───────────────────────────────────────────
    private void OnBackClicked()
    {
        if (menuController != null)
            menuController.ShowMainPanel();
    }
}
