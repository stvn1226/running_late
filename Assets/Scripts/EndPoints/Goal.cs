using UnityEngine;
using Utils;
using System.Collections;

public class Goal : MonoBehaviour
{
    [Header("Victory Settings")]
    [SerializeField] private float victoryDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private bool hasTriggered = false;

    private void Start()
    {
        ValidateCollider();
    }

    private void ValidateCollider()
    {
        Collider2D collider = GetComponent<Collider2D>();
        
        if (collider == null)
        {
            GameLogger.LogError("GOAL", "No Collider2D found! Please add a BoxCollider2D and set IsTrigger to true.");
        }
        else if (!collider.isTrigger)
        {
            GameLogger.LogWarning("GOAL", "Collider should be set as Trigger for proper collision detection.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        
        if (other.CompareTag(TagUtils.PLAYER_TAG))
        {
            if (showDebugInfo)
            {
                GameLogger.Log("GOAL", $"Goal reached! Player '{other.name}' has reached the school!");
            }

            TriggerVictory();
        }
    }

    private void TriggerVictory()
    {
        hasTriggered = true;

        if (showDebugInfo)
        {
            GameLogger.Log("GOAL", "=== VICTORY TRIGGERED ===");
        }

        Timer timer = FindObjectOfType<Timer>();
        float completionTime = timer != null ? timer.ElapsedTime : 0f;
    
        int currentSeed = LevelStarter.Instance != null ? LevelStarter.Instance.GetCurrentSeed() : 0;
    
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackLevelComplete(currentSeed, completionTime, true);
        }

        StopTimer();
        StartCoroutine(VictorySequence());
    }


    private void StopTimer()
    {
        Timer timer = FindObjectOfType<Timer>();
        
        if (timer != null)
        {
            timer.StopTimerPermanently();
            
            if (showDebugInfo)
            {
                GameLogger.Log("GOAL", $"Timer stopped at {timer.GetFormattedTime()}");
            }
        }
        else
        {
            GameLogger.LogWarning("GOAL", "Timer not found in scene!");
        }
    }

    private IEnumerator VictorySequence()
    {
        if (victoryDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(victoryDelay);
        }

        ShowVictoryPanel();
    }

    private void ShowVictoryPanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryPanel();
            
            if (showDebugInfo)
            {
                GameLogger.Log("GOAL", "Victory panel displayed via UIManager");
            }
        }
        else
        {
            GameLogger.LogError("GOAL", "UIManager instance not found!");
        }
    }

    public void ResetGoal()
    {
        hasTriggered = false;
    }
}
