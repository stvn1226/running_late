using UnityEngine;
using UnityEngine.UI;

public class Defeat : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float timeLimit = 120f;

    [Header("Visual Timer")]
    [SerializeField] private Image timerCountdown;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private float elapsedTime = 0f;
    private bool isDefeated = false;

    void Update()
    {
        if (isDefeated) return;

        elapsedTime += Time.deltaTime;

        UpdateTimerVisual();

        if (elapsedTime >= timeLimit)
        {
            TriggerDefeat();
        }
    }

    private void UpdateTimerVisual()
    {
        if (timerCountdown != null)
        {
            float timeRemaining = Mathf.Max(0f, timeLimit - elapsedTime);
            timerCountdown.fillAmount = timeRemaining / timeLimit;
        }
    }

    private void TriggerDefeat()
    {
        isDefeated = true;
        
        if (showDebugInfo)
        {
            GameLogger.Log("DEFEAT", "=== TIME'S UP - DEFEAT TRIGGERED ===");
        }
        
        if (AnalyticsManager.Instance != null && LevelStarter.Instance != null)
        {
            int currentSeed = LevelStarter.Instance.GetCurrentSeed();
            float progressPercentage = ProgressTracking.Instance != null ? ProgressTracking.Instance.GetProgressPercentage() : 0f;
            float playerPositionX = ProgressTracking.Instance != null ? ProgressTracking.Instance.GetPlayerPositionX() : 0f;
            
            var defeatParams = new System.Collections.Generic.Dictionary<string, object>
            {
                { "seed", currentSeed },
                { "completion_time", elapsedTime },
                { "success", false },
                { "progress_percentage", progressPercentage },
                { "player_position_x", playerPositionX },
                { "defeat_reason", "timeout" }
            };
            
            AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.LEVEL_COMPLETE, defeatParams);
            
            if (showDebugInfo)
            {
                GameLogger.Log("DEFEAT", $"Defeat tracked - Progress: {progressPercentage:F1}%, Position: {playerPositionX:F1}, Time: {elapsedTime:F1}s");
            }
        }

        ShowLosePanel();
    }

    private void ShowLosePanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLosePanel();
            
            if (showDebugInfo)
            {
                GameLogger.Log("DEFEAT", "Lose panel displayed via UIManager");
            }
        }
        else
        {
            GameLogger.LogError("DEFEAT", "UIManager instance not found!");
        }
    }

    public void ResetDefeat()
    {
        isDefeated = false;
        elapsedTime = 0f;
    }

    public void AddTime(float seconds)
    {
        timeLimit += seconds;
        
        if (showDebugInfo)
        {
            GameLogger.Log("DEFEAT", $"Added {seconds} seconds. New time limit: {timeLimit}s");
        }
    }

    public float GetTimeRemaining()
    {
        return Mathf.Max(0f, timeLimit - elapsedTime);
    }

    public float GetTimeLimit()
    {
        return timeLimit;
    }
}