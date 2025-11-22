using UnityEngine;

public class ScoreBoost : PowerUp
{
    [Header("Score Boost Settings")]
    [SerializeField] private int scoreToAdd = 100;

    protected override void ApplyEffect(GameObject player)
    {
        if (AnalyticsManager.Instance != null)
        {
            GameLogger.Log("POWERUP", $"Score boosted by {scoreToAdd} points!");
        }
        else
        {
            GameLogger.LogWarning("POWERUP", "AnalyticsManager not found - score boost has no effect");
        }
    }
}
