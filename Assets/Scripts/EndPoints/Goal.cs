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
    private Timer cachedTimer;

    private void Start()
    {
        ValidateCollider();
        CacheReferences();
    }

    private void CacheReferences()
    {
        cachedTimer = FindObjectOfType<Timer>();
        
        if (cachedTimer == null && showDebugInfo)
        {
            GameLogger.LogWarning("GOAL", "Timer not found in scene during initialization");
        }
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

        float completionTime = cachedTimer != null ? cachedTimer.ElapsedTime : 0f;
        string formattedTime = cachedTimer != null ? cachedTimer.GetFormattedTime() : "00:00.00";
        int currentSeed = LevelStarter.Instance != null ? LevelStarter.Instance.GetCurrentSeed() : 0;
    
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackLevelComplete(currentSeed, completionTime, true);
        }

        // ─── Achievements ─────────────────────────────────────────────
        if (Achievements.AchievementsManager.Instance != null)
        {
            Achievements.AchievementsManager.Instance.Unlock(Achievements.AchievementsManager.ACH_FIRST_WIN, "First Day of School", "Complete your first level");
            
            PlayerStates playerStates = FindObjectOfType<PlayerStates>();
            if (playerStates != null && playerStates.hasToast)
            {
                Achievements.AchievementsManager.Instance.Unlock(Achievements.AchievementsManager.ACH_PERFECT_TOAST, "Breakfast Intact", "Reach school without losing the toast");
            }

            if (completionTime > 0f && completionTime <= 45f)
            {
                Achievements.AchievementsManager.Instance.Unlock(Achievements.AchievementsManager.ACH_SPEED_DEMON, "Urban Speed Demon", "Complete a level in under 45 seconds");
            }
        }

        // ─── Steam Rich Presence & Leaderboard ────────────────────────
        if (Steam.SteamRichPresence.Instance != null)
        {
            Steam.SteamRichPresence.Instance.SetLevelCompleted(currentSeed, formattedTime);
        }

        if (Steam.SteamLeaderboardBridge.Instance != null && completionTime > 0f)
        {
            int currentPathMode = LevelStarter.Instance != null ? (int)LevelStarter.Instance.GetPathMode() : 0;
            _ = Steam.SteamLeaderboardBridge.Instance.SubmitTimeAsync(completionTime, currentSeed, currentPathMode);
        }

        StopTimer();
        StartCoroutine(VictorySequence());
    }


    private void StopTimer()
    {
        if (cachedTimer != null)
        {
            cachedTimer.StopTimerPermanently();
            
            if (showDebugInfo)
            {
                GameLogger.Log("GOAL", $"Timer stopped at {cachedTimer.GetFormattedTime()}");
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
