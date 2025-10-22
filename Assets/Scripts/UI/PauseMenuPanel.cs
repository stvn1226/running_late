using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuPanel : BaseMenuPanel
{
    [Header("Pause Menu Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button newLevelButton;
    [SerializeField] private Button quitButton;
    
    [Header("Seed Controls")]
    [SerializeField] private Toggle seedToggle;
    [SerializeField] private TMP_InputField seedInputField;
    
    [Header("Frame Rate Controls")]
    [SerializeField] private Toggle fps60Toggle;
    [SerializeField] private Toggle fps120Toggle;
    
    
    [Header("References")]
    [SerializeField] private LevelStarter levelStarter;
    [SerializeField] private LevelManager levelManager;
    
    private bool isManualSeedMode = false;
    
    protected override void SetupButtons()
    {
        SetupFrameRateToggles();
    }
    
    protected override void RegisterButtonCallbacks()
    {
        retryButton.onClick.AddListener(OnRetryClicked);
        newLevelButton.onClick.AddListener(OnNewLevelClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
        seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.AddListener(OnSeedInputChanged);
        
    }
    
    protected override void UnregisterButtonCallbacks()
    {
        retryButton.onClick.RemoveListener(OnRetryClicked);
        newLevelButton.onClick.RemoveListener(OnNewLevelClicked);
        quitButton?.onClick.RemoveListener(OnQuitClicked);
        seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
        seedInputField.onEndEdit.RemoveListener(OnSeedInputChanged);
        
    }
    
    protected override void OnShow()
    {
        LoadSettings();
    }
    
    private void OnRetryClicked()
    {
        int currentSeed = levelStarter.GetCurrentSeed();
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        
        UIManager.Instance.HideAllPanels();
        levelManager.ReplayLevel();
    }
    
    private void OnNewLevelClicked()
    {
        if (isManualSeedMode && !string.IsNullOrEmpty(seedInputField.text))
        {
            if (int.TryParse(seedInputField.text, out int seed))
            {
                PlayerPrefs.SetInt("NewLevelSeed", seed);
                PlayerPrefs.SetInt("UseNewLevelSeed", 1);
            }
            else
            {
                GameLogger.LogWarning("UI", "Invalid seed format!");
                PlayerPrefs.SetInt("UseNewLevelSeed", 0);
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
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void OnSeedToggleChanged(bool useManualSeed)
    {
        isManualSeedMode = useManualSeed;
        seedInputField.interactable = useManualSeed;
        
        if (useManualSeed)
        {
            seedInputField.text = levelStarter.GetCurrentSeed().ToString();
        }
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
}
