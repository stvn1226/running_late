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
}