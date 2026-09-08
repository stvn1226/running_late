using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuPanel : BaseMenuPanel
{
    [Header("Pause Menu Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button newLevelButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Seed Controls")]
    [SerializeField] private Toggle seedToggle;
    [SerializeField] private TMP_InputField seedInputField;
    
    [Header("Frame Rate Controls")]
    [SerializeField] private Toggle fps60Toggle;
    [SerializeField] private Toggle fps120Toggle;
    
    [Header("Titles")]
    [SerializeField] private TextMeshProUGUI headerText;
    
    [Header("References")]
    [SerializeField] private LevelStarter levelStarter;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private SettingsPanel settingsPanel;
    
    private bool isManualSeedMode = false;
    
    protected override void SetupButtons()
    {
        SetupFrameRateToggles();

        if (seedInputField != null && seedInputField.gameObject.GetComponent<VirtualKeyboardHelper>() == null)
        {
            seedInputField.gameObject.AddComponent<VirtualKeyboardHelper>();
        }
    }
    
    protected override void RegisterButtonCallbacks()
    {
        retryButton.onClick.AddListener(OnRetryClicked);
        newLevelButton.onClick.AddListener(OnNewLevelClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
        seedToggle?.onValueChanged.AddListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.AddListener(OnSeedInputChanged);
    }
    
    protected override void UnregisterButtonCallbacks()
    {
        retryButton.onClick.RemoveListener(OnRetryClicked);
        newLevelButton.onClick.RemoveListener(OnNewLevelClicked);
        settingsButton?.onClick.RemoveListener(OnSettingsClicked);
        quitButton?.onClick.RemoveListener(OnQuitClicked);
        seedToggle?.onValueChanged.RemoveListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.RemoveListener(OnSeedInputChanged);
    }
    
    protected override void OnShow()
    {
        if (headerText != null)
        {
            headerText.text = "<bounce>PAUSE</bounce>";
        }
        LoadSettings();
        
        if (seedToggle != null)
        {
            UpdateToggleNavigation(seedToggle.isOn);
        }
    }
    
    private void OnRetryClicked()
    {
        int currentSeed = levelStarter.GetCurrentSeed();
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        if (levelStarter != null)
            PlayerPrefs.SetInt("RetryPathMode", (int)levelStarter.GetPathMode());
        
        UIManager.Instance.HideAllPanels();
        levelManager.ReplayLevel();
    }
    
    private void OnNewLevelClicked()
    {
        string inputVal = seedInputField != null ? seedInputField.text.Replace("\u200B", "").Trim() : "";
        if (!string.IsNullOrEmpty(inputVal))
        {
            if (int.TryParse(inputVal, out int seed))
            {
                PlayerPrefs.SetInt("NewLevelSeed", seed);
                PlayerPrefs.SetInt("UseNewLevelSeed", 1);
            }
            else
            {
                PlayerPrefs.SetInt("NewLevelSeed", inputVal.GetHashCode());
                PlayerPrefs.SetInt("UseNewLevelSeed", 1);
            }
        }
        else
        {
            PlayerPrefs.SetInt("UseNewLevelSeed", 0);
        }
        
        UIManager.Instance.HideAllPanels();
        levelManager.ReplayLevel();
    }
    
    private void OnQuitClicked()
    {
        GameLogger.LogUI("Quitting application...");
        
        // #if UNITY_EDITOR
        // UnityEditor.EditorApplication.isPlaying = false;
        // #else
        SceneLoader.Instance.LoadMainMenu();
        // #endif
    }
    
    private void OnSeedToggleChanged(bool useManualSeed)
    {
        isManualSeedMode = useManualSeed;
        
        if (seedInputField != null)
        {
            seedInputField.interactable = useManualSeed;
            
            if (useManualSeed)
            {
                seedInputField.text = levelStarter.GetCurrentSeed().ToString();
            }
            else if (UnityEngine.EventSystems.EventSystem.current != null && 
                     UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == seedInputField.gameObject)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(seedToggle.gameObject);
            }
        }
        
        UpdateToggleNavigation(useManualSeed);
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
        else 
        {
            nav.selectOnDown = (fps60Toggle != null && fps60Toggle.gameObject.activeInHierarchy) ? fps60Toggle : quitButton;
        }
        
        seedToggle.navigation = nav;
    }
    
    private void OnSeedInputChanged(string seedText)
    {
        if (int.TryParse(seedText, out int _))
        {
            seedInputField.image.color = Color.white;
        }
        else
        {
            seedInputField.image.color = Color.red;
        }
    }
    
    private void SetupFrameRateToggles()
    {
        fps60Toggle.onValueChanged.AddListener((isOn) => {
            if (isOn) SetTargetFrameRate(60);
        });
        
        fps120Toggle.onValueChanged.AddListener((isOn) => {
            if (isOn) SetTargetFrameRate(120);
        });
    }
    
    private void SetTargetFrameRate(int targetFPS)
    {
        Application.targetFrameRate = targetFPS;
        
        fps60Toggle.SetIsOnWithoutNotify(targetFPS == 60);
        fps120Toggle.SetIsOnWithoutNotify(targetFPS == 120);
        
        PlayerPrefs.SetInt("TargetFrameRate", targetFPS);
        GameLogger.LogUI($"Frame rate set to {targetFPS} FPS");
    }
    
    private void LoadSettings()
    {
        int savedFrameRate = PlayerPrefs.GetInt("TargetFrameRate", 60);
        SetTargetFrameRate(savedFrameRate);
    }
    
    private void OnSettingsClicked()
    {
        GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
        
        if (settingsPanel != null)
        {
            settingsPanel.ShowFromPause();
            Hide();
            GameLogger.LogWarning("UI", "Passing settings to ShowFromPause");
        }
        else
        {
            GameLogger.LogWarning("UI", "SettingsPanel reference is missing!");
        }
    }
    
    public void ShowFromSettings()
    {
        Show();
    }
}
