using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefeatPanel : BaseMenuPanel
{
    [Header("Defeat UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI seedText;
    
    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button newLevelButton;
    [SerializeField] private Button quitButton;
    
    [Header("Seed Controls")]
    [SerializeField] private Toggle seedToggle;
    [SerializeField] private TMP_InputField seedInputField;
    
    [Header("Defeat Phrases")]
    [SerializeField] private string[] defeatPhrases = new string[]
    {
        "TOO LATE!",
        "TIME'S UP!",
        "MISSED IT!",
        "TRY AGAIN!",
        "SO CLOSE!"
    };
    
    [Header("References")]
    [SerializeField] private LevelStarter levelStarter;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Timer timer;
    
    private int currentSeed;
    private bool isManualSeedMode = false;
    
    protected override void SetupButtons()
    {
    }
    
    protected override void RegisterButtonCallbacks()
    {
        retryButton.onClick.AddListener(OnRetryClicked);
        newLevelButton.onClick.AddListener(OnNewLevelClicked);
        seedToggle.onValueChanged.AddListener(OnSeedToggleChanged);
        quitButton.onClick.AddListener(OnQuitClicked);
    }
    
    protected override void UnregisterButtonCallbacks()
    {
        retryButton.onClick.RemoveListener(OnRetryClicked);
        newLevelButton.onClick.RemoveListener(OnNewLevelClicked);
        seedToggle.onValueChanged.RemoveListener(OnSeedToggleChanged);
        quitButton.onClick.RemoveListener(OnQuitClicked);
    }
    
    protected override void OnShow()
    {
        UpdateDefeatData();
        ShowRandomPhrase();
    }
    
    protected override void OnHide()
    {
    }
    
    private void UpdateDefeatData()
    {
        if (timer != null)
        {
            currentTimeText.text = $"Time: {timer.GetFormattedTime()}";
        }
        
        if (levelStarter != null)
        {
            currentSeed = levelStarter.GetCurrentSeed();
            seedText.text = $"Seed: {currentSeed}";
        }
    }
    
    private void ShowRandomPhrase()
    {
        if (titleText != null && defeatPhrases.Length > 0)
        {
            int randomIndex = Random.Range(0, defeatPhrases.Length);
            titleText.text = defeatPhrases[randomIndex];
        }
    }
    
    private void OnSeedToggleChanged(bool useManualSeed)
    {
        isManualSeedMode = useManualSeed;
        seedInputField.interactable = useManualSeed;
        
        if (useManualSeed && levelStarter != null)
        {
            seedInputField.text = levelStarter.GetCurrentSeed().ToString();
        }
    }
    
    private void OnRetryClicked()
    {
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        
        Time.timeScale = 1f;
        UIManager.Instance.HideAllPanels();
        
        if (levelManager != null)
        {
            levelManager.ReplayLevel();
        }
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
        
        Time.timeScale = 1f;
        UIManager.Instance.HideAllPanels();
        
        if (levelManager != null)
        {
            levelManager.ReplayLevel();
        }
    }
    
    private void OnQuitClicked()
    {
        GameLogger.LogUI("Quitting application...");
        
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
