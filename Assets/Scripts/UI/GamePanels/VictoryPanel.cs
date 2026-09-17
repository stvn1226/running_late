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
        seedToggle?.onValueChanged.AddListener(OnSeedToggleChanged);
        quitButton.onClick.AddListener(OnQuitClicked);
    }
    
    protected override void UnregisterButtonCallbacks()
    {
        retryButton.onClick.RemoveListener(OnRetryClicked);
        newLevelButton.onClick.RemoveListener(OnNewLevelClicked);
        seedToggle?.onValueChanged.RemoveListener(OnSeedToggleChanged);
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
        
        if (LevelStarter.Instance != null)
        {
            currentSeed = LevelStarter.Instance.GetCurrentSeed();
            seedText.text = $"Seed: {currentSeed}";
        }
        
        if (LeaderboardManager.Instance != null)
        {
            int currentPathMode = LevelStarter.Instance != null ? (int)LevelStarter.Instance.GetPathMode() : 0;
            LeaderboardManager.Instance.SaveTime(currentSeed, completionTime, pathMode: currentPathMode);
            
            TimeRecord bestRecord = LeaderboardManager.Instance.GetBestTime(currentSeed);
            
            if (bestRecord != null)
            {
                bool isNewRecord = Mathf.Approximately(completionTime, bestRecord.time);
                
                if (isNewRecord)
                {
                    bestTimeText.text = $"<wave>{bestRecord.GetFormattedTime()} (NEW!)</wave>";
                    bestTimeText.color = Color.yellow;
                }
                else
                {
                    bestTimeText.text = $"{bestRecord.GetFormattedTime()}";
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
            string phrase = victoryPhrases[randomIndex];
            if (!string.IsNullOrEmpty(phrase))
            {
                if (phrase.StartsWith("<incr>") && !phrase.Contains("</incr>"))
                {
                    phrase += "</incr>";
                }
                titleText.text = phrase;
            }
        }
    }
    
    private void OnSeedToggleChanged(bool useManualSeed)
    {
        isManualSeedMode = useManualSeed;
        seedInputField.interactable = useManualSeed;
        
        if (useManualSeed && LevelStarter.Instance != null)
        {
            seedInputField.text = LevelStarter.Instance.GetCurrentSeed().ToString();
        }
    }
    
    private void OnRetryClicked()
    {
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        if (LevelStarter.Instance != null)
            PlayerPrefs.SetInt("RetryPathMode", (int)LevelStarter.Instance.GetPathMode());
        
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ReplayLevel();
        }
        else
        {
            GameLogger.LogError("UI", "LevelManager.Instance is null! Cannot retry.");
        }
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
        
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }
        
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ReplayLevel();
        }
        else
        {
            GameLogger.LogError("UI", "LevelManager.Instance is null! Cannot start new level.");
        }
    }
    
    private void OnQuitClicked()
    {
        GameLogger.LogUI("Quitting application...");
        
        Time.timeScale = 1f;
        
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMainMenu();
        }
        else
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
