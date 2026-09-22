using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main Leaderboard panel shown from the Main Menu.
/// Displays Rank, Jugador, Tiempo, Seed, and Play Button.
/// Launches the level with its exact Seed and PathMode (DualPath / SingleSidewalk).
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
    [Tooltip("Text label inside TabGlobal button")]
    [SerializeField] private TextMeshProUGUI tabGlobalLabel;
    [Tooltip("Text label inside TabFriends button")]
    [SerializeField] private TextMeshProUGUI tabFriendsLabel;
    [SerializeField] private Color tabSelectedColor   = Color.white;
    [SerializeField] private Color tabDeselectedColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Header("Tab Sprites (optional - auto-detected from TabGlobal)")]
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabInactiveSprite;
    private Image _tabGlobalImage;
    private Image _tabFriendsImage;

    [Header("State UI (optional)")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI emptyLabel;
    [SerializeField] private TextMeshProUGUI titleLabel;

    [Header("Settings")]
    [SerializeField] private int maxRows = 10;

    [Header("Embedded Mode")]
    [SerializeField] private bool isEmbedded = false;
    [SerializeField] private ScrollRect scrollRect;
    private Selectable _sideControlsTarget;

    private readonly List<LeaderboardRowUI> _rowPool = new();
    // All Step-1 menu buttons that should navigate LEFT into the leaderboard.
    private readonly List<Selectable> _menuButtons = new();
    private bool _isFetching = false;
    private bool _showingGlobal = true;

    private void Awake()
    {
        if (menuController == null)
        {
            menuController = GetComponentInParent<MainMenuController>() ?? Object.FindFirstObjectByType<MainMenuController>();
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>(true);
        }
        if (scrollRect != null && scrollRect.GetComponent<AutoScrollToSelection>() == null)
        {
            scrollRect.gameObject.AddComponent<AutoScrollToSelection>();
        }

        InitTabVisuals();
    }

    protected override void SetupPanel()
    {
        if (tabGlobal != null)
        {
            tabGlobal.onClick.AddListener(() => SwitchTab(true, forceRefresh: true));
            var triggerGlobal = tabGlobal.GetComponent<TabHighlightTrigger>() ?? tabGlobal.gameObject.AddComponent<TabHighlightTrigger>();
            triggerGlobal.onHighlight = () => SwitchTab(true, forceRefresh: false);

            // Auto-find label if not assigned
            if (tabGlobalLabel == null)
                tabGlobalLabel = tabGlobal.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (tabFriends != null)
        {
            tabFriends.onClick.AddListener(() => SwitchTab(false, forceRefresh: true));
            var triggerFriends = tabFriends.GetComponent<TabHighlightTrigger>() ?? tabFriends.gameObject.AddComponent<TabHighlightTrigger>();
            triggerFriends.onHighlight = () => SwitchTab(false, forceRefresh: false);

            if (tabFriendsLabel == null)
                tabFriendsLabel = tabFriends.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            var backLbl = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backLbl != null) backLbl.SetText("← Back");

            // If panel is embedded from the start (Inspector), hide immediately.
            if (isEmbedded)
                backButton.gameObject.SetActive(false);
        }

        if (titleLabel != null)
            titleLabel.SetText("Leaderboard");

        if (emptyLabel != null)
            emptyLabel.SetText("No records yet");

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>(true);
        }
        if (scrollRect != null && scrollRect.GetComponent<AutoScrollToSelection>() == null)
        {
            scrollRect.gameObject.AddComponent<AutoScrollToSelection>();
        }

        // Apply initial tab visual state (Global is active by default)
        RefreshTabVisuals(isGlobalActive: true);
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
        if (backButton != null && !isEmbedded)
        {
            backButton.gameObject.SetActive(true);
        }
        RefreshTabVisuals(true);
        // Kick off a fresh fetch every time the panel opens.
        _ = FetchAndPopulateAsync(_showingGlobal);
    }

    protected override void SetDefaultSelection()
    {
        if (tabGlobal != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(tabGlobal.gameObject);
    }

    // ─── Tab Logic ────────────────────────────────────────────
    private void InitTabVisuals()
    {
        if (tabGlobal != null && _tabGlobalImage == null)
            _tabGlobalImage = tabGlobal.image != null ? tabGlobal.image : tabGlobal.GetComponent<Image>();

        if (tabFriends != null && _tabFriendsImage == null)
            _tabFriendsImage = tabFriends.image != null ? tabFriends.image : tabFriends.GetComponent<Image>();

        // Auto-detect sprites from TabGlobal button if unassigned
        if (tabActiveSprite == null && tabGlobal != null)
        {
            if (tabGlobal.spriteState.highlightedSprite != null)
                tabActiveSprite = tabGlobal.spriteState.highlightedSprite;
            else if (tabGlobal.spriteState.selectedSprite != null)
                tabActiveSprite = tabGlobal.spriteState.selectedSprite;
        }

        if (tabInactiveSprite == null && _tabGlobalImage != null && _tabGlobalImage.sprite != null)
        {
            tabInactiveSprite = _tabGlobalImage.sprite;
        }

        // If TabFriends doesn't have SpriteSwap configured but TabGlobal does, sync state
        if (tabFriends != null && tabGlobal != null)
        {
            if (tabGlobal.transition == Selectable.Transition.SpriteSwap &&
                tabFriends.transition != Selectable.Transition.SpriteSwap)
            {
                tabFriends.transition = Selectable.Transition.SpriteSwap;
                tabFriends.spriteState = tabGlobal.spriteState;
            }
        }
    }

    private void SwitchTab(bool global, bool forceRefresh = false)
    {
        if (_isFetching) return;
        if (!forceRefresh && global == _showingGlobal) return;
        _showingGlobal = global;
        RefreshTabVisuals(global);
        _ = FetchAndPopulateAsync(global);
    }

    private void RefreshTabColors(bool isGlobalActive)
    {
        RefreshTabVisuals(isGlobalActive);
    }

    private void RefreshTabVisuals(bool isGlobalActive)
    {
        InitTabVisuals();

        // 1. Text colors
        if (tabGlobalLabel  != null) tabGlobalLabel.color  = isGlobalActive  ? tabSelectedColor : tabDeselectedColor;
        if (tabFriendsLabel != null) tabFriendsLabel.color = !isGlobalActive ? tabSelectedColor : tabDeselectedColor;

        // 2. Button sprites (active tab shows highlighted sprite, inactive shows normal sprite)
        if (_tabGlobalImage != null)
        {
            Sprite target = isGlobalActive ? tabActiveSprite : tabInactiveSprite;
            if (target != null)
            {
                _tabGlobalImage.sprite = target;
                _tabGlobalImage.overrideSprite = null;
            }
        }

        if (_tabFriendsImage != null)
        {
            Sprite target = !isGlobalActive ? tabActiveSprite : tabInactiveSprite;
            if (target != null)
            {
                _tabFriendsImage.sprite = target;
                _tabFriendsImage.overrideSprite = null;
            }
        }
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
                    SpawnRow(e.GlobalRank, e.PlayerName, e.TimeSeconds, e.Seed, e.PathMode, isLocal);
                }

                SetLoadingVisible(false);
                SetEmptyVisible(false);
                LinkNavigation(_sideControlsTarget);
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
                    string holder = !string.IsNullOrEmpty(rec.playerName)
                        ? rec.playerName
                        : (SteamManager.IsInitialized ? SteamManager.GetPlayerName() : "You");

                    SpawnRow(i + 1, holder, rec.time, rec.seed, rec.pathMode, true);
                }
                SetLoadingVisible(false);
                SetEmptyVisible(false);
                LinkNavigation(_sideControlsTarget);
                _isFetching = false;
                return;
            }
        }

        // ── Nothing to show ──────────────────────────────────
        SetLoadingVisible(false);
        SetEmptyVisible(true);
        LinkNavigation(_sideControlsTarget);
        _isFetching = false;
    }

    // ─── Row Pool ─────────────────────────────────────────────
    private void SpawnRow(int rank, string playerName, float timeSeconds, int seed, int pathMode, bool isLocal)
    {
        if (rowPrefab == null || rowContainer == null) return;

        LeaderboardRowUI row = Instantiate(rowPrefab, rowContainer);
        row.SetData(rank, playerName, timeSeconds, seed, pathMode, isLocal, OnPlayWithSeedClicked);
        _rowPool.Add(row);
    }

    private int ResolvePathMode(int seed, int fallbackPathMode = 0)
    {
        if (fallbackPathMode != 0) return fallbackPathMode;

        if (SavedLevelsList.instance != null)
        {
            var match = SavedLevelsList.instance.GetSavedLevels()?.Find(l => l.savedLevelSeed == seed);
            if (match != null) return match.pathMode;
        }
        if (RecentLevelsList.instance != null)
        {
            var match = RecentLevelsList.instance.GetRecentLevels()?.Find(l => l.savedLevelSeed == seed);
            if (match != null) return match.pathMode;
        }
        return fallbackPathMode;
    }

    private void OnPlayWithSeedClicked(int seed, int pathMode)
    {
        if (seed == 0)
        {
            GameLogger.LogWarning("LEADERBOARD", "Cannot launch game from leaderboard: Seed is 0");
            return;
        }

        int finalPathMode = ResolvePathMode(seed, pathMode);
        GameLogger.Log("LEADERBOARD", $"Play clicked for seed {seed}, pathMode {finalPathMode} ({(finalPathMode == 0 ? "DualPath" : "SingleSidewalk")})");

        AudioManager.Instance?.Play(AudioManager.SoundType.OptionSelect);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);

        PlayerPrefs.SetInt("UseNewLevelSeed", 1);
        PlayerPrefs.SetInt("NewLevelSeed", seed);
        PlayerPrefs.SetInt("NewLevelPathMode", finalPathMode);
        PlayerPrefs.SetInt("PathMode", finalPathMode);
        PlayerPrefs.Save();

        // ── Loading feedback ──────────────────────────────────
        // Reuse the panel's loading overlay as a visual loading screen.
        // Disable EventSystem input so the player can't double-trigger.
        SetLoadingVisible(true);
        if (EventSystem.current != null)
            EventSystem.current.enabled = false;

        if (menuController == null)
        {
            menuController = GetComponentInParent<MainMenuController>() ?? Object.FindFirstObjectByType<MainMenuController>();
        }

        if (menuController != null && menuController.gameObject.activeInHierarchy)
        {
            menuController.StartGame();
        }
        else if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadGameplay();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("ModularLevel");
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rowPool)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        _rowPool.Clear();

        if (scrollRect != null)
        {
            var autoScroll = scrollRect.GetComponent<AutoScrollToSelection>();
            if (autoScroll != null)
                autoScroll.ResetScroll();
            else if (scrollRect.content != null)
            {
                scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, 0f);
                scrollRect.velocity = Vector2.zero;
            }
        }
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

    // ─── Embedded Mode ────────────────────────────────────────
    public void ShowEmbedded(Selectable returnTarget)
    {
        isEmbedded = true;
        _sideControlsTarget = returnTarget;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            isVisible = true;
        }
        else
        {
            gameObject.SetActive(true);
            isVisible = true;
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }

        _showingGlobal = true;
        RefreshTabVisuals(true);
        _ = FetchAndPopulateAsync(_showingGlobal);
    }

    public void HideEmbedded()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            isVisible = false;
        }
        else
        {
            gameObject.SetActive(false);
            isVisible = false;
        }
    }

    public void RegisterMenuButtons(IEnumerable<Selectable> buttons)
    {
        _menuButtons.Clear();
        foreach (var b in buttons)
        {
            if (b != null) _menuButtons.Add(b);
        }
    }

    public void LinkNavigation(Selectable sideTarget)
    {
        if (sideTarget != null)
        {
            _sideControlsTarget = sideTarget;
        }

        Selectable returnTarget = _sideControlsTarget;
        if (returnTarget == null) return;

        // Determine first selectable in leaderboard
        Selectable firstRowSelectable = (_rowPool.Count > 0 && _rowPool[0] != null && _rowPool[0].PrimarySelectable != null)
            ? _rowPool[0].PrimarySelectable
            : null;

        Selectable firstInLeaderboard = firstRowSelectable != null
            ? firstRowSelectable
            : (tabGlobal != null ? (Selectable)tabGlobal : returnTarget);

        // 1. From ALL menu-side buttons: Left goes into Leaderboard
        // The primary returnTarget is still used for the row→right path.
        var allSideButtons = new System.Collections.Generic.HashSet<Selectable> { returnTarget };
        foreach (var b in _menuButtons)
            if (b != null) allSideButtons.Add(b);

        foreach (var btn in allSideButtons)
        {
            Navigation sideNav = btn.navigation;
            sideNav.mode = Navigation.Mode.Explicit;
            sideNav.selectOnLeft = firstInLeaderboard;
            btn.navigation = sideNav;
        }

        // 2. Link leaderboard rows
        for (int i = 0; i < _rowPool.Count; i++)
        {
            if (_rowPool[i] == null) continue;
            Selectable rowSel = _rowPool[i].PrimarySelectable;
            if (rowSel == null) continue;

            Navigation nav = rowSel.navigation;
            nav.mode = Navigation.Mode.Explicit;

            // Right always returns to side controls (Play button)
            nav.selectOnRight = returnTarget;

            // Up goes to previous row or tabGlobal if first row
            if (i > 0 && _rowPool[i - 1] != null && _rowPool[i - 1].PrimarySelectable != null)
            {
                nav.selectOnUp = _rowPool[i - 1].PrimarySelectable;
            }
            else
            {
                nav.selectOnUp = tabGlobal != null ? (Selectable)tabGlobal : null;
            }

            // Down goes to next row, or stays on current row if at bottom
            if (i < _rowPool.Count - 1 && _rowPool[i + 1] != null && _rowPool[i + 1].PrimarySelectable != null)
            {
                nav.selectOnDown = _rowPool[i + 1].PrimarySelectable;
            }
            else
            {
                nav.selectOnDown = rowSel; // Stays on bottom element
            }

            nav.selectOnLeft = null;
            rowSel.navigation = nav;
        }

        // 3. Tab buttons navigation
        if (tabGlobal != null)
        {
            Navigation nav = tabGlobal.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnRight = tabFriends != null ? (Selectable)tabFriends : returnTarget;
            nav.selectOnLeft = null;
            nav.selectOnDown = firstRowSelectable;
            nav.selectOnUp = null;
            tabGlobal.navigation = nav;
        }

        if (tabFriends != null)
        {
            Navigation nav = tabFriends.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = tabGlobal != null ? (Selectable)tabGlobal : null;
            nav.selectOnRight = returnTarget;
            nav.selectOnDown = firstRowSelectable;
            nav.selectOnUp = null;
            tabFriends.navigation = nav;
        }
    }

    /// <summary>
    /// Forwards pointer enter (mouse hover) and UI select (gamepad / keyboard focus) to switch tabs automatically.
    /// </summary>
    private class TabHighlightTrigger : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public System.Action onHighlight;

        public void OnPointerEnter(PointerEventData eventData)
        {
            onHighlight?.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            onHighlight?.Invoke();
        }
    }
}
