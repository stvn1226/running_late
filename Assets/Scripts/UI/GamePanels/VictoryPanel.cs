using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils;

public class VictoryPanel : BaseMenuPanel
{
    [Header("Victory UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private TextMeshProUGUI seedText;
    
    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button newLevelButton;
    [SerializeField] private Button quitButton;
    
    [Header("Seed Controls")]
    [SerializeField] private Toggle seedToggle;
    [SerializeField] private TMP_InputField seedInputField;
    
    [Header("Victory Phrases")]
    [SerializeField] private string[] victoryPhrases = new string[]
    {
        "PHEW!",
        "NICE!",
        "PERFECT!",
        "AWESOME!",
        "SPEEDY!"
    };
    
    [Header("References")]
    [SerializeField] private LevelStarter levelStarter;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Timer timer;
    
    private Coroutine phraseRotationCoroutine;
    private float completionTime;
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
        UpdateVictoryData();
        ShowRandomPhrase();
    }
    
    protected override void OnHide()
    {
    }
    
    private void UpdateVictoryData()
    {
        if (timer != null)
        {
            completionTime = timer.ElapsedTime;
            currentTimeText.text = $"Time: {timer.GetFormattedTime()}";
        }
        
        if (levelStarter != null)
        {
            currentSeed = levelStarter.GetCurrentSeed();
            seedText.text = $"Seed: {currentSeed}";
        }
        
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.SaveTime(currentSeed, completionTime);
            
            TimeRecord bestRecord = LeaderboardManager.Instance.GetBestTime(currentSeed);
            
            if (bestRecord != null)
            {
                bool isNewRecord = Mathf.Approximately(completionTime, bestRecord.time);
                
                if (isNewRecord)
                {
                    bestTimeText.text = $"Best: {bestRecord.GetFormattedTime()} (NEW!)";
                    bestTimeText.color = Color.yellow;
                }
                else
                {
                    bestTimeText.text = $"Best: {bestRecord.GetFormattedTime()}";
                    bestTimeText.color = Color.black;
                }
            }
            else
            {
                bestTimeText.text = "";
            }
        }
    }
    
    private void ShowRandomPhrase()
    {
        if (titleText != null && victoryPhrases.Length > 0)
        {
            int randomIndex = Random.Range(0, victoryPhrases.Length);
            titleText.text = victoryPhrases[randomIndex];
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
