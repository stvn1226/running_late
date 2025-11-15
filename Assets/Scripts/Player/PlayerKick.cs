using UnityEngine;

public class PlayerKick : MonoBehaviour
{
    private BoxCollider2D kickBox;
    //private PlayerStates states;

    void Start()
    {
        kickBox = GetComponent<BoxCollider2D>();
        //states = GetComponentInParent<PlayerStates>();
        EndKick(); // Ensure kick hitbox is disabled at start
    }

    //void Update()
    //{
        
    //}

    public void PerformKick(float kickDuration)
    {
        Invoke("StartKick", 0.1f); // Slight delay to sync with animation
        Invoke("EndKick", kickDuration + 0.1f); // Disable kick hitbox after kick duration
    }

    private void StartKick()
    {
        if (kickBox != null)
        {
            kickBox.enabled = true;
            
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
        if (kickBox != null) {
            kickBox.enabled = false;
            //states.isKicking = false;
        }
    }
}
