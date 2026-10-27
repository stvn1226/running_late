using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuPanel : BaseMainMenuPanel
{
    [Header("Step Containers")]
    [SerializeField] private GameObject step1Container;
    [SerializeField] private GameObject step2Container;
    
    [Header("Step 1 - Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private bool showLeaderboard = true;
    [SerializeField] private Button quitButton;

    [Header("Embedded Leaderboard")]
    [SerializeField] private LeaderboardPanel embeddedLeaderboard;

    [Header("Loading Feedback")]
    [Tooltip("Optional: assign a fullscreen black Image/CanvasGroup to show while the level loads")]
    [SerializeField] private GameObject loadingScreen;
    
    [Header("Step 2 - Play Options")]
    [SerializeField] private Button playButtonStep2;
    [SerializeField] private Button recentLevelsButtonStep2;
    [SerializeField] private Button savedLevelsButtonStep2;
    [SerializeField] private Button backButtonStep2;
    
    [Header("Step 2 - Seed Configuration")]
    [SerializeField] private Toggle seedToggle;
    [SerializeField] private TMP_InputField seedInputField;
    [SerializeField] private RandomSeedController randomSeedController;
    [SerializeField] private SteamKeyboardController steamKeyboardController;
    
    private bool isInStep2 = false;
    
    protected override void SetupPanel()
    {
        SetupStep1Buttons();
        SetupStep2Buttons();
        
        if (randomSeedController == null)
        {
            randomSeedController = FindFirstObjectByType<RandomSeedController>();
            if (randomSeedController == null)
            {
                GameObject seedControllerObj = new GameObject("RandomSeedController");
                randomSeedController = seedControllerObj.AddComponent<RandomSeedController>();
                GameLogger.Log("MENU", "RandomSeedController created");
            }
        }

        if (seedInputField != null && seedInputField.gameObject.GetComponent<VirtualKeyboardHelper>() == null)
        {
            seedInputField.gameObject.AddComponent<VirtualKeyboardHelper>();
        }
    }
    
    private void SetupStep1Buttons()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OnCreditsClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (leaderboardButton != null)
        {
            if (IsMobilePlatform())
            {
                leaderboardButton.gameObject.SetActive(false);
            }
            else
            {
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            }
        }

        if (quitButton != null)
        {
#if UNITY_IOS
            quitButton.gameObject.SetActive(false);
#else
            quitButton.onClick.AddListener(OnQuitClicked);
#endif
        }

        if (embeddedLeaderboard == null && menuController != null)
        {
            embeddedLeaderboard = menuController.LeaderboardPanel;
        }
        if (embeddedLeaderboard == null)
        {
            embeddedLeaderboard = Object.FindFirstObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
        }

        if (leaderboardContainer == null && leaderboardButton != null && leaderboardButton.transform.parent != null)
        {
            if (leaderboardButton.transform.parent.name.ToLower().Contains("leaderboard"))
            {
                leaderboardContainer = leaderboardButton.transform.parent.gameObject;
            }
        }

        if (IsMobilePlatform())
        {
            showLeaderboard = false;
            if (leaderboardContainer != null) leaderboardContainer.SetActive(false);
            if (embeddedLeaderboard != null)
            {
                embeddedLeaderboard.HideEmbedded();
                if (embeddedLeaderboard.gameObject != null)
                    embeddedLeaderboard.gameObject.SetActive(false);
            }
        }

        ApplyLeaderboardState();
    }
    
    private void SetupStep2Buttons()
    {
        SetupSeedKeyboard();

        if (playButtonStep2 != null)
        {
            playButtonStep2.onClick.AddListener(OnPlayStep2Clicked);
        }
        
        if (recentLevelsButtonStep2 != null)
        {
            recentLevelsButtonStep2.onClick.AddListener(OnRecentsClicked);
        }

        if (savedLevelsButtonStep2 != null)
        {
            savedLevelsButtonStep2.onClick.AddListener(OnSavedClicked);
        }
        
        if (seedToggle != null)
        {
            seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
            OnSeedToggleChanged(seedToggle.isOn);
        }

        SetupStep2BackButton();
    }

    private void SetupSeedKeyboard()
    {
        if (seedInputField == null)
            return;

        // Keep the field selectable for controller navigation, but don't enter text
        // editing merely by moving the selection onto it. Confirm opens Steam's keyboard.
        seedInputField.shouldActivateOnSelect = false;

        if (steamKeyboardController == null)
        {
            steamKeyboardController = seedInputField.GetComponent<SteamKeyboardController>();
        }

        if (steamKeyboardController == null)
        {
            steamKeyboardController = seedInputField.gameObject.AddComponent<SteamKeyboardController>();
        }

        steamKeyboardController.Configure(seedInputField);
    }
    
    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OnCreditsClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
        
        if (playButtonStep2 != null)
            playButtonStep2.onClick.RemoveListener(OnPlayStep2Clicked);
        
        if (recentLevelsButtonStep2 != null)
            recentLevelsButtonStep2.onClick.RemoveListener(OnRecentsClicked);

        if (savedLevelsButtonStep2 != null)
            savedLevelsButtonStep2.onClick.RemoveListener(OnSavedClicked);
        
        if (seedToggle != null)
            seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
    }
    
    private bool forceStep2OnShow = false;

    public void SetForceStep2OnNextShow(bool force)
    {
        forceStep2OnShow = force;
    }

    protected override void OnShow()
    {
        if (forceStep2OnShow)
        {
            ShowStep2();
            forceStep2OnShow = false;
        }
        else
        {
            ShowStep1();
        }
    }

    protected override void SetDefaultSelection()
    {
        if (isInStep2)
        {
            if (playButtonStep2 != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(playButtonStep2.gameObject);
            }
        }
        else
        {
            base.SetDefaultSelection();
        }
    }
    
    public void ShowStep1()
    {
        if (step1Container != null)
        {
            step1Container.SetActive(true);
        }
        
        if (step2Container != null)
        {
            step2Container.SetActive(false);
        }
        
        isInStep2 = false;
        
        if (playButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        ApplyLeaderboardState();

        if (!IsMobilePlatform() && embeddedLeaderboard != null && showLeaderboard)
        {
            // Register ALL step-1 buttons so they all navigate LEFT into the leaderboard.
            embeddedLeaderboard.RegisterMenuButtons(new Selectable[]
            {
                playButton,
                creditsButton,
                settingsButton,
                quitButton,
            });
            embeddedLeaderboard.ShowEmbedded(playButton);
        }
        else if (IsMobilePlatform() && embeddedLeaderboard != null)
        {
            embeddedLeaderboard.HideEmbedded();
            if (embeddedLeaderboard.gameObject != null)
                embeddedLeaderboard.gameObject.SetActive(false);
        }

        
        if (backButtonStep2 != null)
        {
            backButtonStep2.gameObject.SetActive(false);
        }

        if (step2TapDismissObj != null)
        {
            step2TapDismissObj.SetActive(false);
        }
        
        GameLogger.Log("MENU", "Showing Step 1 - Main Menu");
    }
    
    public void ShowStep2()
    {
        if (step1Container != null)
        {
            step1Container.SetActive(false);
        }
        
        if (step2Container != null)
        {
            step2Container.SetActive(true);
        }
        
        isInStep2 = true;
        
        SetupStep2BackButton();

        if (playButtonStep2 != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButtonStep2.gameObject);
        }

        if (embeddedLeaderboard != null)
        {
            embeddedLeaderboard.HideEmbedded();
        }
        
        GameLogger.Log("MENU", "Showing Step 2 - Play Options");
    }

    private GameObject step2TapDismissObj;

    private void SetupStep2BackButton()
    {
        if (backButtonStep2 == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.name == "Step2BackButton" || b.name == "BackBtnStep2")
                {
                    backButtonStep2 = b;
                    break;
                }
            }
        }

        if (backButtonStep2 == null)
        {
            GameObject backObj = new GameObject("Step2BackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backObj.transform.SetParent(transform, false);
            backObj.transform.SetAsLastSibling();

            RectTransform rect = backObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(50f, -50f);
            rect.sizeDelta = new Vector2(180f, 65f);

            Image img = backObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 0.88f);
            img.raycastTarget = true;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(backObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "← VOLVER";
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            if (playButtonStep2 != null)
            {
                var refText = playButtonStep2.GetComponentInChildren<TextMeshProUGUI>();
                if (refText != null && refText.font != null)
                {
                    tmp.font = refText.font;
                }
            }

            backButtonStep2 = backObj.GetComponent<Button>();
            backButtonStep2.targetGraphic = img;

            ColorBlock cb = backButtonStep2.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.9f, 0.4f, 1f);
            cb.pressedColor = new Color(0.8f, 0.7f, 0.2f, 1f);
            backButtonStep2.colors = cb;
        }

        // Tap outside the post-it note to dismiss Step 2 and return to Step 1 (same as Esc on desktop)
        if (step2TapDismissObj == null)
        {
            step2TapDismissObj = new GameObject("Step2TapDismiss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            step2TapDismissObj.transform.SetParent(transform, false);
            if (step2Container != null && step2Container.transform.parent != null)
            {
                step2TapDismissObj.transform.SetSiblingIndex(Mathf.Max(0, step2Container.transform.parent.GetSiblingIndex()));
            }

            RectTransform dismissRect = step2TapDismissObj.GetComponent<RectTransform>();
            dismissRect.anchorMin = Vector2.zero;
            dismissRect.anchorMax = Vector2.one;
            dismissRect.sizeDelta = Vector2.zero;

            Image dismissImg = step2TapDismissObj.GetComponent<Image>();
            dismissImg.color = Color.clear;
            dismissImg.raycastTarget = true;

            Button dismissBtn = step2TapDismissObj.GetComponent<Button>();
            dismissBtn.transition = Selectable.Transition.None;
            dismissBtn.onClick.AddListener(OnBackStep2Clicked);
        }

        if (backButtonStep2 != null)
        {
            backButtonStep2.onClick.RemoveListener(OnBackStep2Clicked);
            backButtonStep2.onClick.AddListener(OnBackStep2Clicked);
            backButtonStep2.gameObject.SetActive(isInStep2);
            if (isInStep2) backButtonStep2.transform.SetAsLastSibling();
        }

        if (step2TapDismissObj != null)
        {
            step2TapDismissObj.SetActive(isInStep2);
        }
    }

    public void OnBackStep2Clicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.OptionReturn);
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
        ShowStep1();
    }

    protected override void OnHide()
    {
        base.OnHide();
        if (backButtonStep2 != null)
        {
            backButtonStep2.gameObject.SetActive(false);
        }
        if (step2TapDismissObj != null)
        {
            step2TapDismissObj.SetActive(false);
        }
        if (embeddedLeaderboard != null)
        {
            embeddedLeaderboard.HideEmbedded();
        }
    }
    
    public bool IsInStep2()
    {
        return isInStep2;
    }
    
    private void OnSeedToggleChanged(bool isOn)
    {
        if (seedInputField != null)
        {
            seedInputField.interactable = isOn;
            
            if (!isOn && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == seedInputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(seedToggle.gameObject);
            }
        }
        
        UpdateToggleNavigation(isOn);
        
        GameLogger.Log("MENU", $"Seed toggle changed: {isOn}, InputField interactable: {isOn}");
    }
    
    private void UpdateToggleNavigation(bool seedInputActive)
    {
        if (seedToggle == null) return;
        
        Navigation nav = seedToggle.navigation;
        nav.mode = Navigation.Mode.Explicit;
        
        if (seedInputActive && seedInputField != null)
        {
            nav.selectOnDown = seedInputField;
        }
        else if (recentLevelsButtonStep2 != null)
        {
            nav.selectOnDown = recentLevelsButtonStep2;
        }
        
        seedToggle.navigation = nav;
        
        GameLogger.Log("MENU", $"Toggle navigation updated - selectOnDown: {(seedInputActive ? "InputField" : "RecentLevels")}");
    }
    
    private void OnPlayClicked()
    {
        GameLogger.Log("MENU", "Play clicked - Going to Step 2");
        ShowStep2();
    }
    
    private void OnPlayStep2Clicked()
    {
        GameLogger.Log("MENU", "Play Step 2 clicked - Starting game with configuration");
        
        ApplySeedConfiguration();

        // Loading feedback — disable input and show overlay before scene load.
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.enabled = false;
        
        if (menuController != null)
        {
            menuController.StartGame();
        }
    }
    
    private void ApplySeedConfiguration()
    {
        string customSeed = seedInputField != null ? seedInputField.text.Replace("\u200B", "").Trim() : "";
        
        if (!string.IsNullOrEmpty(customSeed))
        {
            int numericSeed;
            if (int.TryParse(customSeed, out numericSeed))
            {
                // Parsed successfully
            }
            else
            {
                numericSeed = customSeed.GetHashCode();
            }

            PlayerPrefs.SetInt("NewLevelSeed", numericSeed);
            PlayerPrefs.SetInt("UseNewLevelSeed", 1);

            if (randomSeedController != null)
            {
                randomSeedController.SetRandomSeed(customSeed);
            }
            
            GameLogger.Log("MENU", $"Using custom seed via PlayerPrefs: {numericSeed}");
        }
        else
        {
            PlayerPrefs.SetInt("UseNewLevelSeed", 0);
            if (randomSeedController != null) randomSeedController.GenerateRandomSeed();
            GameLogger.Log("MENU", "Custom seed empty - Generating random seed");
        }
    }
    
    private void OnRecentsClicked()
    {
        GameLogger.Log("MENU", "Recent levels clicked");

        if (menuController != null)
        {
            menuController.ShowRecentLevelsPanel();
        }
    }

    private void OnSavedClicked()
    {
        GameLogger.Log("MENU", "Saved levels clicked");

        if (menuController != null)
        {
            menuController.ShowSavedLevelsPanel();
        }
    }

    private void OnCreditsClicked()
    {
        GameLogger.Log("MENU", "Credits clicked");
        
        if (menuController != null)
        {
            menuController.ShowCreditsPanel();
        }
    }
    
    private void OnSettingsClicked()
    {
        GameLogger.Log("MENU", "Settings clicked");
        
        if (menuController != null)
        {
            menuController.ShowSettingsPanel();
        }
    }

    private void OnLeaderboardClicked()
    {
        GameLogger.Log("MENU", "Leaderboard clicked");

        if (menuController != null)
        {
            menuController.ShowLeaderboardPanel();
        }
    }
    
    private void OnQuitClicked()
    {
        GameLogger.Log("MENU", "Quit clicked");
        
        if (menuController != null)
        {
            menuController.QuitGame();
        }
    }

    /// <summary>
    /// Programmatically enable or disable the Leaderboard button.
    /// </summary>
    public void SetLeaderboardActive(bool active)
    {
        showLeaderboard = active;
        ApplyLeaderboardState();
    }

    private static bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return Application.isMobilePlatform;
#endif
    }

    private void ApplyLeaderboardState()
    {
        if (IsMobilePlatform())
        {
            showLeaderboard = false;
            if (leaderboardContainer != null) leaderboardContainer.SetActive(false);
            if (leaderboardButton != null) leaderboardButton.gameObject.SetActive(false);
            if (embeddedLeaderboard != null)
            {
                embeddedLeaderboard.HideEmbedded();
                if (embeddedLeaderboard.gameObject != null)
                    embeddedLeaderboard.gameObject.SetActive(false);
            }
            return;
        }

        // Old standalone button is hidden because it is replaced by embedded scrollable leaderboard
        GameObject containerToToggle = leaderboardContainer != null 
            ? leaderboardContainer 
            : (leaderboardButton != null ? leaderboardButton.gameObject : null);

        if (containerToToggle != null)
        {
            containerToToggle.SetActive(false);
        }

        if (embeddedLeaderboard != null)
        {
            if (showLeaderboard && !isInStep2 && isVisible)
            {
                embeddedLeaderboard.ShowEmbedded(playButton);
            }
            else
            {
                embeddedLeaderboard.HideEmbedded();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyLeaderboardState();
        }
    }
#endif
}
