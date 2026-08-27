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
                // Only check completion after startup to avoid reading previous clip's residual normalizedTime
                if (Time.frameCount > states.kickStartFrame + 1 && (Time.time - states.kickStartTime) > 0.05f && nTime >= 0.95f)
                {
                    DisableHitbox();
                    states.EndKick("animFinished");
                }
            }
            else
            {
                // SAFETY: Give animator at least 2 frames to catch up with state change
                if (Time.frameCount > states.kickStartFrame + 2 && (Time.time - states.kickStartTime) > 0.06f)
                {
                    GameLogger.LogWarning("KICK", $"INTERRUPTED: Animator mismatch after startup frame. State isKicking=true but Animator is {playerAnimator.GetCurrentStateName()} (Frame: {Time.frameCount} > {states.kickStartFrame})");
                    DisableHitbox();
                    states.EndKick("animMismatch");
                }
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
            Achievements.AchievementsManager.Instance?.ReportKickPerformed();
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