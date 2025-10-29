using TMPro;
using UnityEngine;
using Utils;

public class BestTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private string timeFormat = "mm:ss.ff";
    [SerializeField] private string bestTimePrefix = "Best: ";

    [Header("References")]
    [SerializeField] private Timer currentTimer;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private TextMeshProUGUI timerText;
    private int currentSeed = -1;
    private TimeRecord bestRecord;
    private bool hasRecord = false;

    void Start()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        if (timerText == null)
        {
            GameLogger.LogError("BEST_TIMER", "No TextMeshProUGUI component found!");
            return;
        }

        if (currentTimer == null)
        {
            currentTimer = GetComponentInParent<Timer>();
            if (currentTimer == null)
            {
                GameLogger.LogError("BEST_TIMER", "No Timer reference found!");
            }
        }

        if (LevelStarter.Instance != null)
        {
            LevelStarter.Instance.OnLevelGenerationCompleted += OnLevelGenerated;
        }
    }

    void OnDestroy()
    {
        if (LevelStarter.Instance != null)
        {
            LevelStarter.Instance.OnLevelGenerationCompleted -= OnLevelGenerated;
        }
    }

    void Update()
    {
        UpdateDisplay();
    }

    private void OnLevelGenerated()
    {
        LoadBestTimeForCurrentLevel();
    }

    private void LoadBestTimeForCurrentLevel()
    {
        if (LevelStarter.Instance == null)
        {
            if (showDebugInfo)
                GameLogger.LogWarning("BEST_TIMER", "LevelStarter.Instance is null!");
            return;
        }

        currentSeed = LevelStarter.Instance.GetCurrentSeed();

        if (LeaderboardManager.Instance != null)
        {
            hasRecord = LeaderboardManager.Instance.HasRecord(currentSeed);

            if (hasRecord)
            {
                bestRecord = LeaderboardManager.Instance.GetBestTime(currentSeed);

                if (showDebugInfo)
                    GameLogger.Log("BEST_TIMER", $"Loaded best time for seed {currentSeed}: {bestRecord.GetFormattedTime()}");
            }
            else
            {
                bestRecord = null;

                if (showDebugInfo)
                    GameLogger.Log("BEST_TIMER", $"No record found for seed {currentSeed}");
            }
        }
        else
        {
            GameLogger.LogWarning("BEST_TIMER", "LeaderboardManager.Instance is null!");
        }
    }

    private void UpdateDisplay()
    {
        if (timerText == null) return;

        float timeToDisplay;

        if (hasRecord && bestRecord != null)
        {
            timeToDisplay = bestRecord.time;
        }
        else if (currentTimer != null)
        {
            timeToDisplay = currentTimer.ElapsedTime;
        }
        else
        {
            timeToDisplay = 0f;
        }

        timerText.text = bestTimePrefix + FormatTime(timeToDisplay);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time * 100f) % 100f);

        switch (timeFormat.ToLower())
        {
            case "mm:ss.ff":
                return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
            case "mm:ss":
                return $"{minutes:00}:{seconds:00}";
            case "ss.ff":
                return $"{seconds:00}.{centiseconds:00}";
            case "ss":
                return $"{seconds:00}";
            default:
                return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
        }
    }

    public void RefreshBestTime()
    {
        LoadBestTimeForCurrentLevel();
    }
}
