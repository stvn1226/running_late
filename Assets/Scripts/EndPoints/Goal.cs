using UnityEngine;
using Utils;

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

        StartCoroutine(VictorySequence());
    }

    private System.Collections.IEnumerator VictorySequence()
    {
        if (victoryDelay > 0f)
        {
            yield return new WaitForSeconds(victoryDelay);
        }

        ShowEndGamePanel();
    }

    private void ShowEndGamePanel()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEndGamePanel();
            
            if (showDebugInfo)
            {
                GameLogger.Log("GOAL", "EndGamePanel displayed via UIManager");
            }
        }
        else
        {
            GameLogger.LogError("GOAL", "UIManager instance not found! Cannot show EndGamePanel.");
        }
    }

    public void ResetGoal()
    {
        hasTriggered = false;
    }
}
