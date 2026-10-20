using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils;

public class DefeatPanel : BaseMenuPanel
{
    [Header("Defeat UI")]
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
    [SerializeField] private Timer timer;
    
    private float completionTime;
    private int currentSeed;
    private bool isManualSeedMode = false;
    
    protected override void SetupButtons()
    {
#if UNITY_IOS
        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(false);
        }
#endif
    }
    
    protected override void RegisterButtonCallbacks()
    {
        retryButton.onClick.AddListener(OnRetryClicked);
        newLevelButton.onClick.AddListener(OnNewLevelClicked);
        seedToggle?.onValueChanged.AddListener(OnSeedToggleChanged);
#if !UNITY_IOS
        quitButton.onClick.AddListener(OnQuitClicked);
#endif
    }
    
    protected override void UnregisterButtonCallbacks()
    {
        retryButton.onClick.RemoveListener(OnRetryClicked);
        newLevelButton.onClick.RemoveListener(OnNewLevelClicked);
        seedToggle?.onValueChanged.RemoveListener(OnSeedToggleChanged);
#if !UNITY_IOS
        quitButton.onClick.RemoveListener(OnQuitClicked);
#endif
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
            completionTime = timer.ElapsedTime;
            currentTimeText.text = $"Time: {timer.GetFormattedTime()}";
        }
        
        if (LevelStarter.Instance != null)
        {
            currentSeed = LevelStarter.Instance.GetCurrentSeed();
            seedText.text = $"Seed: {currentSeed}";
        }
        
        if (LeaderboardManager.Instance != null && bestTimeText != null)
        {
            TimeRecord bestRecord = LeaderboardManager.Instance.GetBestTime(currentSeed);
            
            if (bestRecord != null)
            {
                bestTimeText.text = $"Best: {bestRecord.GetFormattedTime()}";
                bestTimeText.color = Color.black;
            }
            else
            {
                bestTimeText.text = "Best: --:--";
            }
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
