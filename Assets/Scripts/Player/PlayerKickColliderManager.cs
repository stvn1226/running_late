using UnityEngine;

public class PlayerKickColliderManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStates states;
    [SerializeField] private BoxCollider2D kickBox;

    [Header("Timing")]
    [SerializeField] private float kickActivationDelay = 0.06f;
    [SerializeField] private float kickActiveDuration = 0.22f;

    private float kickActivationTimer = 0f;
    private float kickActiveTimer = 0f;
    private int lastHandledKickCount = -1;
    private bool wasKicking = false;

    private void Awake()
    {
        ValidateDependencies();
    }

    private void Start()
    {
        if (kickBox != null)
        {
            kickBox.enabled = false;
        }
    }

    private void Update()
    {
        if (states == null || kickBox == null) return;

        bool isKicking = states.isKicking;

        if (isKicking)
        {
            // New kick started (edge: not kicking last frame → kicking this frame)
            if (!wasKicking)
            {
                lastHandledKickCount = states.kickTriggerCount;
                kickBox.enabled = false; // reset overlap triggers
                kickActivationTimer = kickActivationDelay;
                kickActiveTimer = kickActiveDuration;

                GameLogger.Log("KICK_COLL", $"Kick strike #{lastHandledKickCount} - delay={kickActivationDelay:F2}s, isGrounded={states.isGrounded}");
                TrackKickAnalytics();
                Achievements.AchievementsManager.Instance?.ReportKickPerformed();
            }

            // Hitbox activation delay
            if (kickActivationTimer > 0f)
            {
                kickActivationTimer -= Time.deltaTime;
                if (kickActivationTimer <= 0f)
                {
                    kickBox.enabled = true;
                    GameLogger.Log("KICK_COLL", $"Hitbox ENABLED #{lastHandledKickCount}");
                }
            }
            // Active duration window
            else if (kickActiveTimer > 0f)
            {
                kickActiveTimer -= Time.deltaTime;
                if (kickActiveTimer <= 0f)
                {
                    kickBox.enabled = false;
                    GameLogger.Log("KICK_COLL", "Hitbox active window expired - DISABLED");
                }
            }
        }
        else
        {
            if (kickBox.enabled)
            {
                kickBox.enabled = false;
                GameLogger.Log("KICK_COLL", "Kick ended - collider DISABLED");
            }
            kickActivationTimer = 0f;
            kickActiveTimer = 0f;
        }

        wasKicking = isKicking;
    }

    private void TrackKickAnalytics()
    {
        if (AnalyticsManager.Instance != null)
        {
            Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
            Vector3 playerPos = transform.position;
            float velocityX = rb != null ? rb.linearVelocity.x : 0f;
            
            var context = AnalyticsEvents.CreatePlayerActionContext(
                AnalyticsEvents.PLAYER_KICK, 
                playerPos.x, 
                velocityX
            );
            
            AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_KICK, context);
        }
    }

    private void ValidateDependencies()
    {
        if (states == null)
        {
            states = GetComponentInParent<PlayerStates>();
            if (states == null)
            {
                GameLogger.LogError("KICK_COLL", "PlayerStates not found in parent!");
            }
        }

        if (kickBox == null)
        {
            kickBox = GetComponent<BoxCollider2D>();
            if (kickBox == null)
            {
                GameLogger.LogError("KICK_COLL", "BoxCollider2D not found on this GameObject!");
            }
        }
    }
}
