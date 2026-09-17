using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Utils;

public class EndGamePanel : BaseMenuPanel
{
    [Header("Panel Type")]
    [SerializeField] private bool isVictoryPanel = true;
    
    [Header("End Game UI")]
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
    
    [Header("Defeat Phrases")]
    [SerializeField] private string[] defeatPhrases = new string[]
    {
        "TOO LATE!",
        "TIME'S UP!",
        "MISSED IT!",
        "TRY AGAIN!",
        "SO CLOSE!"
    };
    
    [SerializeField] private float phraseRotationInterval = 2f;
    
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
        UpdateEndGameData();
        StartPhraseRotation();
    }
    
    protected override void OnHide()
    {
        StopPhraseRotation();
    }
    
    private void UpdateEndGameData()
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
            
            if (isVictoryPanel && LeaderboardManager.Instance != null)
            {
                int currentPathMode = levelStarter != null ? (int)levelStarter.GetPathMode() : 0;
                LeaderboardManager.Instance.SaveTime(currentSeed, completionTime, pathMode: currentPathMode);
                
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
                        bestTimeText.color = Color.white;
                    }
                }
            }
            else if (!isVictoryPanel)
            {
                bestTimeText.text = "";
            }
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
    
    private string FormatTime(float time)
    {
        if (Mathf.Approximately(time, float.MaxValue)) return "--:--.--";
        
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time * 100f) % 100f);
        
        return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }
    
    private void StartPhraseRotation()
    {
        if (phraseRotationCoroutine != null)
        {
            StopCoroutine(phraseRotationCoroutine);
        }
        phraseRotationCoroutine = StartCoroutine(RotatePhrases());
    }
    
    private void StopPhraseRotation()
    {
        if (phraseRotationCoroutine != null)
        {
            StopCoroutine(phraseRotationCoroutine);
            phraseRotationCoroutine = null;
        }
    }
    
    private IEnumerator RotatePhrases()
    {
        string[] phrases = isVictoryPanel ? victoryPhrases : defeatPhrases;
        int currentIndex = Random.Range(0, phrases.Length);
        titleText.text = phrases[currentIndex];
        yield return null;
    }
    
    private void OnRetryClicked()
    {
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        if (LevelStarter.Instance != null)
            PlayerPrefs.SetInt("RetryPathMode", (int)LevelStarter.Instance.GetPathMode());
        
        Time.timeScale = 1f;
        UIManager.Instance.HideAllPanels();
        
        if (levelManager != null)
        {
            levelManager.ReplayLevel();
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
