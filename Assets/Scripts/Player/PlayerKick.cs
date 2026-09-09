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
            bool isActuallyInKick = playerAnimator.IsInKickState();
            float nTime = playerAnimator.GetKickNormalizedTime();

            if (isActuallyInKick)
            {
                bool shouldBeActive = nTime >= config.kickHitboxStartN && nTime <= config.kickHitboxEndN;

                if (shouldBeActive && !hitboxActive) EnableHitbox();
                else if (!shouldBeActive && hitboxActive) DisableHitbox();

                // Animation finished (reached >= 95% of actual kick clip)
                if (!playerAnimator.IsInTransition() && nTime >= 0.95f && (Time.time - states.kickStartTime) > 0.1f)
                {
                    DisableHitbox();
                    states.EndKick("animFinished");
                }
            }
            else
            {
                // Animator is transitioning into kick or hasn't switched clips yet
                if (hitboxActive) DisableHitbox();

                // Generous safety backstop (0.35s): only force exit if Animator completely failed to enter Kick state
                if ((Time.time - states.kickStartTime) > 0.35f)
                {
                    GameLogger.LogWarning("KICK", $"INTERRUPTED: Animator failed to enter kick state within 0.35s (State is {playerAnimator.GetCurrentStateName()})");
                    DisableHitbox();
                    states.EndKick("animMismatchTimeout");
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