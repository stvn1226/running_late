using UnityEngine;

public class PlayerKick : MonoBehaviour
{
    private BoxCollider2D kickBox;
    private PlayerStates states;

    void Start()
    {
        kickBox = GetComponent<BoxCollider2D>();
        states = GetComponentInParent<PlayerStates>();
        
        if (states == null)
        {
            GameLogger.LogError("KICK", "PlayerStates not found in parent! Kick system will not work properly.");
        }
        
        EndKick();
    }

    public void PerformKick(float kickDuration)
    {
        Invoke("StartKick", 0.1f);
        Invoke("EndKick", kickDuration + 0.1f);
        
        GameLogger.LogVerbose("KICK", $"PerformKick scheduled - StartKick at T+0.1s, EndKick at T+{kickDuration + 0.1f}s");
    }

    private void StartKick()
    {
        if (kickBox != null)
        {
            kickBox.enabled = true;
            GameLogger.LogVerbose("KICK", "KickBox ENABLED (Invoke callback)");
            
            if (AnalyticsManager.Instance != null)
            {
                Vector3 playerPos = transform.position;
                Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
                float velocityX = rb != null ? rb.linearVelocity.x : 0f;
            
                var context = AnalyticsEvents.CreatePlayerActionContext(
                    AnalyticsEvents.PLAYER_KICK, 
                    playerPos.x, 
                    velocityX
                );
            
                AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_KICK, context);
            }
        }
    }

    private void EndKick()
    {
        if (kickBox != null)
        {
            kickBox.enabled = false;
            GameLogger.LogVerbose("KICK", "KickBox DISABLED (Invoke callback)");
        }
    }
}