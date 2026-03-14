using UnityEngine;

public class PlayerKick : MonoBehaviour
{
    private BoxCollider2D kickBox;
    private PlayerStates states;
    private PlayerAnimator playerAnimator;
    private PlayerConfig config;
    private bool hitboxActive = false;

    void Start()
    {
        kickBox = GetComponent<BoxCollider2D>();
        states = GetComponentInParent<PlayerStates>();
        playerAnimator = GetComponentInParent<PlayerAnimator>();
        
        if (states != null)
        {
            config = states.GetComponent<PlayerMovement>()?.config;
        }

        if (states == null || playerAnimator == null)
        {
            GameLogger.LogError("KICK", "Dependencies not found! Kick system will not work properly.");
        }
        
        DisableHitbox();
    }

    void Update()
    {
        if (states == null || config == null || playerAnimator == null) return;

        if (states.isKicking)
        {
            if (playerAnimator.IsPlayingKick())
            {
                float nTime = playerAnimator.GetKickNormalizedTime();
                bool shouldBeActive = nTime >= config.kickHitboxStartN && nTime <= config.kickHitboxEndN;

                if (shouldBeActive && !hitboxActive) EnableHitbox();
                else if (!shouldBeActive && hitboxActive) DisableHitbox();

                // Animation finished (reached >= 95% without looping)
                if (nTime >= 0.95f)
                {
                    DisableHitbox();
                    states.EndKick("animFinished");
                }
            }
            else
            {
                // Animator switched to a different clip (e.g., interrupted by landing or wallrun)
                DisableHitbox();
                states.EndKick("animChanged");
            }
        }
        else if (hitboxActive)
        {
            DisableHitbox();
        }
    }

    public void PerformKick(float kickDuration)
    {
        // Now handled via states.isKicking and PlayerAnimator detection
        if (config.kickDebug) GameLogger.Log("KICK", "PerformKick called (Animation synchronization active)");
    }

    private void EnableHitbox()
    {
        if (kickBox != null)
        {
            kickBox.enabled = true;
            hitboxActive = true;
            if (config.kickDebug) GameLogger.Log("KICK", $"Hitbox ENABLED (NTime: {playerAnimator.GetKickNormalizedTime():F2})");
            TrackKickAnalytics();
        }
    }

    private void DisableHitbox()
    {
        if (hitboxActive)
        {
            if (kickBox != null) kickBox.enabled = false;
            hitboxActive = false;
            if (config.kickDebug) GameLogger.Log("KICK", $"Hitbox DISABLED (NTime: {playerAnimator.GetKickNormalizedTime():F2})");
        }
        else if (kickBox != null && kickBox.enabled)
        {
            kickBox.enabled = false;
        }
    }

    private void TrackKickAnalytics()
    {
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