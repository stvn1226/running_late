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
    
    [Header("Step 2 - Play Options")]
    [SerializeField] private Button playButtonStep2;
    [SerializeField] private Button recentLevelsButtonStep2;
    [SerializeField] private Button savedLevelsButtonStep2;
    
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
            leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        if (leaderboardContainer == null && leaderboardButton != null && leaderboardButton.transform.parent != null)
        {
            if (leaderboardButton.transform.parent.name.ToLower().Contains("leaderboard"))
            {
                leaderboardContainer = leaderboardButton.transform.parent.gameObject;
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
        
        if (playButtonStep2 != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButtonStep2.gameObject);
        }
        
        GameLogger.Log("MENU", "Showing Step 2 - Play Options");
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

    private void ApplyLeaderboardState()
    {
        GameObject containerToToggle = leaderboardContainer != null 
            ? leaderboardContainer 
            : (leaderboardButton != null ? leaderboardButton.gameObject : null);

        if (containerToToggle != null)
        {
            containerToToggle.SetActive(showLeaderboard);
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
