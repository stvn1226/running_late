using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EndGamePanel : BaseMenuPanel
{
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
    
    [Header("Title Phrases")]
    [SerializeField] private string[] victoryPhrases = new string[]
    {
        "PHEW!",
        "NICE!",
        "PERFECT!",
        "AWESOME!",
        "SPEEDY!"
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
            
            string bestTimeKey = $"BestTime_Seed_{currentSeed}";
            float bestTime = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);
            
            if (completionTime < bestTime)
            {
                PlayerPrefs.SetFloat(bestTimeKey, completionTime);
                bestTimeText.text = $"Best: {FormatTime(completionTime)} (NEW!)";
                bestTimeText.color = Color.yellow;
            }
            else
            {
                bestTimeText.text = $"Best: {FormatTime(bestTime)}";
                bestTimeText.color = Color.white;
            }
        }
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
    
    private string FormatTime(float time)
    {
        if (time == float.MaxValue) return "--:--.--";
        
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
        int currentIndex = Random.Range(0, victoryPhrases.Length);
        
        // In case we want to change the words each certain number of seconds
        // while (true)
        // {
        //     titleText.text = victoryPhrases[currentIndex];
        //     currentIndex = (currentIndex + 1) % victoryPhrases.Length;
        //     
        //     yield return new WaitForSecondsRealtime(phraseRotationInterval);
        // }
        titleText.text = victoryPhrases[currentIndex];
        yield return null;
    }
    
    private void OnRetryClicked()
    {
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
    
    private void OnSeededClicked()
    {
        UIManager.Instance.ShowPanel<PauseMenuPanel>();
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
}
